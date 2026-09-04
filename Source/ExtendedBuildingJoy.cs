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
    /// The recreation buildings Shared Joys cannot recognise, because no <c>JoyGiverDef</c> names
    /// their <c>ThingDef</c> in <c>thingDefs</c>: art, graves and meditation foci. Their vanilla
    /// givers find their target on their own, by thing group or by comp, and so have nothing
    /// to list.
    /// </summary>
    public static class ExtendedBuildingJoy
    {
        /// <summary>Spread radius when several pawns aim at the same meeting point.</summary>
        const float SpreadRadius = 12f;

        /// <summary>Participant ceiling reported for a meeting point.</summary>
        const int MaxCountedSpots = 6;

        public static ExtJoyKind KindOf(Thing t)
        {
            if (t == null || !t.Spawned) return ExtJoyKind.None;
            if (IsMeditationFocus(t)) return ExtJoyKind.Meditation;
            if (t is Building_Grave) return ExtJoyKind.Grave;
            if (IsArt(t)) return ExtJoyKind.Art;
            return ExtJoyKind.None;
        }

        // --- Recognition -------------------------------------------------------------------

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

        // --- Building the jobs -----------------------------------------------------------

        /// <summary>
        /// Builds a pawn's recreation job at this meeting point, or null.
        /// Targets already handed out are added to <paramref name="taken"/> so the next pawn picks
        /// another one: Shared Joys passes the same list to every participant.
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
        /// Art and graves are reserved for a single pawn (<c>JobDriver_VisitJoyThing</c> reserves the
        /// target with <c>maxPawns 1</c>). So a group spreads over the neighbouring pieces:
        /// a sculpture garden or a graveyard, never a single statue.
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

        /// <summary>
        /// Would there be a valid target if none had been handed out yet? Used by the diagnosis: if
        /// so while <see cref="TryMakeJob"/> returned null, then everything is taken, which is not
        /// at all the same thing as "nothing usable here".
        /// </summary>
        public static bool HasAnyTarget(Pawn pawn, Building anchor)
        {
            if (pawn == null || anchor == null || !anchor.Spawned) return false;
            List<LocalTargetInfo> nothingTaken = new List<LocalTargetInfo>();

            switch (KindOf(anchor))
            {
                case ExtJoyKind.Art:
                    return PickFreeTarget(pawn, anchor, nothingTaken, ThingRequestGroup.Art, CanViewArt) != null;
                case ExtJoyKind.Grave:
                    return PickFreeTarget(pawn, anchor, nothingTaken, ThingRequestGroup.Grave, CanVisitGrave) != null;
                case ExtJoyKind.Meditation:
                    return MakeMeditationJob(pawn, anchor, nothingTaken) != null;
                default:
                    return false;
            }
        }

        static bool CanViewArt(Pawn pawn, Thing t)
        {
            if (!IsArt(t) || t.Faction != Faction.OfPlayer || t.Fogged()) return false;

            CompArt art = t.TryGetComp<CompArt>();
            if (art == null || !art.CanShowArt) return false;

            Room room = t.GetRoom();
            if (room == null) return false;
            // Vanilla avoids art in a bedroom that is not the pawn's own.
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
        /// Meditation is the only one of the three where everyone truly shares the same target: the
        /// focus is common and only the sitting cell changes. It is also the only one that can hold
        /// a whole group around a single object.
        /// </summary>
        static Job MakeMeditationJob(Pawn pawn, Building focus, List<LocalTargetInfo> taken)
        {
            if (!MeditationUtility.CanMeditateNow(pawn)) return null;
            if (focus.IsForbidden(pawn)) return null;

            Building_Throne throne = focus as Building_Throne;
            if (throne != null)
            {
                // A throne belongs to its owner alone, and vanilla goes through Reign.
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
            // Same choice as MeditationUtility.GetMeditationJob(forJoy: true).
            if (ModsConfig.IdeologyActive && pawn.Ideo != null
                && pawn.Ideo.foundation is IdeoFoundation_Deity deity && deity.DeitiesListForReading.Any())
                return JobDefOf.MeditatePray;
            return JobDefOf.Meditate;
        }

        // --- Counting the spots ---------------------------------------------------------------

        /// <summary>
        /// How many pawns this meeting point can hold. Shared Joys uses it to pick a place for an
        /// autonomous event, and requires at least two spots.
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
