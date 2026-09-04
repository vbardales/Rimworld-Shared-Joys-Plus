using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SharedJoysPlus
{
    /// <summary>
    /// Everything that touches Blues' assembly goes through here, and through reflection only.
    /// No compile-time reference: if Shared Joys disappears, renames a type or changes a
    /// signature, this mod still loads and only the features that depend on it go quiet.
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

        /// <summary>Is Shared Joys loaded and recognisable?</summary>
        public static bool Present => JoyUtilType != null;

        static MethodInfo Method(Type type, string name, params Type[] args)
        {
            return type == null ? null : AccessTools.Method(type, name, args);
        }

        static MethodInfo Getter(string propertyName)
        {
            return SettingsType == null ? null : AccessTools.PropertyGetter(SettingsType, propertyName);
        }

        // --- Shared Joys' settings, with this mod's own defaults as a fallback ------------------

        /// <summary>The level recreation is brought down to before a shared break.</summary>
        public static float JoyDrainTo => joyDrainToGetter != null ? (float)joyDrainToGetter.Invoke(null, null) : 0.6f;

        /// <summary>Above this recreation level, a pawn turns a manual invitation down.</summary>
        public static float MaxRecTrig => maxRecTrigGetter != null ? (float)maxRecTrigGetter.Invoke(null, null) : 0.95f;

        /// <summary>The minimum opinion required between the two pawns.</summary>
        public static int NeedFren => needFrenGetter != null ? (int)needFrenGetter.Invoke(null, null) : 30;

        public static bool EnableMenus => enableMenusField == null || (bool)enableMenusField.GetValue(null);

        // --- Shared predicates -----------------------------------------------------------------

        public static bool IsOnHangoutCooldown(Pawn p)
        {
            return isOnCooldownMethod != null && (bool)isOnCooldownMethod.Invoke(null, new object[] { p });
        }

        public static bool IsBusy(Pawn p)
        {
            if (isBusyMethod != null) return (bool)isBusyMethod.Invoke(null, new object[] { p });
            // Minimal fallback, matching what Shared Joys does.
            if (p.Drafted || p.InMentalState || p.IsBurning()) return true;
            var job = p.jobs?.curJob;
            if (job == null) return false;
            return job.playerForced || !p.jobs.IsCurrentJobPlayerInterruptible();
        }

        /// <summary>
        /// Is one of Shared Joys' private static fields set? Used for the health check on the
        /// <c>MethodInfo</c>s it caches: a null field there means a vanilla method it could not
        /// find, and therefore a feature switched off without a word.
        /// </summary>
        public static bool PrivateStaticFieldIsSet(string typeName, string fieldName)
        {
            Type type = AccessTools.TypeByName(typeName);
            FieldInfo field = type == null ? null : AccessTools.Field(type, fieldName);
            // Field gone: the class has changed shape, which is not ours to judge here.
            if (field == null) return true;
            return field.GetValue(null) != null;
        }

        public static bool IsUnderLord(Pawn p)
        {
            if (isUnderLordMethod != null) return (bool)isUnderLordMethod.Invoke(null, new object[] { p });
            return Verse.AI.Group.LordUtility.GetLord(p) != null;
        }
    }
}
