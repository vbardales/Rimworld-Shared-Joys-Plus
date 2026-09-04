using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SharedJoysPlus
{
    /// <summary>
    /// Lets board games be played without a chair, sitting on the ground, up to the number of
    /// players the job allows.
    /// </summary>
    /// <remarks>
    /// <c>JoyGiver_InteractBuildingSitAdjacent.TryGivePlayJob</c> looks for a seat in two passes:
    /// a real chair first, then any reservable cell. But between the two it does
    /// <c>if (def.requireChair) break;</c>, and <c>requireChair</c> defaults to <b>true</b>, with
    /// no <c>JoyGiverDef</c> in the base game setting it to false. So the second pass almost never
    /// happens: no chair, no game.
    ///
    /// Nothing in the game actually needs that chair. <c>JobDriver_SitFacingBuilding</c> reserves
    /// the seat with <c>ReserveSittableOrSpot</c>, which takes a bare cell, and the pawn sits down
    /// without anything breaking. So the second pass is done here instead.
    ///
    /// The one limit kept is the game's own: <c>jobDef.joyMaxParticipants</c>, the very number the
    /// driver reserves on the building. A board meant for two stays a board for two.
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

            // The same refusals as CanInteractWith, minus the one about the chair.
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
        /// A free cardinal cell. Chairs first, as vanilla does: where there are any, the pawns may
        /// as well sit on them.
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
                    // A wall or another piece of furniture: you do not sit inside it.
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
