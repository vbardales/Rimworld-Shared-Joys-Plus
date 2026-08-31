# Journal des modifications

Format inspiré de [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/).
Ce fichier sert au dépôt et à rédiger les notes de version Steam ; RimWorld ne l'affiche pas en jeu.

## [1.0.0] — non publié

Première version. RimWorld 1.6. Mod compagnon de **Shared Joys** de Blues
([3719496210](https://steamcommunity.com/sharedfiles/filedetails/?id=3719496210)).

### Bâtiments que Shared Joys refusait

- Les œuvres d'art dont le `CompArt` porte `canBeEnjoyedAsArt` — les sculptures, en vanilla.
- Les foyers de méditation : spot, trône, arbre anima, et tout bâtiment portant un
  `CompMeditationFocus`. Royalty uniquement.
- Les tombes occupées par un colon.

Ils apparaissent dans « Relax at … », dans les invitations et dans les événements autonomes.
`JobDriver_VisitJoyThing` réservant sa cible pour un seul pion, un groupe se répartit sur les
pièces voisines de la même salle, dans un rayon de 12 cases. La méditation est le seul cas où le
foyer est réellement partagé : seule la case d'assise change.

### Activités sans lieu

- Nouvelle entrée de menu contextuel sur un colon : « Inviter … à une activité », qui liste ce qui
  est réellement possible à cet instant. Fonctionne avec plusieurs colons sélectionnés.
- Couvre promenade, contemplation du ciel, baignade, bonhomme de neige, lecture, méditation,
  prière, substances et friandises.
- La liste est déduite au chargement : est « sans lieu » tout `JoyGiverDef` dont le `giverClass`
  ne dérive ni de `JoyGiver_InteractBuilding`, ni de `JoyGiver_WatchBuilding`, ni de
  `JoyGiver_SocialRelax`. Un mod tiers apparaît donc sans patch.
- Promenades et baignades reprennent le trajet de l'hôte ; la contemplation du ciel prend une case
  libre et découverte à côté de la sienne.

### Réglages

- Deux interrupteurs : bâtiments étendus, menu d'activités.
- Une case par activité, pour retirer de la liste ce qu'on ne veut pas voir proposer.
- Les seuils (opinion minimale, niveau de loisir maximal, purge du loisir) sont lus dans les
  réglages de Shared Joys. Rien à régler deux fois.

### Notes techniques

- Trois greffes Harmony en postfix sur `Blues.JoyUtil` : `IsValidJoyBuilding`, `MakeJoyJob`,
  `GetAvailableSpots`. Toutes passives — elles ne s'expriment que là où l'original a renoncé.
- Aucune référence de compilation vers l'assemblage de Blues : tout passe par `AccessTools`. Le
  mod se charge sans lui, le menu d'activités reste utilisable seul, et une mise à jour qui déplace
  une méthode produit un avertissement de journal au lieu d'un échec de chargement.
- Les defs `Blues_SharedJoy_Thought` et `Blues_InviteHangout` sont réutilisées, jamais redéclarées :
  deux pensées concurrentes fausseraient leurs `stackLimit`.
- Interface en anglais et en français, 25 clés alignées.
