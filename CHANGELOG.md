# Journal des modifications

Format inspiré de [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/).
Ce fichier sert au dépôt et à rédiger les notes de version Steam ; RimWorld ne l'affiche pas en jeu.

## [1.0.0] - non publié

Première version. RimWorld 1.6. Mod compagnon de **Shared Joys** de Blues
([3719496210](https://steamcommunity.com/sharedfiles/filedetails/?id=3719496210)).

Shared Joys n'accepte un bâtiment comme lieu de loisir qu'à deux conditions : un `JoyGiverDef` cite
son `ThingDef` dans `thingDefs`, ou il porte un `CompGatherSpot` actif. Or **9 des 23 `JoyGiverDef`
de 1.6 seulement remplissent `thingDefs`** : les autres cherchent leur cible par groupe de choses,
par comp ou par utilitaire, et n'ont donc rien à lister. Six types de loisir sur dix étaient
atteignables.

### Bâtiments que Shared Joys refusait

- **Les œuvres d'art** dont le `CompArt` porte `canBeEnjoyedAsArt`, soit les sculptures en vanilla. Les
  pierres tombales artistiques et les armes ornées portent `CompArt` sans ce drapeau et restent
  dehors, à raison.
- **Les foyers de méditation** : spot, trône, arbre anima, et tout bâtiment portant un
  `CompMeditationFocus`. Royalty uniquement.
- **Les tombes** occupées par un colon.

Ils apparaissent dans « Relax at … », dans les invitations et dans les événements autonomes.

`JobDriver_VisitJoyThing` réservant sa cible pour un seul pion, un groupe se répartit sur les pièces
voisines de la même salle, dans un rayon de 12 cases : un jardin de sculptures, un cimetière. La
méditation est le seul des trois où le foyer est réellement partagé : seule la case d'assise change,
et un arbre anima peut accueillir tout le monde.

### Activités sans lieu

Aucun bâtiment à cliquer pour une promenade : d'où une entrée de menu contextuel sur un colon,
« Inviter … à une activité », qui liste ce qui est réellement possible à cet instant. Fonctionne
avec plusieurs colons sélectionnés.

Promenade, contemplation du ciel, baignade, bonhomme de neige, lecture, méditation, prière,
substances et friandises. Aucun comportement n'est réécrit : c'est le `JoyGiver` vanilla qui
fabrique la tâche de chaque pion.

La liste est **déduite au chargement**, pas codée en dur : est « sans lieu » tout `JoyGiverDef` dont
le `giverClass` ne dérive ni de `JoyGiver_InteractBuilding`, ni de `JoyGiver_WatchBuilding`, ni de
`JoyGiver_SocialRelax`. Un mod tiers apparaît donc sans patch, vérifié en jeu avec
`Joy_BathingAtDrumBath`, qui s'est présenté seul dans la liste.

On ne filtre surtout pas sur `thingDefs` : `EatChocolate` en a une liste et reste une activité sans
lieu. Ni sur `jobDef` : `TakeDrug` et `EatChocolate` n'en déclarent aucun, `JoyGiver_Ingest`
fabriquant son `JobDefOf.Ingest` lui-même.

Promenades et baignades reprennent le trajet de l'hôte ; la contemplation du ciel prend une case
libre et découverte à côté de la sienne. Ces trois pilotes ne réservent rien, partager un trajet ne
peut donc pas produire de conflit.

### Deux correctifs à Shared Joys lui-même

- `JoyUtil.IsValidChair` reconnaissait un siège par son nom : en plus de `building.isSittable`, il
  acceptait tout `defName` contenant « bench » ou « seat ». Étant un **ou**, l'heuristique
  n'élargissait rien d'utile et n'ajoutait que des faux positifs. En vanilla seul, elle attrape
  **sept établis** : `SimpleResearchBench`, `HiTechResearchBench`, `AncientSimpleResearchBench`,
  `HandTailoringBench`, `ElectricTailoringBench`, `FabricationBench`, `AncientWorkbenchs`. Un colon
  pouvait être envoyé « s'asseoir » sur un établi de recherche.
- `JoyJobFactory.FreeParticipantSlots` comptait **toutes** les réservations posées sur le bâtiment,
  quelle que soit la tâche, et les soustrayait de `joyMaxParticipants`. Un porteur, un réparateur,
  un nettoyeur : chacun faisait paraître le lieu complet et interdisait le moment partagé.

Un troisième défaut est **signalé mais pas réparé**, parce qu'il ne peut pas l'être de l'extérieur :
Shared Joys met en cache le `MethodInfo` de deux méthodes **privées** du vanilla
(`JoyGiver_InteractBuilding.CanInteractWith` et `TryGivePlayJob`). Si une version de RimWorld les
renomme, `HasOwnWorker` renvoie faux et les échecs, le poker, le billard et les instruments se
rabattent en silence sur le chemin « regarder un bâtiment ». Notre propre recherche échouerait pour
la même raison : tout ce qu'on peut faire est transformer la panne muette en ligne de journal.

### Les plateaux se jouent sans chaise

`JoyGiver_InteractBuildingSitAdjacent.TryGivePlayJob` cherche une place en deux passes : une vraie
chaise d'abord, puis n'importe quelle case réservable. Entre les deux il fait
`if (def.requireChair) break;`, et `requireChair` vaut **vrai par défaut**, aucun `JoyGiverDef` du
jeu de base ne le passant à faux. La seconde passe n'a donc pratiquement jamais lieu : pas de
chaise, pas de partie. Dehors, des colons restent plantés à côté d'un plateau qu'ils ne toucheront
jamais.

Rien dans le jeu n'exige pourtant cette chaise. `JobDriver_SitFacingBuilding` réserve la case
d'assise par `ReserveSittableOrSpot`, qui accepte une case nue, et le pion s'y assoit sans rien
casser. La seconde passe est donc refaite ici, les chaises restant préférées quand il y en a.

La seule limite conservée est celle du jeu : `jobDef.joyMaxParticipants`, le nombre même que le
pilote réserve sur le bâtiment. Un plateau prévu pour deux reste à deux.

### Le message d'échec dit la vraie raison

Shared Joys n'en a qu'un pour tous les échecs de lieu, « Not enough space for everyone to chill »,
et il envoie chercher de la place là où il manque le plus souvent une chaise.

| Au lieu de « pas assez de place » | On lit |
|---|---|
| échecs, Ur, poker, plateaux de mods | il faut une chaise libre sur l'un des quatre côtés |
| télévision | il faut une chaise libre pour s'y asseoir |
| une sculpture, plusieurs pions | elle ne se contemple qu'à un à la fois |
| une tombe, plusieurs pions | on ne s'y recueille qu'à un |
| foyer de méditation saturé | aucune place libre autour |

Le message n'est pas doublé mais **remplacé**, par substitution dans `JoyUtil.Notify`. Cette méthode
n'est appelée que sur les chemins manuels chez Blues, tous ses appels étant gardés par
`if (manual)`. Un événement autonome raté reste donc silencieux.

Le cas de la chaise n'est le bug de personne : `requireChair` vaut vrai par défaut et aucun
`JoyGiverDef` du jeu de base ne le passe à faux, si bien que
`JoyGiver_InteractBuildingSitAdjacent.TryGivePlayJob` sort avant d'essayer le sol. Des colons ne
jouent pas aux échecs debout ; le message ne le disait simplement pas.

### Traduction

Interface en anglais et en français, 30 clés alignées. **Shared Joys lui-même est traduit en
français** (`Languages/French/Keyed/BluesSharedJoys.xml`, 47 clés), le mod d'origine n'étant livré
qu'en anglais.

### Réglages

- Deux interrupteurs : bâtiments étendus, menu d'activités.
- Une case par activité, pour retirer de la liste ce qu'on ne veut pas voir proposer.
- Les seuils (opinion minimale, niveau de loisir maximal, purge du loisir) sont lus dans les
  réglages de Shared Joys. Rien à régler deux fois.

### Notes techniques

- Trois greffes Harmony en **postfix** sur `Blues.JoyUtil` : `IsValidJoyBuilding`, `MakeJoyJob`,
  `GetAvailableSpots`. Toutes passives : elles ne s'expriment que là où l'original a renoncé. Les
  correctifs, eux, sont des **prefix** qui remplacent un comportement : les deux familles ne se
  mélangent pas.
- **Aucune référence de compilation vers l'assemblage de Blues** : tout passe par `AccessTools`. Le
  mod se charge sans lui, le menu d'activités reste utilisable seul, et une mise à jour qui déplace
  une méthode produit un avertissement de journal au lieu d'un échec de chargement.
- Le bilan est journalisé **même quand tout va bien** (`3/3 hooks applied`, suivi des activités
  détectées) : un mod muet est indiscernable d'un mod cassé.
- Les defs `Blues_SharedJoy_Thought` et `Blues_InviteHangout` sont réutilisées, jamais redéclarées :
  deux pensées concurrentes fausseraient leurs `stackLimit`.
