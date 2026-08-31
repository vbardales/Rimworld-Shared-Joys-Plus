using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SharedJoysPlus
{
    public class SharedJoysPlusSettings : ModSettings
    {
        public bool extendBuildings = true;
        public bool enableActivityMenu = true;
        public List<string> disabledActivities = new List<string>();

        public bool IsDisabled(JoyGiverDef d) => disabledActivities.Contains(d.defName);

        public void SetDisabled(JoyGiverDef d, bool disabled)
        {
            if (disabled)
            {
                if (!disabledActivities.Contains(d.defName)) disabledActivities.Add(d.defName);
            }
            else disabledActivities.Remove(d.defName);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref extendBuildings, "extendBuildings", true);
            Scribe_Values.Look(ref enableActivityMenu, "enableActivityMenu", true);
            Scribe_Collections.Look(ref disabledActivities, "disabledActivities", LookMode.Value);
            if (disabledActivities == null) disabledActivities = new List<string>();
        }
    }

    public class SharedJoysPlusMod : Mod
    {
        public static SharedJoysPlusSettings Settings;

        Vector2 scroll;

        public SharedJoysPlusMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<SharedJoysPlusSettings>();
        }

        public override string SettingsCategory() => "SJP_ModTitle".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            if (!BluesBridge.Present)
            {
                GUI.color = Color.yellow;
                listing.Label("SJP_SharedJoysMissing".Translate());
                GUI.color = Color.white;
                listing.GapLine();
            }

            listing.CheckboxLabeled("SJP_ExtendBuildings".Translate(), ref Settings.extendBuildings,
                "SJP_ExtendBuildingsDesc".Translate());
            listing.CheckboxLabeled("SJP_EnableActivityMenu".Translate(), ref Settings.enableActivityMenu,
                "SJP_EnableActivityMenuDesc".Translate());

            listing.GapLine();
            listing.Label("SJP_ActivityList".Translate());

            List<JoyGiverDef> activities = PlacelessActivities.All;
            Rect listRect = listing.GetRect(inRect.height - listing.CurHeight - 12f);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 20f, activities.Count * 26f);

            Widgets.BeginScrollView(listRect, ref scroll, viewRect);
            float y = 0f;
            foreach (JoyGiverDef d in activities)
            {
                Rect row = new Rect(0f, y, viewRect.width, 24f);
                bool enabled = !Settings.IsDisabled(d);
                bool before = enabled;
                Widgets.CheckboxLabeled(row, PlacelessActivities.LabelFor(d), ref enabled);
                if (enabled != before) Settings.SetDisabled(d, !enabled);
                y += 26f;
            }
            Widgets.EndScrollView();

            listing.End();
        }
    }
}
