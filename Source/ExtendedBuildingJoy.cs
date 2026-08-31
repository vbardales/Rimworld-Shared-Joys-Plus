using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace SharedJoysPlus
{
    public enum ExtJoyKind
    {
        None,
        Art,
        Grave,
        Meditation
    }

    /// <summary>
    /// Les batiments de loisir que Shared Joys ne sait pas reconnaitre, parce qu'aucun
    /// <c>JoyGiverDef</c> ne cite leur <c>ThingDef</c> dans <c>thingDefs</c> : l'art, les tombes
    /// et les foyers de meditation. Leurs fournisseurs vanilla cherchent leur cible eux-memes,
    /// par groupe de choses ou par comp, et n'ont donc rien a lister.
    /// </summary>
    public static class ExtendedBuildingJoy
    {
        /// <summary>Rayon de dispersion quand plusieurs pions visent le meme point de rendez-vous.</summary>
        const float SpreadRadius = 12f;

        /// <summary>Plafond de participants annonce pour un point de rendez-vous.</summary>
        const int MaxCountedSpots = 6;

        public static ExtJoyKind KindOf(Thing t)
        {
            if (t == null || !t.Spawned) return ExtJoyKind.None;
            if (IsMeditationFocus(t)) return ExtJoyKind.Meditation;
            if (t is Building_Grave) return ExtJoyKind.Grave;
            if (IsArt(t)) return ExtJoyKind.Art;
            return ExtJoyKind.None;
        }

        // --- Reconnaissance -------------------------------------------------------------------

        static bool IsArt(Thing t)
        {
            CompArt art = t.TryGetComp<CompArt>();
            return art != null && art.Props != null && art.Props.canBeEnjoyedAsArt;
        }

        static bool IsMeditationFocus(Thing t)
        {
            if (!ModsConfig.RoyaltyActive) return false;
            if (t is Building_Throne) return true;
            if (t.def == ThingDefOf.MeditationSpot) return true;
            return t.TryGetComp<CompMeditationFocus>() != null;
        }

        public static bool IsPrison(Room room)
        {
            if (room == null) return false;
            if (room.IsPrisonCell) return true;
            RoomRoleDef role = room.Role;
            return role == RoomRoleDefOf.PrisonCell || role == RoomRoleDefOf.PrisonBarracks;
        }

        // --- Fabrication des taches -----------------------------------------------------------

        /// <summary>
        /// Fabrique la tache de loisir d'un pion sur ce point de rendez-vous, ou null.
        /// Les cibles deja distribuees sont ajoutees a <paramref name="taken"/> pour que le pion
        /// suivant en prenne une autre : Shared Joys passe la meme liste a tous les participants.
        /// </summary>
        public static Job TryMakeJob(Pawn pawn, Building anchor, List<LocalTargetInfo> taken)
        {
            if (pawn == null || anchor == null || !anchor.Spawned) return null;
            if (taken == null) taken = new List<LocalTargetInfo>();

            switch (KindOf(anchor))
            {
                case ExtJoyKind.Art: return MakeVisitJob(pawn, anchor, taken, ThingRequestGroup.Art, SjpDefOf.ViewArt, CanViewArt);
                case ExtJoyKind.Grave: return MakeVisitJob(pawn, anchor, taken, ThingRequestGroup.Grave, SjpDefOf.VisitGrave, CanVisitGrave);
                case ExtJoyKind.Meditation: return MakeMeditationJob(pawn, anchor, taken);
                default: return null;
            }
        }

        /// <summary>
        /// Art et tombes se reservent a un seul pion (<c>JobDriver_VisitJoyThing</c> reserve la
        /// cible avec <c>maxPawns 1</c>). Un groupe se repartit donc sur les pieces voisines :
        /// un jardin de sculptures ou un cimetiere, pas une seule statue.
        /// </summary>
        static Job MakeVisitJob(Pawn pawn, Building anchor, List<LocalTargetInfo> taken,
                                ThingRequestGroup group, JobDef jobDef, Func<Pawn, Thing, bool> validator)
        {
            Thing target = PickFreeTarget(pawn, anchor, taken, group, validator);
            if (target == null) return null;
            taken.Add(target);
            return JobMaker.MakeJob(jobDef, target);
        }

        static Thing PickFreeTarget(Pawn pawn, Building anchor, List<LocalTargetInfo> taken,
                                    ThingRequestGroup group, Func<Pawn, Thing, bool> validator)
        {
            if (!taken.Contains(anchor) && validator(pawn, anchor)) return anchor;

            Room anchorRoom = anchor.GetRoom();
            Thing best = null;
            float bestDist = float.MaxValue;

            foreach (Thing t in anchor.Map.listerThings.ThingsInGroup(group))
            {
                if (t == anchor || taken.Contains(t)) continue;
                float dist = t.Position.DistanceTo(anchor.Position);
                if (dist > SpreadRadius || dist >= bestDist) continue;
                if (anchorRoom != null && t.GetRoom() != anchorRoom) continue;
                if (!validator(pawn, t)) continue;
                best = t;
                bestDist = dist;
            }
            return best;
        }

        static bool CanViewArt(Pawn pawn, Thing t)
        {
            if (!IsArt(t) || t.Faction != Faction.OfPlayer || t.Fogged()) return false;

            CompArt art = t.TryGetComp<CompArt>();
            if (art == null || !art.CanShowArt) return false;

            Room room = t.GetRoom();
            if (room == null) return false;
            // Le vanilla evite l'art d'une chambre qui n'est pas la sienne.
            if (room.Role != null && room.Role.avoidViewingArtIfUnowned
                && (pawn.ownership?.OwnedRoom == null || pawn.ownership.OwnedRoom != room))
                return false;

            if (t.IsForbidden(pawn) || !t.IsPoliticallyProper(pawn)) return false;
            return pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.None);
        }

        static bool CanVisitGrave(Pawn pawn, Thing t)
        {
            Building_Grave grave = t as Building_Grave;
            if (grave == null || !grave.HasCorpse || grave.Fogged()) return false;
            if (grave.Faction != Faction.OfPlayer) return false;
            if (grave.Corpse.InnerPawn?.Faction != Faction.OfPlayer) return false;
            if (grave.IsForbidden(pawn) || !grave.IsPoliticallyProper(pawn)) return false;
            return pawn.CanReserveAndReach(grave, PathEndMode.Touch, Danger.None);
        }

        /// <summary>
        /// La meditation est le seul des trois cas ou tout le monde partage vraiment la meme
        /// cible : le foyer est commun, seule la case d'assise change. C'est aussi le seul qui
        /// puisse accueillir un groupe entier autour d'un unique objet.
        /// </summary>
        static Job MakeMeditationJob(Pawn pawn, Building focus, List<LocalTargetInfo> taken)
        {
            if (!MeditationUtility.CanMeditateNow(pawn)) return null;
            if (focus.IsForbidden(pawn)) return null;

            Building_Throne throne = focus as Building_Throne;
            if (throne != null)
            {
                // Un trone n'appartient qu'a son proprietaire, et le vanilla passe par Reign.
                if (taken.Contains(throne)) return null;
                if (throne.AssignedPawn != null && throne.AssignedPawn != pawn) return null;
                if (!pawn.CanReserveAndReach(throne, PathEndMode.OnCell, Danger.None)) return null;
                taken.Add(throne);
                return JobMaker.MakeJob(JobDefOf.Reign, throne, LocalTargetInfo.Invalid, throne);
            }

            List<LocalTargetInfo> alreadyTaken = taken;
            LocalTargetInfo spot = MeditationUtility.MeditationSpotForFocus(
                focus, pawn, cell => !alreadyTaken.Contains(new LocalTargetInfo(cell)));
            if (!spot.IsValid) return null;

            taken.Add(spot);
            JobDef def = MeditationJobFor(pawn);
            return JobMaker.MakeJob(def, spot, LocalTargetInfo.Invalid, focus);
        }

        static JobDef MeditationJobFor(Pawn pawn)
        {
            // Meme choix que MeditationUtility.GetMeditationJob(forJoy: true).
            if (ModsConfig.IdeologyActive && pawn.Ideo != null
                && pawn.Ideo.foundation is IdeoFoundation_Deity deity && deity.DeitiesListForReading.Any())
                return JobDefOf.MeditatePray;
            return JobDefOf.Meditate;
        }

        // --- Comptage des places ---------------------------------------------------------------

        /// <summary>
        /// Combien de pions ce point de rendez-vous peut accueillir. Shared Joys s'en sert pour
        /// choisir un lieu d'evenement autonome, et exige au moins deux places.
        /// </summary>
        public static int CountSpots(Building anchor, Pawn probe)
        {
            if (anchor == null || !anchor.Spawned || probe == null) return 0;

            switch (KindOf(anchor))
            {
                case ExtJoyKind.Art: return CountNearbyTargets(anchor, probe, ThingRequestGroup.Art, CanViewArt);
                case ExtJoyKind.Grave: return CountNearbyTargets(anchor, probe, ThingRequestGroup.Grave, CanVisitGrave);
                case ExtJoyKind.Meditation: return CountMeditationSpots(anchor, probe);
                default: return 0;
            }
        }

        static int CountNearbyTargets(Building anchor, Pawn probe, ThingRequestGroup group, Func<Pawn, Thing, bool> validator)
        {
            Room anchorRoom = anchor.GetRoom();
            int count = 0;
            foreach (Thing t in anchor.Map.listerThings.ThingsInGroup(group))
            {
                if (t != anchor)
                {
                    if (t.Position.DistanceTo(anchor.Position) > SpreadRadius) continue;
                    if (anchorRoom != null && t.GetRoom() != anchorRoom) continue;
                }
                if (!validator(probe, t)) continue;
                if (++count >= MaxCountedSpots) break;
            }
            return count;
        }

        static int CountMeditationSpots(Building focus, Pawn probe)
        {
            if (!MeditationUtility.CanMeditateNow(probe)) return 0;
            if (focus is Building_Throne) return 1;

            Map map = focus.Map;
            int count = 0;
            foreach (IntVec3 cell in focus.OccupiedRect().ExpandedBy(2).AdjacentCellsCardinal)
            {
                if (!cell.InBounds(map) || !cell.Standable(map) || cell.IsForbidden(probe)) continue;
                if (map.reservationManager.IsReservedByAnyoneOf(cell, Faction.OfPlayer)) continue;
                if (++count >= MaxCountedSpots) break;
            }
            return count;
        }
    }
}
