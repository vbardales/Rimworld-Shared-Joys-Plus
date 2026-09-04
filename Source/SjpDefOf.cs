using RimWorld;
using Verse;

namespace SharedJoysPlus
{
    /// <summary>
    /// Two base game JobDefs that <c>RimWorld.JobDefOf</c> does not expose: their vanilla givers
    /// reach them through <c>def.jobDef</c>, never by name.
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
