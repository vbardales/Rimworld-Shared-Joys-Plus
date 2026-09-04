using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SharedJoysPlus
{
    /// <summary>
    /// Dit pourquoi un moment partage n'a pas pu commencer.
    /// </summary>
    /// <remarks>
    /// Shared Joys n'a qu'un seul message pour tous les echecs de lieu — « Not enough space for
    /// everyone to chill » — et il envoie chercher de la place la ou il manque le plus souvent une
    /// chaise. Notre extension aggrave le probleme : une seule sculpture pour deux colons produit
    /// exactement le meme texte, alors que la vraie raison est qu'une oeuvre ne se contemple qu'a un.
    ///
    /// On ne double pas le message, on le <b>remplace</b> : le diagnostic est calcule quand la
    /// fabrication de la tache echoue, puis substitue dans <c>JoyUtil.Notify</c>. Cette methode
    /// n'est appelee que sur les chemins manuels — tous ses appels chez Blues sont gardes par
    /// <c>if (manual)</c> — donc un evenement autonome rate ne dira jamais rien.
    /// </remarks>
    public static class FailureDiagnosis
    {
        static string pendingReason;
        static int pendingTick = -1;

        /// <summary>
        /// Calcule et retient la raison de l'echec pour ce batiment. Appele depuis le postfix de
        /// <c>MakeJoyJob</c>, donc une fois par participant : le dernier a parler gagne, ce qui est
        /// le bon choix — c'est celui qui n'a pas pu suivre.
        /// </summary>
        public static void Note(Pawn pawn, Building b)
        {
            pendingReason = Diagnose(pawn, b);
            pendingTick = Find.TickManager?.TicksGame ?? -1;
        }

        /// <summary>
        /// Substitue notre raison a celle de Shared Joys, si elle a ete calculee au meme tic. La
        /// borne de temps evite qu'un diagnostic oublie ne vienne coiffer un message sans rapport,
        /// « untel ne peut pas atteindre untel » par exemple.
        /// </summary>
        public static bool TryTakeOver(ref string text)
        {
            if (pendingReason == null) return false;

            bool sameTick = pendingTick == (Find.TickManager?.TicksGame ?? -1);
            string reason = pendingReason;
            pendingReason = null;
            pendingTick = -1;

            if (!sameTick) return false;
            text = reason;
            return true;
        }

        static string Diagnose(Pawn pawn, Building b)
        {
            if (pawn == null || b == null || !b.Spawned) return null;

            switch (ExtendedBuildingJoy.KindOf(b))
            {
                case ExtJoyKind.Art:
                    // Une cible existerait si rien n'etait pris : c'est donc que tout est pris.
                    return ExtendedBuildingJoy.HasAnyTarget(pawn, b)
                        ? "SJP_ArtAllTaken".Translate(b.LabelShort).ToString()
                        : null;

                case ExtJoyKind.Grave:
                    return ExtendedBuildingJoy.HasAnyTarget(pawn, b)
                        ? "SJP_GraveAllTaken".Translate(b.LabelShort).ToString()
                        : null;

                case ExtJoyKind.Meditation:
                    return "SJP_NoMeditationSpot".Translate(b.LabelShort).ToString();
            }

            return DiagnoseSeating(pawn, b);
        }

        /// <summary>
        /// Le cas qui trompe le plus : les plateaux et les televisions veulent une chaise, et le
        /// vanilla ne s'en cache pas — <c>requireChair</c> vaut vrai par defaut et aucun
        /// <c>JoyGiverDef</c> du jeu de base ne le passe a faux. Sans chaise cardinale libre,
        /// <c>JoyGiver_InteractBuildingSitAdjacent.TryGivePlayJob</c> sort avant meme d'essayer le
        /// sol.
        /// </summary>
        static string DiagnoseSeating(Pawn pawn, Building b)
        {
            JoyGiverDef giver = GroundSeating.GiverFor(b.def);
            if (giver == null || giver.giverClass == null) return null;

            // Le plafond d'abord : c'est la raison la plus frequente, et la seule qu'aucune
            // disposition de la piece ne peut lever. Shared Joys calcule les places libres comme
            // joyMaxParticipants moins les reservations posees, si bien qu'un meuble prevu pour
            // deux refuse le troisieme avec le meme « pas assez de place » qu'une piece sans
            // chaises. On ne parle que quand les places sont vraiment prises, pour qu'un vrai
            // manque d'espace garde son propre message.
            int seats = giver.jobDef != null ? giver.jobDef.joyMaxParticipants : 0;
            if (seats > 0 && TakenSlots(b, giver) >= seats)
                return "SJP_SeatsOnly".Translate(b.LabelShort, seats).ToString();

            // Plus de message de chaise pour les plateaux : GroundSeating les fait jouer par terre.
            // S'ils echouent encore, c'est qu'aucune des quatre cases cardinales n'est libre.
            if (typeof(JoyGiver_InteractBuildingSitAdjacent).IsAssignableFrom(giver.giverClass))
                return "SJP_NoSpotBeside".Translate(b.LabelShort).ToString();

            if (typeof(JoyGiver_WatchBuilding).IsAssignableFrom(giver.giverClass)
                && giver.desireSit
                && !HasFreeChairAmong(pawn, WatchBuildingUtility.CalculateWatchCells(b.def, b.Position, b.Rotation, b.Map), b.Map))
                return "SJP_NeedsChairToWatch".Translate(b.LabelShort).ToString();

            return null;
        }

        /// <summary>
        /// How many places on this building are already spoken for, counted the same way
        /// <see cref="SharedJoysFixes.FreeParticipantSlots_Prefix"/> counts them: only the
        /// reservations whose job is this recreation, never a hauler or a cleaner passing through.
        /// </summary>
        static int TakenSlots(Building b, JoyGiverDef giver)
        {
            if (b?.Map == null || giver?.jobDef == null) return 0;

            int taken = 0;
            foreach (ReservationManager.Reservation reservation in b.Map.reservationManager.ReservationsReadOnly)
            {
                if (reservation.Target.Thing != b) continue;
                if (reservation.Job == null || reservation.Job.def != giver.jobDef) continue;
                taken++;
            }
            return taken;
        }

        static bool HasFreeChairAmong(Pawn pawn, IEnumerable<IntVec3> cells, Map map)
        {
            if (map == null) return false;
            foreach (IntVec3 cell in cells)
            {
                if (!cell.InBounds(map) || cell.IsForbidden(pawn)) continue;
                Building edifice = cell.GetEdifice(map);
                if (edifice == null || !edifice.def.building.isSittable) continue;
                if (!pawn.CanReserveSittableOrSpot(cell)) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Le <c>JoyGiverDef</c> qui sert ce <c>ThingDef</c>, s'il y en a un. Meme recherche que
        /// celle de Shared Joys, refaite ici plutot que lue chez lui : son cache est prive, et une
        /// dependance de plus sur sa forme interne ne vaut pas les quelques lignes economisees.
        /// </summary>
        static JoyGiverDef GiverFor(ThingDef def)
        {
            List<JoyGiverDef> all = DefDatabase<JoyGiverDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                List<ThingDef> things = all[i].thingDefs;
                if (things != null && things.Contains(def)) return all[i];
            }
            return null;
        }
    }
}
