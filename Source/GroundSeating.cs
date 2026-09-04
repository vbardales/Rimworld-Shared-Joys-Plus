using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SharedJoysPlus
{
    /// <summary>
    /// Fait jouer aux plateaux sans chaise, assis par terre, dans la limite du nombre de joueurs
    /// que la tache autorise.
    /// </summary>
    /// <remarks>
    /// <c>JoyGiver_InteractBuildingSitAdjacent.TryGivePlayJob</c> cherche une place en deux passes :
    /// d'abord une vraie chaise, puis n'importe quelle case reservable. Mais entre les deux il fait
    /// <c>if (def.requireChair) break;</c>, et <c>requireChair</c> vaut <b>vrai par defaut</b> —
    /// aucun <c>JoyGiverDef</c> du jeu de base ne le passe a faux. La seconde passe n'a donc
    /// pratiquement jamais lieu : pas de chaise, pas de partie.
    ///
    /// Rien dans le jeu n'exige pourtant cette chaise. <c>JobDriver_SitFacingBuilding</c> reserve
    /// la case d'assise par <c>ReserveSittableOrSpot</c>, qui accepte une case nue, et le pion s'y
    /// assoit sans rien casser. On refait donc la seconde passe nous-memes.
    ///
    /// Le seul plafond qu'on respecte est celui du jeu : <c>jobDef.joyMaxParticipants</c>, celui-la
    /// meme que le pilote reserve sur le batiment. Un plateau prevu pour deux reste a deux.
    /// </remarks>
    public static class GroundSeating
    {
        public static Job TryMake(Pawn pawn, Building b, List<LocalTargetInfo> taken)
        {
            if (pawn == null || b == null || !b.Spawned || b.Map == null) return null;
            if (taken == null) taken = new List<LocalTargetInfo>();

            JoyGiverDef giver = GiverFor(b.def);
            if (giver?.jobDef == null || giver.giverClass == null) return null;
            if (!typeof(JoyGiver_InteractBuildingSitAdjacent).IsAssignableFrom(giver.giverClass)) return null;

            // Les memes refus que CanInteractWith, moins celui de la chaise.
            if (b.IsForbidden(pawn) || b.Fogged()) return null;
            if (!b.IsSociallyProper(pawn) || !b.IsPoliticallyProper(pawn)) return null;
            CompPowerTrader power = b.TryGetComp<CompPowerTrader>();
            if (power != null && !power.PowerOn) return null;

            int cap = Mathf.Max(1, giver.jobDef.joyMaxParticipants);
            if (!pawn.CanReserve(b, cap)) return null;

            IntVec3 seat = FindSeat(pawn, b, taken, chairsOnly: true);
            if (!seat.IsValid) seat = FindSeat(pawn, b, taken, chairsOnly: false);
            if (!seat.IsValid) return null;

            taken.Add(seat);
            return JobMaker.MakeJob(giver.jobDef, b, seat);
        }

        /// <summary>
        /// Une case cardinale libre. Les chaises d'abord, comme le vanilla : quand il y en a, autant
        /// que les pions s'assoient dessus.
        /// </summary>
        static IntVec3 FindSeat(Pawn pawn, Building b, List<LocalTargetInfo> taken, bool chairsOnly)
        {
            Map map = b.Map;
            foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(b))
            {
                if (!cell.InBounds(map) || cell.IsForbidden(pawn)) continue;
                if (taken.Contains(cell)) continue;
                if (!pawn.CanReserveSittableOrSpot(cell)) continue;

                Building edifice = cell.GetEdifice(map);
                if (chairsOnly)
                {
                    if (edifice == null || !edifice.def.building.isSittable) continue;
                }
                else if (edifice != null && !edifice.def.building.isSittable)
                {
                    // Un mur ou un autre meuble : on ne s'assoit pas dedans.
                    continue;
                }

                return cell;
            }
            return IntVec3.Invalid;
        }

        public static JoyGiverDef GiverFor(ThingDef def)
        {
            List<JoyGiverDef> all = DefDatabase<JoyGiverDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                List<ThingDef> things = all[i].thingDefs;
                if (things != null && things.Contains(def)) return all[i];
            }
            return null;
        }
    }
}
