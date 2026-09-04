using RimWorld;
using Verse;
using Verse.AI;

namespace SharedJoysPlus
{
    /// <summary>
    /// Bug fixes for Shared Joys, on top of the extension.
    /// </summary>
    /// <remarks>
    /// Unlike the three hooks in <see cref="HarmonyPatches"/>, which are passive and only speak up
    /// where the original has given up, these <b>replace</b> a behaviour. Each one therefore carries
    /// the reason the original behaviour is wrong, not merely what it does instead.
    ///
    /// Neither has a switch: a setting nobody would ever untick is just noise in an already busy
    /// window. If Blues fixes it upstream the prefix simply computes the same answer he does;
    /// if he changes the signature, <c>PatchFix</c> says so in the log and moves on.
    /// </remarks>
    public static class SharedJoysFixes
    {
        /// <summary>
        /// <c>JoyUtil.IsValidChair</c> recognises a seat by
        /// <c>defName.ToLower().Contains("bench")</c> or <c>"seat"</c>, on top of
        /// <c>building.isSittable</c>. Being an OR, the heuristic widens nothing useful: it only
        /// adds false positives. In vanilla it already catches
        /// <c>HandTailoringBench</c>, <c>ElectricTailoringBench</c>, <c>SimpleResearchBench</c> and
        /// <c>HiTechResearchBench</c>: a pawn sent to "sit" on a research bench.
        ///
        /// Nothing is lost by dropping it: furniture the game can actually seat a pawn on carries
        /// <c>isSittable</c>, or <c>CanReserveSittableOrSpot</c> would refuse it anyway.
        /// </summary>
        public static bool IsValidChair_Prefix(Building chair, Pawn pawn, ref bool __result)
        {
            __result = chair != null
                       && chair.def.building != null
                       && chair.def.building.isSittable
                       && pawn.CanReserveAndReach(chair, PathEndMode.Touch, Danger.Some);
            return false;
        }

        /// <summary>
        /// <c>JoyJobFactory.FreeParticipantSlots</c> counts <b>every</b> reservation held on the
        /// building, whatever the job, and subtracts them from <c>joyMaxParticipants</c>. A hauler
        /// fetching an item left on it, a builder repairing it, a cleaner: each one makes the table
        /// look full and blocks the shared break.
        ///
        /// So only the reservations whose job is this very recreation are counted.
        /// </summary>
        public static bool FreeParticipantSlots_Prefix(Building b, JoyGiverDef giver, ref int __result)
        {
            // Outside our assumptions: let the original speak.
            if (b == null || b.Map == null || giver == null || giver.jobDef == null) return true;

            int taken = 0;
            foreach (ReservationManager.Reservation reservation in b.Map.reservationManager.ReservationsReadOnly)
            {
                if (reservation.Target.Thing != b) continue;
                if (reservation.Job == null || reservation.Job.def != giver.jobDef) continue;
                taken++;
            }

            __result = giver.jobDef.joyMaxParticipants - taken;
            return false;
        }

        /// <summary>
        /// <c>JoyJobFactory</c> caches in static fields the <c>MethodInfo</c> of two <b>private</b>
        /// methods of <c>JoyGiver_InteractBuilding</c>: <c>CanInteractWith</c> and
        /// <c>TryGivePlayJob</c>. If a RimWorld version renames them, <c>HasOwnWorker</c> simply
        /// returns false and Shared Joys falls back to the watch-a-building path: chess, poker
        /// and instruments quietly become televisions.
        ///
        /// Nothing can repair that from the outside: our own lookup would fail for the same reason.
        /// But a silent failure can at least become a line in the log, which is all the difference
        /// between a bug a player reports and a bug you see when the game loads.
        /// </summary>
        public static void ReportReflectionHealth()
        {
            if (BluesBridge.SettingsType == null) return;

            bool canInteract = BluesBridge.PrivateStaticFieldIsSet("Blues.JoyJobFactory", "canInteractWith");
            bool tryGivePlay = BluesBridge.PrivateStaticFieldIsSet("Blues.JoyJobFactory", "tryGivePlayJob");
            if (canInteract && tryGivePlay) return;

            Log.Warning("[Shared Joys+] Shared Joys could not find JoyGiver_InteractBuilding."
                        + (canInteract ? "" : " CanInteractWith")
                        + (tryGivePlay ? "" : " TryGivePlayJob")
                        + " - those are private vanilla methods, and this RimWorld version seems to "
                        + "have renamed them. Chess, poker, billiards and instruments will fall back "
                        + "to the watch-building path instead of their own.");
        }
    }
}
