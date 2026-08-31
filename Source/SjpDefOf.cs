using RimWorld;
using Verse;

namespace SharedJoysPlus
{
    /// <summary>
    /// Deux JobDef du jeu de base que <c>RimWorld.JobDefOf</c> n'expose pas : leurs fournisseurs
    /// vanilla les prennent par <c>def.jobDef</c>, jamais par nom.
    /// </summary>
    [DefOf]
    public static class SjpDefOf
    {
        public static JobDef ViewArt;
        public static JobDef VisitGrave;

        static SjpDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SjpDefOf));
        }
    }
}
