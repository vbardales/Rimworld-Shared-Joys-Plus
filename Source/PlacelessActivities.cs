using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SharedJoysPlus
{
    /// <summary>
    /// Les loisirs qui n'ont aucun batiment a cliquer : promenade, contemplation du ciel,
    /// baignade, bonhomme de neige, lecture, meditation, priere, drogues et friandises.
    /// Shared Joys ne pouvait pas les atteindre — toute son interface part d'un batiment.
    ///
    /// On ne reecrit aucun comportement : c'est le <c>JoyGiver</c> vanilla lui-meme qui fabrique
    /// la tache de chaque pion. On se contente de la demander pour plusieurs pions a la fois, et
    /// de rapprocher les destinations quand l'activite s'y prete.
    /// </summary>
    public static class PlacelessActivities
    {
        /// <summary>Activites ou tout le monde suit le meme trajet que l'hote.</summary>
        static readonly HashSet<string> SharesPath = new HashSet<string> { "GoForWalk", "GoSwimming" };

        /// <summary>Activites ou chacun prend une case libre a cote de celle de l'hote.</summary>
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
        /// Un fournisseur est « sans lieu » des lors qu'il n'est pas ancre sur un batiment.
        /// Les trois familles ancrees sont exactement celles que Shared Joys couvre deja :
        /// <c>JoyGiver_InteractBuilding</c> (echecs, billard, instruments, telescope),
        /// <c>JoyGiver_WatchBuilding</c> (television, fers a cheval) et
        /// <c>JoyGiver_SocialRelax</c> (points de rassemblement).
        /// On ne filtre surtout pas sur <c>thingDefs</c> : le chocolat en a une liste, et reste
        /// une activite sans lieu.
        /// </summary>
        static bool IsPlaceless(JoyGiverDef d)
        {
            if (d == null || d.jobDef == null || d.giverClass == null) return false;
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

        /// <summary>Le pion peut-il s'y mettre a l'instant ? (besoins, capacites, tolerance au type)</summary>
        public static bool AvailableFor(JoyGiverDef d, Pawn pawn)
        {
            JoyGiver worker = d.Worker;
            return worker != null && worker.CanBeGivenTo(pawn);
        }

        /// <summary>
        /// Verification complete : le fournisseur produit-il vraiment une tache ? <c>CanBeGivenTo</c>
        /// ne regarde que le pion, pas la carte — la priere n'exige rien de lui mais demande une
        /// chambre a lui ou une salle de culte, la baignade demande de l'eau assez chaude. Sans ce
        /// second test, le menu proposerait des entrees qui echouent au clic.
        /// Aucun de ces fournisseurs ne modifie l'etat du jeu quand il cherche.
        /// </summary>
        public static bool CanActuallyStart(JoyGiverDef d, Pawn pawn)
        {
            return AvailableFor(d, pawn) && d.Worker.TryGiveJob(pawn) != null;
        }

        // --- Lancement -------------------------------------------------------------------------

        /// <summary>
        /// Met tout le groupe a la meme activite. Le premier pion est l'hote : si lui n'y arrive
        /// pas, rien ne se passe. Si personne d'autre ne suit, on annule aussi l'hote plutot que
        /// de le laisser partir seul apres avoir affiche une invitation.
        /// </summary>
        public static bool TryStart(List<Pawn> group, JoyGiverDef giver, out string reason)
        {
            reason = null;
            if (group.NullOrEmpty() || giver == null || giver.jobDef == null) return false;

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
        /// Rapproche la destination d'un invite de celle de l'hote. Ni la promenade, ni la
        /// baignade, ni la contemplation du ciel ne reservent quoi que ce soit
        /// (<c>TryMakePreToilReservations</c> y renvoie <c>true</c> sans rien prendre), donc
        /// partager un trajet ne peut pas produire de conflit de reservation.
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

            // Meme reglage que Shared Joys : on redescend le loisir pour que le moment dure.
            Need_Joy joy = pawn.needs?.joy;
            if (joy != null) joy.CurLevel = Mathf.Min(joy.CurLevel, BluesBridge.JoyDrainTo);
            return true;
        }
    }
}
