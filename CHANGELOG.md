# Journal des modifications

Format inspiré de [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/).
Ce fichier sert au dépôt et à rédiger les notes de version Steam ; RimWorld ne l'affiche pas en jeu.

## [1.0.2] — 2026-09-03

### Ajouté

- **Le message d'échec dit maintenant la vraie raison.** Shared Joys n'en a qu'un pour tous les
  échecs de lieu — « Not enough space for everyone to chill » — et il envoie chercher de la place là
  où il manque le plus souvent une chaise. L'extension aggravait le cas : une seule sculpture pour
  deux colons produisait le même texte.

  | Au lieu de « pas assez de place » | On lit |
  |---|---|
  | échecs, Ur, poker, plateaux de mods | il faut une chaise libre sur l'un des quatre côtés |
  | télévision | il faut une chaise libre pour s'y asseoir |
  | une sculpture, plusieurs pions | elle ne se contemple qu'à un à la fois |
  | une tombe, plusieurs pions | on ne s'y recueille qu'à un |
  | foyer de méditation saturé | aucune place libre autour |

  Le message n'est pas doublé mais **remplacé**, par substitution dans `JoyUtil.Notify`. Cette
  méthode n'est appelée que sur les chemins manuels chez Blues — tous ses appels sont gardés par
  `if (manual)` — donc un événement autonome raté reste silencieux comme avant.

  À noter : le cas de la chaise n'est **pas un bug**, ni chez Blues ni chez nous. `requireChair`
  vaut vrai par défaut et aucun `JoyGiverDef` du jeu de base ne le passe à faux, si bien que
  `JoyGiver_InteractBuildingSitAdjacent.TryGivePlayJob` sort avant d'essayer le sol. Des colons ne
  jouent pas aux échecs debout — le message ne le disait simplement pas.

## [1.0.1] — 2026-09-03

### Corrigé

- **Les substances et les friandises n'étaient jamais proposées**, alors que la fiche les annonce.
  `TakeDrug` et `EatChocolate` ne déclarent aucun `jobDef` — `JoyGiver_Ingest.CreateIngestJob`
  fabrique son `JobDefOf.Ingest` lui-même — et le filtre des activités sans lieu exigeait ce champ.
  Il n'a jamais servi : c'est le fournisseur vanilla qui construit la tâche. Le premier chargement
  en jeu annonçait 11 activités au lieu de 13.

### Vérifié en jeu

Premier chargement avec Shared Joys actif : les trois greffes se posent
(`3/3 hooks applied`), et `JoyGiver_InteractBuilding.CanInteractWith` et `TryGivePlayJob` se
résolvent bien en 1.6.4871 — l'avertissement prévu pour leur disparition ne s'est pas déclenché.
`Joy_BathingAtDrumBath`, fourni par un mod tiers, apparaît seul dans la liste des activités sans
lieu : la déduction au chargement fonctionne comme prévu.

## [1.0.0] — 2026-09-03

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

### Deux correctifs à Shared Joys lui-même

Contrairement aux greffes ci-dessus, ceux-ci **remplacent** un comportement. Ils n'ont pas
d'interrupteur : si Blues corrige en amont, le prefix calcule le même résultat que lui, et s'il
change une signature le correctif est ignoré avec une ligne de journal.

- `JoyUtil.IsValidChair` reconnaissait un siège par son nom : en plus de `building.isSittable`, il
  acceptait tout `defName` contenant « bench » ou « seat ». Étant un **ou**, l'heuristique
  n'élargissait rien d'utile et n'ajoutait que des faux positifs — en vanilla seul elle attrape
  **sept établis** : `SimpleResearchBench`, `HiTechResearchBench`, `AncientSimpleResearchBench`,
  `HandTailoringBench`, `ElectricTailoringBench`, `FabricationBench`, `AncientWorkbenchs`. Un
  colon pouvait être envoyé « s'asseoir » sur un établi de recherche.
- `JoyJobFactory.FreeParticipantSlots` comptait **toutes** les réservations posées sur le bâtiment,
  quelle que soit la tâche, et les soustrayait de `joyMaxParticipants`. Un porteur, un réparateur,
  un nettoyeur : chacun faisait paraître le lieu complet et interdisait le moment partagé.

Un troisième défaut est **signalé mais pas réparé**, parce qu'il ne peut pas l'être de l'extérieur :
Shared Joys met en cache le `MethodInfo` de deux méthodes **privées** du vanilla
(`JoyGiver_InteractBuilding.CanInteractWith` et `TryGivePlayJob`). Si une version de RimWorld les
renomme, `HasOwnWorker` renvoie faux et les échecs, le poker, le billard et les instruments se
rabattent en silence sur le chemin « regarder un bâtiment ». Notre propre recherche échouerait pour
la même raison : tout ce qu'on peut faire est transformer la panne muette en ligne de journal.

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
