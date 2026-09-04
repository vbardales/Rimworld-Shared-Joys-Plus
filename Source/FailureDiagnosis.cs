using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SharedJoysPlus
{
    /// <summary>
    /// Says why a shared break could not start.
    /// </summary>
    /// <remarks>
    /// Shared Joys has a single message for every placement failure, "Not enough space for everyone
    /// to chill", and it sends you looking for room where what is usually missing is a chair. Our
    /// extension made it worse: one sculpture for two colonists produced exactly the same text,
    /// when the real reason is that an artwork can only be admired by one pawn at a time.
    ///
    /// The message is not doubled but <b>replaced</b>: the reason is worked out when job creation
    /// fails, then substituted inside <c>JoyUtil.Notify</c>. That method is only ever called on
    /// manual paths, every call site in Blues' code being guarded by <c>if (manual)</c>, so a
    /// failed autonomous event never says a word.
    /// </remarks>
    public static class FailureDiagnosis
    {
        static string pendingReason;
        static int pendingTick = -1;

        /// <summary>
        /// Works out and holds the reason this building failed. Called from the <c>MakeJoyJob</c>
        /// postfix, so once per participant: the last to speak wins, which is the right choice, since
        /// that is the one who could not follow.
        /// </summary>
        public static void Note(Pawn pawn, Building b)
        {
            pendingReason = Diagnose(pawn, b);
            pendingTick = Find.TickManager?.TicksGame ?? -1;
        }

        /// <summary>
        /// Substitutes our reason for Shared Joys' own, if it was worked out on the same tick. That
        /// time bound stops a forgotten diagnosis from capping an unrelated message, such as
        /// "so-and-so cannot reach so-and-so".
        /// </summary>
        public static bool TryTakeOver(ref string text)
        {
            if (pendingReason == null) return false;

            bool sameTick = pendingTick == (Find.TickManager?.TicksGame ?? -1);
            string reason = pendingReason;
            pendingReason = null;
            pendingTick = -1;

            if (!sameTick) return false;
            text = reason;
            return true;
        }

        static string Diagnose(Pawn pawn, Building b)
        {
            if (pawn == null || b == null || !b.Spawned) return null;

            switch (ExtendedBuildingJoy.KindOf(b))
            {
                case ExtJoyKind.Art:
                    // A target would exist if nothing were taken, so everything must be taken.
                    return ExtendedBuildingJoy.HasAnyTarget(pawn, b)
                        ? "SJP_ArtAllTaken".Translate(b.LabelShort).ToString()
                        : null;

                case ExtJoyKind.Grave:
                    return ExtendedBuildingJoy.HasAnyTarget(pawn, b)
                        ? "SJP_GraveAllTaken".Translate(b.LabelShort).ToString()
                        : null;

                case ExtJoyKind.Meditation:
                    return "SJP_NoMeditationSpot".Translate(b.LabelShort).ToString();
            }

            return DiagnoseSeating(pawn, b);
        }

        /// <summary>
        /// The most misleading case: boards and televisions want a chair, and vanilla makes no secret
        /// of it. <c>requireChair</c> defaults to true and no base game <c>JoyGiverDef</c> sets it
        /// false. Boards are handled by <see cref="GroundSeating"/> now, so only the television case
        /// still turns on a chair here.
        /// </summary>
        static string DiagnoseSeating(Pawn pawn, Building b)
        {
            JoyGiverDef giver = GroundSeating.GiverFor(b.def);
            if (giver == null || giver.giverClass == null) return null;

            // The ceiling first: it is the most frequent reason, and the only one no arrangement of the
            // room can lift. Shared Joys works out free places as joyMaxParticipants minus the
            // reservations already placed, so a piece of furniture seating two refuses a third with the
            // same "not enough space" as a room with no chairs in it. We only speak when the seats really
            // are all taken, so that a genuine lack of space keeps its own message.
            int seats = giver.jobDef != null ? giver.jobDef.joyMaxParticipants : 0;
            if (seats > 0 && TakenSlots(b, giver) >= seats)
                return "SJP_SeatsOnly".Translate(b.LabelShort, seats).ToString();

            // No chair message for boards any more: GroundSeating has them play on the ground. If they
            // still fail, none of the four cardinal cells is free.
            if (typeof(JoyGiver_InteractBuildingSitAdjacent).IsAssignableFrom(giver.giverClass))
                return "SJP_NoSpotBeside".Translate(b.LabelShort).ToString();

            if (typeof(JoyGiver_WatchBuilding).IsAssignableFrom(giver.giverClass)
                && giver.desireSit
                && !HasFreeChairAmong(pawn, WatchBuildingUtility.CalculateWatchCells(b.def, b.Position, b.Rotation, b.Map), b.Map))
                return "SJP_NeedsChairToWatch".Translate(b.LabelShort).ToString();

            return null;
        }

        /// <summary>
        /// How many places on this building are already spoken for, counted the same way
        /// <see cref="SharedJoysFixes.FreeParticipantSlots_Prefix"/> counts them: only the
        /// reservations whose job is this recreation, never a hauler or a cleaner passing through.
        /// </summary>
        static int TakenSlots(Building b, JoyGiverDef giver)
        {
            if (b?.Map == null || giver?.jobDef == null) return 0;

            int taken = 0;
            foreach (ReservationManager.Reservation reservation in b.Map.reservationManager.ReservationsReadOnly)
            {
                if (reservation.Target.Thing != b) continue;
                if (reservation.Job == null || reservation.Job.def != giver.jobDef) continue;
                taken++;
            }
            return taken;
        }

        static bool HasFreeChairAmong(Pawn pawn, IEnumerable<IntVec3> cells, Map map)
        {
            if (map == null) return false;
            foreach (IntVec3 cell in cells)
            {
                if (!cell.InBounds(map) || cell.IsForbidden(pawn)) continue;
                Building edifice = cell.GetEdifice(map);
                if (edifice == null || !edifice.def.building.isSittable) continue;
                if (!pawn.CanReserveSittableOrSpot(cell)) continue;
                return true;
            }
            return false;
        }

    }
}
