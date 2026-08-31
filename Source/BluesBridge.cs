using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SharedJoysPlus
{
    /// <summary>
    /// Tout ce qui touche a l'assemblage de Blues passe par ici, et uniquement par reflexion.
    /// Aucune reference de compilation : si Shared Joys disparait, change de nom de type ou de
    /// signature, ce mod continue de se charger, et seules les fonctions qui en dependent
    /// s'eteignent.
    /// </summary>
    public static class BluesBridge
    {
        public const string HarmonyId = "nelim.sharedjoysplus";

        public static readonly Type JoyUtilType = AccessTools.TypeByName("Blues.JoyUtil");
        public static readonly Type SettingsType = AccessTools.TypeByName("Blues.JoyousSet");

        static readonly MethodInfo isOnCooldownMethod = Method(JoyUtilType, "IsOnHangoutCooldown", typeof(Pawn));
        static readonly MethodInfo isBusyMethod = Method(JoyUtilType, "IsBusy", typeof(Pawn));
        static readonly MethodInfo isUnderLordMethod = Method(JoyUtilType, "IsUnderLord", typeof(Pawn));

        static readonly MethodInfo joyDrainToGetter = Getter("JoyDrainTo");
        static readonly MethodInfo maxRecTrigGetter = Getter("MaxRecTrig");
        static readonly MethodInfo needFrenGetter = Getter("NeedFren");
        static readonly FieldInfo enableMenusField = AccessTools.Field(SettingsType, "enableMenus");

        /// <summary>Shared Joys est-il charge et reconnaissable ?</summary>
        public static bool Present => JoyUtilType != null;

        static MethodInfo Method(Type type, string name, params Type[] args)
        {
            return type == null ? null : AccessTools.Method(type, name, args);
        }

        static MethodInfo Getter(string propertyName)
        {
            return SettingsType == null ? null : AccessTools.PropertyGetter(SettingsType, propertyName);
        }

        // --- Reglages de Shared Joys, avec les valeurs par defaut du mod en repli -------------

        /// <summary>Niveau auquel le loisir est ramene avant un moment partage.</summary>
        public static float JoyDrainTo => joyDrainToGetter != null ? (float)joyDrainToGetter.Invoke(null, null) : 0.6f;

        /// <summary>Au-dessus de ce niveau de loisir, un pion decline une invitation manuelle.</summary>
        public static float MaxRecTrig => maxRecTrigGetter != null ? (float)maxRecTrigGetter.Invoke(null, null) : 0.95f;

        /// <summary>Opinion minimale exigee entre les deux pions.</summary>
        public static int NeedFren => needFrenGetter != null ? (int)needFrenGetter.Invoke(null, null) : 30;

        public static bool EnableMenus => enableMenusField == null || (bool)enableMenusField.GetValue(null);

        // --- Predicats partages ---------------------------------------------------------------

        public static bool IsOnHangoutCooldown(Pawn p)
        {
            return isOnCooldownMethod != null && (bool)isOnCooldownMethod.Invoke(null, new object[] { p });
        }

        public static bool IsBusy(Pawn p)
        {
            if (isBusyMethod != null) return (bool)isBusyMethod.Invoke(null, new object[] { p });
            // Repli minimal, aligne sur ce que fait Shared Joys.
            if (p.Drafted || p.InMentalState || p.IsBurning()) return true;
            var job = p.jobs?.curJob;
            if (job == null) return false;
            return job.playerForced || !p.jobs.IsCurrentJobPlayerInterruptible();
        }

        public static bool IsUnderLord(Pawn p)
        {
            if (isUnderLordMethod != null) return (bool)isUnderLordMethod.Invoke(null, new object[] { p });
            return Verse.AI.Group.LordUtility.GetLord(p) != null;
        }
    }
}
