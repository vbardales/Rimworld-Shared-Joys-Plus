# Shared Joys — tous les loisirs

Mod compagnon de **Shared Joys** de Blues ([Steam 3719496210](https://steamcommunity.com/sharedfiles/filedetails/?id=3719496210)).
Il ne remplace rien et ne copie aucun fichier : il se charge après lui et complète ce que son
mécanisme ne pouvait pas atteindre.

## Le problème

Tout Shared Joys tient à une seule fonction, `JoyUtil.IsValidJoyBuilding`, qui n'accepte un
bâtiment qu'à deux conditions : soit un `JoyGiverDef` **cite son `ThingDef`** dans `<thingDefs>`,
soit il porte un `CompGatherSpot` actif.

Or sur les 23 `JoyGiverDef` de RimWorld 1.6 (Core + Royalty + Odyssey), **9 seulement remplissent
`thingDefs`**. Les autres cherchent leur cible autrement — par groupe de choses
(`ThingRequestGroup.Art`, `.Grave`), par comp (`CompMeditationFocus`), par utilitaire
(`BookUtility`), ou n'ont aucune cible du tout. Ils n'ont donc rien à lister, et Shared Joys ne
pouvait pas les voir.

| Type de loisir | Avant | Après |
|---|---|---|
| Gaming_Dexterity, Gaming_Cerebral, Television, Telescope, HighCulture | ✅ `thingDefs` | inchangé |
| Social | ✅ `CompGatherSpot` | inchangé |
| **Meditative** | ❌ | art, méditation, tombes, promenade, ciel, baignade, bonhomme de neige |
| **Reading** | ❌ | lecture partagée |
| **Chemical / Gluttonous** | ❌ | substances et friandises partagées |

## Ce que le mod ajoute

### 1. Des bâtiments que Shared Joys refusait

Trois greffes Harmony en **postfix** sur `Blues.JoyUtil`, toutes passives : elles ne s'expriment
que là où le mod d'origine a déjà renoncé (résultat faux, tâche nulle, zéro place).

| Greffe | Rôle |
|---|---|
| `IsValidJoyBuilding` | fait accepter l'art, les tombes et les foyers de méditation |
| `MakeJoyJob` | fabrique la tâche, faute de `JoyGiverDef` pour le faire |
| `GetAvailableSpots` | annonce le nombre de places, sans quoi les événements autonomes ignorent le lieu |

Ces bâtiments apparaissent donc dans « Relax at ... », dans les invitations, et dans les
événements autonomes, exactement comme un billard.

**Ce qui est reconnu :**

- **L'art** — tout `CompArt` dont les props posent `canBeEnjoyedAsArt`. En vanilla, cela ne vise
  que les sculptures (`SculptureBase`, `CubeSculptureBase` d'Anomaly, et deux defs d'Odyssey et
  d'Anomaly) : la pierre tombale artistique et les armes ornées portent `CompArt` sans ce
  drapeau, et restent donc hors du lot.
- **Les foyers de méditation** — spot de méditation, trône, arbre anima, et tout bâtiment portant
  un `CompMeditationFocus`. Uniquement si Royalty est actif.
- **Les tombes** — `Building_Grave` occupée par un colon.

**Le point qui gouverne la répartition du groupe.** `JobDriver_VisitJoyThing` réserve sa cible
avec `maxPawns 1` : deux pions **ne peuvent pas** admirer la même sculpture ni se recueillir sur
la même tombe. Le mod répartit donc le groupe sur les pièces voisines de la même salle, dans un
rayon de 12 cases — un jardin de sculptures, un cimetière. La méditation est le seul des trois cas
où le foyer est réellement partagé : seule la case d'assise change, et un arbre anima peut
accueillir tout le monde.

La liste des cibles déjà distribuées est celle que Shared Joys passe lui-même à chaque
participant (`takenSpots`), complétée ici : rien à synchroniser en plus.

### 2. Des activités sans lieu

Il n'y a **aucun bâtiment à cliquer** pour une promenade. D'où une nouvelle entrée de menu
contextuel sur un colon : **« Inviter X à une activité... »**, qui ouvre la liste des loisirs
possibles à cet instant. Fonctionne aussi avec plusieurs colons sélectionnés.

Aucun comportement n'est réécrit : c'est le `JoyGiver` vanilla lui-même qui fabrique la tâche de
chaque pion. Le mod se contente de la demander pour plusieurs pions à la fois, puis de rapprocher
les destinations quand l'activité s'y prête :

- **promenade et baignade** : les invités reprennent le trajet de l'hôte, point par point ;
- **contemplation du ciel** : chacun prend une case libre et découverte à côté de celle de l'hôte ;
- le reste (lecture, bonhomme de neige, substances) : chacun sa cible, c'est la nature de
  l'activité.

Ces trois pilotes ne réservent rien — `TryMakePreToilReservations` y renvoie `true` sans rien
prendre — donc partager un trajet ne peut pas produire de conflit de réservation.

**La liste est construite au chargement**, pas codée en dur : est « sans lieu » tout `JoyGiverDef`
dont le `giverClass` ne dérive ni de `JoyGiver_InteractBuilding`, ni de `JoyGiver_WatchBuilding`,
ni de `JoyGiver_SocialRelax` — les trois familles ancrées sur un bâtiment, celles que Shared Joys
couvre déjà. Un mod tiers qui ajoute un loisir sans lieu apparaît donc tout seul.

On ne filtre **surtout pas** sur `thingDefs` : `EatChocolate` en a une liste et reste une activité
sans lieu.

## Ce que le mod ne fait pas

**Les bâtiments de loisir orphelins** — ceux qui portent un `<building><joyKind>` sans qu'aucun
`JoyGiverDef` ne les serve — ne sont pas traités ici. C'est le domaine de **Joy Rescue**, qui
fabrique les `JobDef` et `JoyGiverDef` manquants au chargement. Une fois qu'il les a créés, Shared
Joys les voit sans que ce mod-ci ait à intervenir : les deux se composent, il n'y a rien à
dupliquer.

## Réglages

- **Étendre les invitations aux bâtiments** (art, méditation, tombes) — décochable.
- **Activer le menu des activités sans lieu** — décochable.
- **Une case par activité**, pour retirer de la liste ce qu'on ne veut pas voir proposer
  (les substances, typiquement).

Les seuils — opinion minimale, niveau de loisir maximal, purge du loisir avant un moment partagé —
sont lus dans les réglages de Shared Joys : rien à régler deux fois. Si le mod d'origine est
absent, des valeurs de repli prennent le relais.

## Robustesse

Le mod **ne référence pas** l'assemblage de Blues à la compilation. Tout passe par réflexion
(`AccessTools`). Conséquences :

- si Shared Joys est absent, le mod se charge quand même, journalise la situation, et **le menu
  des activités reste utilisable seul** ;
- si Shared Joys change de forme lors d'une mise à jour Steam, les greffes manquantes sont
  signalées une par une dans le journal au lieu de faire tomber le chargement.

## Construction

```
dotnet build SharedJoysPlus/Source/SharedJoysPlus.csproj -c Release
```

Sortie directe dans `SharedJoysPlus/Assemblies/`. Une jonction NTFS relie
`RimWorld\Mods\SharedJoysPlus` à ce dossier : aucune copie à faire.

## Ordre de chargement

Après **Shared Joys**. Le `loadAfter` de `About.xml` s'en charge.

Sous licence MIT. Ce mod ne contient aucun fichier de Shared Joys — ni def, ni texture, ni ligne
de code — et ne référence même pas sa DLL à la compilation : il est publiable tel quel. Voir
[ATTRIBUTION.md](ATTRIBUTION.md).
