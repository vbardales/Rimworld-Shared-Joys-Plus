using RimWorld;
using Verse;

namespace SharedJoysPlus
{
    /// <summary>
    /// The social record of a shared break. Shared Joys' own defs are reused rather than declaring
    /// more: without that, two competing memories would stack for the same event, and the stack
    /// limit of each would be wrong.
    /// </summary>
    public static class SharedJoyRecord
    {
        static bool resolved;
        static ThoughtDef thought;
        static InteractionDef interaction;

        static void Resolve()
        {
            if (resolved) return;
            resolved = true;
            thought = DefDatabase<ThoughtDef>.GetNamed("Blues_SharedJoy_Thought", false);
            interaction = DefDatabase<InteractionDef>.GetNamed("Blues_InviteHangout", false);
        }

        public static void Record(Pawn initiator, Pawn recipient)
        {
            Resolve();
            if (initiator == null || recipient == null) return;

            if (interaction != null)
            {
                Find.PlayLog.Add(new PlayLogEntry_Interaction(interaction, initiator, recipient, null));
                MoteMaker.MakeInteractionBubble(initiator, recipient, ThingDefOf.Mote_Speech,
                    interaction.GetSymbol(null, null));
            }

            if (thought == null) return;
            initiator.needs?.mood?.thoughts?.memories?.TryGainMemory(thought, recipient);
            recipient.needs?.mood?.thoughts?.memories?.TryGainMemory(thought, initiator);
        }
    }
}
