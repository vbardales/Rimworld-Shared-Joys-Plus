using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SharedJoysPlus
{
    /// <summary>
    /// Trois greffes sur <c>Blues.JoyUtil</c>, toutes en postfix et toutes passives : elles ne
    /// s'expriment que la ou Shared Joys a deja renonce (resultat faux, tache nulle, zero place).
    /// Rien de son comportement d'origine n'est modifie.
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

            if (patched < 3)
                Log.Warning($"[Shared Joys+] {patched}/3 hooks applied. Shared Joys has most likely changed shape: " +
                            "the extended buildings (art, graves, meditation foci) will stay out of reach.");
            else
                // Toujours journaliser le succes, pas seulement l'echec : un mod muet est
                // indiscernable d'un mod casse, et c'est la seule trace verifiable sans jouer.
                Log.Message($"[Shared Joys+] 3/3 hooks applied, {PlacelessActivities.All.Count} placeless activities found: " +
                            string.Join(", ", PlacelessActivities.All.ConvertAll(d => d.defName).ToArray()));
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

        /// <summary>Fait accepter comme lieu de loisir l'art, les tombes et les foyers de meditation.</summary>
        public static void IsValidJoyBuilding_Postfix(Building b, bool manual, ref bool __result)
        {
            if (__result || !Enabled || b == null || !b.Spawned) return;
            if (ExtendedBuildingJoy.KindOf(b) == ExtJoyKind.None) return;
            // Meme garde que l'original : hors invitation manuelle, jamais en prison.
            if (!manual && ExtendedBuildingJoy.IsPrison(b.GetRoom())) return;
            __result = true;
        }

        /// <summary>Fabrique la tache quand aucun JoyGiverDef ne cite ce batiment.</summary>
        public static void MakeJoyJob_Postfix(Pawn pawn, Building b, List<LocalTargetInfo> takenSpots, ref Job __result)
        {
            if (__result != null || !Enabled || pawn == null || b == null) return;
            __result = ExtendedBuildingJoy.TryMakeJob(pawn, b, takenSpots);
        }

        /// <summary>Annonce le nombre de places, sans quoi les evenements autonomes ignorent le lieu.</summary>
        public static void GetAvailableSpots_Postfix(Building b, Pawn probe, ref int __result)
        {
            if (__result > 0 || !Enabled || b == null) return;
            __result = ExtendedBuildingJoy.CountSpots(b, probe);
        }
    }
}
