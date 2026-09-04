using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace SharedJoysPlus
{
    /// <summary>
    /// "Invite X to an activity..." on a colonist's float menu. It is the only possible entry point
    /// for recreation that has no building: there is nothing on the map to click.
    /// This provider does not derive from Shared Joys' own: it works even without that mod.
    /// </summary>
    public class InviteToActivityProvider : FloatMenuOptionProvider
    {
        protected override bool Drafted => false;
        protected override bool Undrafted => true;
        protected override bool Multiselect => true;

        static SharedJoysPlusSettings Settings => SharedJoysPlusMod.Settings;

        public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
        {
            if (Settings == null || !Settings.enableActivityMenu) return false;
            if (!BluesBridge.EnableMenus) return false;
            if (!IsUsableColonist(pawn)) return false;
            return base.SelectedPawnValid(pawn, context);
        }

        public override bool TargetPawnValid(Pawn pawn, FloatMenuContext context)
        {
            if (!IsUsableColonist(pawn)) return false;
            if (pawn.HostileTo(Faction.OfPlayer)) return false;
            return base.TargetPawnValid(pawn, context);
        }

        static bool IsUsableColonist(Pawn pawn)
        {
            if (pawn == null || pawn.needs?.joy == null) return false;
            if (!pawn.IsColonist && !pawn.IsSlaveOfColony) return false;
            if (pawn.DeadOrDowned || !pawn.Awake()) return false;
            if (pawn.jobs?.curJob != null && !pawn.jobs.curJob.def.playerInterruptible) return false;
            return true;
        }

        protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
        {
            Pawn host = context.FirstSelectedPawn;
            if (host == null || clickedPawn == host) return null;

            FloatMenuOption blocked = Blocker(host, clickedPawn);
            if (blocked != null) return blocked;

            List<Pawn> group = BuildGroup(context, clickedPawn);
            return new FloatMenuOption(
                "SJP_InviteToActivity".Translate(clickedPawn.LabelShort),
                () => Find.WindowStack.Add(new FloatMenu(BuildActivityMenu(group))),
                MenuOptionPriority.Low);
        }

        static List<Pawn> BuildGroup(FloatMenuContext context, Pawn clickedPawn)
        {
            List<Pawn> group = new List<Pawn> { context.FirstSelectedPawn };
            foreach (Pawn p in context.ValidSelectedPawns)
                if (!group.Contains(p) && IsUsableColonist(p)) group.Add(p);
            if (!group.Contains(clickedPawn)) group.Add(clickedPawn);
            return group;
        }

        /// <summary>The same refusals Shared Joys applies to a manual invitation.</summary>
        static FloatMenuOption Blocker(Pawn host, Pawn target)
        {
            float max = BluesBridge.MaxRecTrig;
            if (host.needs.joy.CurLevel >= max) return Disabled("BluesAlreadySatisfied", host);
            if (target.needs.joy.CurLevel >= max) return Disabled("BluesAlreadySatisfied", target);

            if (BluesBridge.IsOnHangoutCooldown(target))
                return Disabled("SJP_JustHungOut", target);

            int need = BluesBridge.NeedFren;
            if (host.relations.OpinionOf(target) < need || target.relations.OpinionOf(host) < need)
                return DisabledPair("BluesNotEnoughFren", host, target);

            if (!host.CanReach(target, PathEndMode.Touch, Danger.Deadly))
                return DisabledPair("BluesCantReach", host, target);

            return null;
        }

        static FloatMenuOption Disabled(string key, Pawn p)
        {
            return new FloatMenuOption(key.Translate(p.LabelShort), null) { Disabled = true };
        }

        static FloatMenuOption DisabledPair(string key, Pawn a, Pawn b)
        {
            return new FloatMenuOption(key.Translate(a.LabelShort, b.LabelShort), null) { Disabled = true };
        }

        static List<FloatMenuOption> BuildActivityMenu(List<Pawn> group)
        {
            Pawn host = group[0];
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            foreach (JoyGiverDef d in PlacelessActivities.All)
            {
                if (Settings.IsDisabled(d)) continue;
                if (!PlacelessActivities.CanActuallyStart(d, host)) continue;

                JoyGiverDef giver = d;
                List<Pawn> pawns = group;
                bool anyoneElse = group.Skip(1).Any(p => PlacelessActivities.AvailableFor(giver, p));

                string label = PlacelessActivities.LabelFor(giver);
                if (!anyoneElse)
                {
                    options.Add(new FloatMenuOption("SJP_ActivityUnavailable".Translate(label), null) { Disabled = true });
                    continue;
                }

                options.Add(new FloatMenuOption(label, delegate
                {
                    if (!PlacelessActivities.TryStart(pawns, giver, out string reason) && !reason.NullOrEmpty())
                        Messages.Message(reason, host, MessageTypeDefOf.RejectInput, false);
                }));
            }

            if (options.Count == 0)
                options.Add(new FloatMenuOption("SJP_NoActivity".Translate(), null) { Disabled = true });

            return options;
        }
    }
}
