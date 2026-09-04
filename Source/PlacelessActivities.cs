using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SharedJoysPlus
{
    /// <summary>
    /// The recreations with no building to click: walks, skygazing, swimming, snowmen, reading,
    /// meditation, prayer, drugs and treats.
    /// Shared Joys could not reach them: its whole interface starts from a building.
    ///
    /// No behaviour is rewritten: the vanilla <c>JoyGiver</c> builds each pawn's job itself. All we
    /// do is ask it for several pawns at once, then bring the destinations closer together where
    /// the activity allows it.
    /// </summary>
    public static class PlacelessActivities
    {
        /// <summary>Activities where everyone follows the host's own route.</summary>
        static readonly HashSet<string> SharesPath = new HashSet<string> { "GoForWalk", "GoSwimming" };

        /// <summary>Activities where each pawn takes a free cell next to the host's.</summary>
        static readonly HashSet<string> SharesSpot = new HashSet<string> { "Skygaze" };

        static List<JoyGiverDef> cache;

        public static List<JoyGiverDef> All
        {
            get
            {
                if (cache == null)
                    cache = DefDatabase<JoyGiverDef>.AllDefsListForReading
                        .Where(IsPlaceless)
                        .OrderBy(d => LabelFor(d))
                        .ToList();
                return cache;
            }
        }

        /// <summary>
        /// A giver counts as placeless as soon as it is not anchored to a building.
        /// The three anchored families are exactly the ones Shared Joys already covers:
        /// <c>JoyGiver_InteractBuilding</c> (chess, billiards, instruments, telescope),
        /// <c>JoyGiver_WatchBuilding</c> (television, horseshoes) and
        /// <c>JoyGiver_SocialRelax</c> (gathering spots).
        /// Filtering on <c>thingDefs</c> would be quite wrong: chocolate has a list of them and is
        /// still a placeless activity.
        /// </summary>
        /// <remarks>
        /// Do not require a <c>jobDef</c> either: <c>TakeDrug</c> and <c>EatChocolate</c> declare
        /// none, because <c>JoyGiver_Ingest.CreateIngestJob</c> builds a <c>JobDefOf.Ingest</c> job
        /// itself. Requiring it meant promising drugs and treats in the description and never
        /// offering them. The field was never needed anyway: the vanilla worker builds the job.
        /// </remarks>
        static bool IsPlaceless(JoyGiverDef d)
        {
            if (d == null || d.giverClass == null) return false;
            if (typeof(JoyGiver_InteractBuilding).IsAssignableFrom(d.giverClass)) return false;
            if (typeof(JoyGiver_WatchBuilding).IsAssignableFrom(d.giverClass)) return false;
            if (typeof(JoyGiver_SocialRelax).IsAssignableFrom(d.giverClass)) return false;
            return true;
        }

        public static string LabelFor(JoyGiverDef d)
        {
            string key = "SJP_Act_" + d.defName;
            if (key.CanTranslate()) return key.Translate();
            if (!d.label.NullOrEmpty()) return d.label;
            if (d.jobDef != null && !d.jobDef.reportString.NullOrEmpty())
                return d.jobDef.reportString.Replace("TargetA", "").Replace(".", "").Trim().CapitalizeFirst();
            return d.defName;
        }

        /// <summary>Can the pawn do it right now? (needs, capacities, tolerance for the type)</summary>
        public static bool AvailableFor(JoyGiverDef d, Pawn pawn)
        {
            JoyGiver worker = d.Worker;
            return worker != null && worker.CanBeGivenTo(pawn);
        }

        /// <summary>
        /// The full check: does the giver really produce a job? <c>CanBeGivenTo</c> only looks at the
        /// pawn, never at the map: prayer asks nothing of him but wants a room of his own or a
        /// worship room, swimming wants water warm enough. Without this second test the menu would
        /// offer entries that fail on click.
        /// None of these givers changes game state while searching.
        /// </summary>
        public static bool CanActuallyStart(JoyGiverDef d, Pawn pawn)
        {
            return AvailableFor(d, pawn) && d.Worker.TryGiveJob(pawn) != null;
        }

        // --- Starting -------------------------------------------------------------------------

        /// <summary>
        /// Puts the whole group on the same activity. The first pawn is the host: if he cannot manage
        /// it, nothing happens. If nobody else follows, the host is cancelled too rather than left
        /// to wander off alone after an invitation was shown.
        /// </summary>
        public static bool TryStart(List<Pawn> group, JoyGiverDef giver, out string reason)
        {
            reason = null;
            if (group.NullOrEmpty() || giver == null) return false;

            Pawn host = group[0];
            float hostJoyBefore = host.needs?.joy?.CurLevel ?? 0f;

            Job hostJob = MakeJobFor(host, giver, null);
            if (hostJob == null || !TakeJob(host, hostJob))
            {
                reason = "SJP_CantDoActivity".Translate(host.LabelShort, LabelFor(giver));
                return false;
            }

            List<Pawn> joined = new List<Pawn>();
            for (int i = 1; i < group.Count; i++)
            {
                Pawn p = group[i];
                Job job = MakeJobFor(p, giver, hostJob);
                if (job != null && TakeJob(p, job)) joined.Add(p);
            }

            if (joined.Count == 0)
            {
                host.jobs?.StopAll(false, true);
                if (host.needs?.joy != null) host.needs.joy.CurLevel = hostJoyBefore;
                reason = "SJP_NobodyJoined".Translate(LabelFor(giver));
                return false;
            }

            foreach (Pawn p in joined) SharedJoyRecord.Record(host, p);
            return true;
        }

        static Job MakeJobFor(Pawn pawn, JoyGiverDef giver, Job hostJob)
        {
            if (pawn?.jobs == null || pawn.needs?.joy == null) return null;
            if (!AvailableFor(giver, pawn)) return null;

            Job job = giver.Worker.TryGiveJob(pawn);
            if (job == null) return null;

            if (hostJob != null) ApplyCohesion(pawn, giver, job, hostJob);
            return job;
        }

        /// <summary>
        /// Brings a guest's destination closer to the host's. Neither walking, nor swimming, nor
        /// skygazing reserves anything at all
        /// (<c>TryMakePreToilReservations</c> returns <c>true</c> there without taking a thing), so
        /// sharing a route cannot produce a reservation conflict.
        /// </summary>
        static void ApplyCohesion(Pawn pawn, JoyGiverDef giver, Job job, Job hostJob)
        {
            if (!hostJob.targetA.IsValid) return;

            if (SharesPath.Contains(giver.defName))
            {
                job.targetA = hostJob.targetA;
                job.targetQueueA = hostJob.targetQueueA == null
                    ? null
                    : new List<LocalTargetInfo>(hostJob.targetQueueA);
                return;
            }

            if (SharesSpot.Contains(giver.defName))
            {
                Map map = pawn.Map;
                IntVec3 root = hostJob.targetA.Cell;
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(root, map, 4,
                    c => c.Standable(map) && !c.IsForbidden(pawn) && !c.Roofed(map)
                         && pawn.CanReserveSittableOrSpot(c));
                if (cell.IsValid) job.targetA = cell;
            }
        }

        static bool TakeJob(Pawn pawn, Job job)
        {
            if (pawn?.jobs == null) return false;
            if (!pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc) || pawn.CurJob != job) return false;

            // Same setting as Shared Joys: recreation is lowered so the moment lasts.
            Need_Joy joy = pawn.needs?.joy;
            if (joy != null) joy.CurLevel = Mathf.Min(joy.CurLevel, BluesBridge.JoyDrainTo);
            return true;
        }
    }
}
