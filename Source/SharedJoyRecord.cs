using RimWorld;
using Verse;

namespace SharedJoysPlus
{
    /// <summary>
    /// Journal social d'un moment partage. On reutilise les defs de Shared Joys plutot que d'en
    /// declarer d'autres : sans ca, deux pensees concurrentes s'empileraient pour le meme
    /// evenement, et le plafond de pile de chacune serait faux.
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
