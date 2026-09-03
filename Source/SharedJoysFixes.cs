using RimWorld;
using Verse;
using Verse.AI;

namespace SharedJoysPlus
{
    /// <summary>
    /// Corrections de bugs de Shared Joys, par-dessus l'extension.
    /// </summary>
    /// <remarks>
    /// A la difference des trois greffes de <see cref="HarmonyPatches"/>, qui sont passives et ne
    /// parlent que la ou l'original a renonce, celles-ci <b>remplacent</b> un comportement. Elles
    /// sont donc toutes coupables depuis les reglages, et chacune porte ici la raison pour laquelle
    /// le comportement d'origine est faux — pas seulement ce qu'elle fait a la place.
    /// </remarks>
    public static class SharedJoysFixes
    {
        /// <summary>
        /// Les greffes sont toujours posees, et c'est ici qu'on decide de s'exprimer ou non : un
        /// reglage qui ne s'appliquerait qu'au redemarrage serait invivable a l'usage.
        /// </summary>
        static bool Enabled => SharedJoysPlusMod.Settings == null || SharedJoysPlusMod.Settings.fixBugs;

        /// <summary>
        /// <c>JoyUtil.IsValidChair</c> reconnait un siege par
        /// <c>defName.ToLower().Contains("bench")</c> ou <c>"seat"</c>, en plus de
        /// <c>building.isSittable</c>. C'est un OU, donc l'heuristique n'elargit rien d'utile : elle
        /// ne fait qu'ajouter des faux positifs. En vanilla elle capture deja
        /// <c>HandTailoringBench</c>, <c>ElectricTailoringBench</c>, <c>SimpleResearchBench</c> et
        /// <c>HiTechResearchBench</c> — un pion envoye « s'asseoir » sur un etabli de recherche.
        ///
        /// Rien n'est perdu en la retirant : un meuble sur lequel le jeu sait asseoir un pion porte
        /// <c>isSittable</c>, sans quoi <c>CanReserveSittableOrSpot</c> le refuserait de toute facon.
        /// </summary>
        public static bool IsValidChair_Prefix(Building chair, Pawn pawn, ref bool __result)
        {
            if (!Enabled) return true;
            __result = chair != null
                       && chair.def.building != null
                       && chair.def.building.isSittable
                       && pawn.CanReserveAndReach(chair, PathEndMode.Touch, Danger.Some);
            return false;
        }

        /// <summary>
        /// <c>JoyJobFactory.FreeParticipantSlots</c> compte <b>toutes</b> les reservations posees sur
        /// le batiment, quelle que soit la tache, et les soustrait de <c>joyMaxParticipants</c>. Un
        /// porteur qui vient prendre un objet pose dessus, un constructeur qui repare, un nettoyeur :
        /// chacun fait paraitre la table pleine et interdit le moment partage.
        ///
        /// On ne compte donc que les reservations dont la tache est bien celle de ce loisir.
        /// </summary>
        public static bool FreeParticipantSlots_Prefix(Building b, JoyGiverDef giver, ref int __result)
        {
            // Desactive, ou hors de nos hypotheses : on laisse l'original s'exprimer.
            if (!Enabled) return true;
            if (b == null || b.Map == null || giver == null || giver.jobDef == null) return true;

            int taken = 0;
            foreach (ReservationManager.Reservation reservation in b.Map.reservationManager.ReservationsReadOnly)
            {
                if (reservation.Target.Thing != b) continue;
                if (reservation.Job == null || reservation.Job.def != giver.jobDef) continue;
                taken++;
            }

            __result = giver.jobDef.joyMaxParticipants - taken;
            return false;
        }

        /// <summary>
        /// <c>JoyJobFactory</c> garde en champs statiques les <c>MethodInfo</c> de deux methodes
        /// <b>privees</b> de <c>JoyGiver_InteractBuilding</c> : <c>CanInteractWith</c> et
        /// <c>TryGivePlayJob</c>. Si une version de RimWorld les renomme, <c>HasOwnWorker</c> renvoie
        /// simplement faux et Shared Joys se rabat sur le chemin « regarder le batiment » — les
        /// echecs, le poker et les instruments deviennent silencieusement des televisions.
        ///
        /// Rien ne peut reparer ca depuis l'exterieur : notre propre recherche echouerait pour la
        /// meme raison. Mais on peut au moins transformer une panne muette en une ligne de journal,
        /// ce qui est toute la difference entre un bug rapporte par un joueur et un bug vu au
        /// chargement.
        /// </summary>
        public static void ReportReflectionHealth()
        {
            if (BluesBridge.SettingsType == null) return;

            bool canInteract = BluesBridge.PrivateStaticFieldIsSet("Blues.JoyJobFactory", "canInteractWith");
            bool tryGivePlay = BluesBridge.PrivateStaticFieldIsSet("Blues.JoyJobFactory", "tryGivePlayJob");
            if (canInteract && tryGivePlay) return;

            Log.Warning("[Shared Joys+] Shared Joys could not find JoyGiver_InteractBuilding."
                        + (canInteract ? "" : " CanInteractWith")
                        + (tryGivePlay ? "" : " TryGivePlayJob")
                        + " - those are private vanilla methods, and this RimWorld version seems to "
                        + "have renamed them. Chess, poker, billiards and instruments will fall back "
                        + "to the watch-building path instead of their own.");
        }
    }
}
