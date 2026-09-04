using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SharedJoysPlus
{
    /// <summary>
    /// Three hooks on <c>Blues.JoyUtil</c>, all postfixes and all passive: they only speak up
    /// where Shared Joys has already given up (false result, null job, zero spots).
    /// None of its original behaviour is changed.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            if (!BluesBridge.Present)
            {
                Log.Message("[Shared Joys+] Shared Joys is not loaded: only the placeless activity menu stays active.");
                return;
            }

            Harmony harmony = new Harmony(BluesBridge.HarmonyId);
            int patched = 0;

            patched += Patch(harmony, "IsValidJoyBuilding",
                new[] { typeof(Building), typeof(bool) }, nameof(IsValidJoyBuilding_Postfix));
            patched += Patch(harmony, "MakeJoyJob",
                new[] { typeof(Pawn), typeof(Building), typeof(List<LocalTargetInfo>) }, nameof(MakeJoyJob_Postfix));
            patched += Patch(harmony, "GetAvailableSpots",
                new[] { typeof(Building), typeof(Pawn) }, nameof(GetAvailableSpots_Postfix));

            // Fixes: these replace a behaviour instead of completing it, hence the prefix. They are
            // always applied, since a setting nobody would untick is only noise.
            PatchFix(harmony, BluesBridge.JoyUtilType, "IsValidChair",
                new[] { typeof(Building), typeof(Pawn) }, nameof(SharedJoysFixes.IsValidChair_Prefix));
            PatchFix(harmony, AccessTools.TypeByName("Blues.JoyJobFactory"), "FreeParticipantSlots",
                new[] { typeof(Building), typeof(JoyGiverDef) }, nameof(SharedJoysFixes.FreeParticipantSlots_Prefix));
            PatchFix(harmony, BluesBridge.JoyUtilType, "Notify",
                new[] { typeof(string), typeof(LookTargets) }, nameof(Notify_Prefix), typeof(HarmonyPatches));
            SharedJoysFixes.ReportReflectionHealth();

            if (patched < 3)
                Log.Warning($"[Shared Joys+] {patched}/3 hooks applied. Shared Joys has most likely changed shape: " +
                            "the extended buildings (art, graves, meditation foci) will stay out of reach.");
            else
                // Always log success, not only failure: a silent mod is indistinguishable from
                // a broken one, and this is the only evidence obtainable without playing.
                Log.Message($"[Shared Joys+] 3/3 hooks applied, {PlacelessActivities.All.Count} placeless activities found: " +
                            string.Join(", ", PlacelessActivities.All.ConvertAll(d => d.defName).ToArray()));
        }

        /// <summary>
        /// Applies a fix as a prefix. Its absence is not a failure of the mod: the bug it targets may
        /// already be fixed upstream, or the method renamed. We say so, and carry on.
        /// </summary>
        static void PatchFix(Harmony harmony, System.Type type, string methodName, System.Type[] args,
                             string prefixName, System.Type patchHost = null)
        {
            MethodInfo original = type == null ? null : AccessTools.Method(type, methodName, args);
            if (original == null)
            {
                Log.Message($"[Shared Joys+] fix skipped, method not found: {methodName}. " +
                            "Shared Joys may have fixed it upstream.");
                return;
            }
            harmony.Patch(original, prefix: new HarmonyMethod(patchHost ?? typeof(SharedJoysFixes), prefixName));
        }

        static int Patch(Harmony harmony, string methodName, System.Type[] args, string postfixName)
        {
            MethodInfo original = AccessTools.Method(BluesBridge.JoyUtilType, methodName, args);
            if (original == null)
            {
                Log.Warning($"[Shared Joys+] method not found: Blues.JoyUtil.{methodName}");
                return 0;
            }
            harmony.Patch(original, postfix: new HarmonyMethod(typeof(HarmonyPatches), postfixName));
            return 1;
        }

        static bool Enabled => SharedJoysPlusMod.Settings != null && SharedJoysPlusMod.Settings.extendBuildings;

        /// <summary>Makes art, graves and meditation foci acceptable as recreation spots.</summary>
        public static void IsValidJoyBuilding_Postfix(Building b, bool manual, ref bool __result)
        {
            if (__result || !Enabled || b == null || !b.Spawned) return;
            if (ExtendedBuildingJoy.KindOf(b) == ExtJoyKind.None) return;
            // Same guard as the original: outside a manual invitation, never in a prison.
            if (!manual && ExtendedBuildingJoy.IsPrison(b.GetRoom())) return;
            __result = true;
        }

        /// <summary>Builds the job when no JoyGiverDef names this building.</summary>
        public static void MakeJoyJob_Postfix(Pawn pawn, Building b, List<LocalTargetInfo> takenSpots, ref Job __result)
        {
            if (__result != null || pawn == null || b == null) return;
            if (Enabled) __result = ExtendedBuildingJoy.TryMakeJob(pawn, b, takenSpots);
            // Boards vanilla refuses for want of a chair: they are played on the ground instead,
            // up to joyMaxParticipants. Does not depend on the extended-buildings setting,
            // since these are base game boards.
            if (__result == null) __result = GroundSeating.TryMake(pawn, b, takenSpots);
            // The diagnosis does not depend on the extension either: the seating cases concern
            // base game boards, even when our extended buildings are switched off.
            if (__result == null) FailureDiagnosis.Note(pawn, b);
        }

        /// <summary>
        /// Replaces Shared Joys' generic failure message with the real reason when we know it.
        /// <c>Notify</c> is only ever called on his manual paths, so a failed autonomous event
        /// stays as quiet as before.
        /// </summary>
        public static void Notify_Prefix(ref string text)
        {
            FailureDiagnosis.TryTakeOver(ref text);
        }

        /// <summary>Reports the spot count, without which autonomous events ignore the place.</summary>
        public static void GetAvailableSpots_Postfix(Building b, Pawn probe, ref int __result)
        {
            if (__result > 0 || !Enabled || b == null) return;
            __result = ExtendedBuildingJoy.CountSpots(b, probe);
        }
    }
}
