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

            // Correctifs : ceux-la remplacent un comportement au lieu de le completer, d'ou le
            // prefix, et d'ou l'interrupteur qui leur est propre dans les reglages.
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
                // Toujours journaliser le succes, pas seulement l'echec : un mod muet est
                // indiscernable d'un mod casse, et c'est la seule trace verifiable sans jouer.
                Log.Message($"[Shared Joys+] 3/3 hooks applied, {PlacelessActivities.All.Count} placeless activities found: " +
                            string.Join(", ", PlacelessActivities.All.ConvertAll(d => d.defName).ToArray()));
        }

        /// <summary>
        /// Pose un correctif en prefix. Son absence n'est pas un echec du mod : le bug vise a
        /// peut-etre deja ete corrige en amont, ou la methode renommee. On le dit, et on continue.
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
            if (__result != null || pawn == null || b == null) return;
            if (Enabled) __result = ExtendedBuildingJoy.TryMakeJob(pawn, b, takenSpots);
            // Les plateaux que le vanilla refuse faute de chaise : on les fait jouer par terre,
            // dans la limite de joyMaxParticipants. Ne depend pas du reglage des batiments
            // etendus, il s'agit de plateaux du jeu de base.
            if (__result == null) __result = GroundSeating.TryMake(pawn, b, takenSpots);
            // Le diagnostic ne depend pas de l'extension : le cas de la chaise manquante concerne
            // les plateaux du jeu de base, meme quand nos batiments etendus sont desactives.
            if (__result == null) FailureDiagnosis.Note(pawn, b);
        }

        /// <summary>
        /// Remplace le message d'echec generique de Shared Joys par la vraie raison quand on la
        /// connait. <c>Notify</c> n'est appelee que sur les chemins manuels chez lui, donc un
        /// evenement autonome rate reste silencieux comme avant.
        /// </summary>
        public static void Notify_Prefix(ref string text)
        {
            FailureDiagnosis.TryTakeOver(ref text);
        }

        /// <summary>Annonce le nombre de places, sans quoi les evenements autonomes ignorent le lieu.</summary>
        public static void GetAvailableSpots_Postfix(Building b, Pawn probe, ref int __result)
        {
            if (__result > 0 || !Enabled || b == null) return;
            __result = ExtendedBuildingJoy.CountSpots(b, probe);
        }
    }
}
