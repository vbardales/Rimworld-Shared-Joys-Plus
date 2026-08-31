# Attribution

## Shared Joys

- **Auteur :** Blues
- **Source :** Steam Workshop [3719496210](https://steamcommunity.com/sharedfiles/filedetails/?id=3719496210),
  `blues.sharedjoys`, RimWorld 1.6.
- **Licence :** aucune déclarée. Aucun dépôt public : ni `About.xml`, ni la DLL, ni la page
  Workshop ne portent de lien vers des sources.

**Rien de ce mod n'est repris ici.** Pas une def, pas une texture, pas une ligne de code. Ce mod-ci
ne contient que du code écrit pour l'occasion, et se greffe sur celui de Blues par Harmony, en
postfix uniquement.

Deux emprunts par référence, jamais par copie :

| Emprunt | Pourquoi |
|---|---|
| `Blues_SharedJoy_Thought`, `Blues_InviteHangout` | Récupérées par `DefDatabase.GetNamed(..., false)`. Déclarer nos propres pensées empilerait deux souvenirs concurrents pour le même événement, et le `stackLimit` de chacune serait faux. |
| `Blues.JoyousSet` (seuils, purge du loisir) | Lus par réflexion. Sans ça, le joueur règlerait les mêmes seuils à deux endroits, avec des résultats différents selon le chemin emprunté. |

Les trois clés de traduction françaises reproduites dans `Languages/French/Keyed/` —
`BluesAlreadySatisfied`, `BluesNotEnoughFren`, `BluesCantReach` — sont des **traductions
originales** des textes anglais de Blues, pas une copie de son fichier. Le mod d'origine n'est
livré qu'en anglais ; sans elles, ces trois messages resteraient en anglais dans une partie
française.

**Si Blues met son mod à jour**, ce mod n'a rien à resynchroniser : il ne suit que trois
signatures de méthode, et signale dans le journal celles qu'il ne retrouve plus.

## RimWorld

Les classes vanilla dont le mod se sert (`JoyGiver`, `MeditationUtility`, `JobDriver_VisitJoyThing`,
`FloatMenuOptionProvider`) appartiennent à **Ludeon Studios**. Elles sont appelées, jamais copiées.

## Licence

Le code de ce mod est sous MIT (`LICENSE`). Il ne couvre évidemment pas le mod de Blues, dont il
dépend.

**Publiable tel quel.** L'absence de licence sur le mod de Blues n'y fait pas obstacle : greffer
Harmony sur un mod tiers sans en reprendre aucun fichier ne relève pas de sa licence, et c'est la
pratique courante du milieu. Ce mod ne sert à rien sans le sien, mais il ne le redistribue pas.

Seule réserve à garder en tête : les trois clés françaises ci-dessus traduisent ses textes anglais.
Si Blues livre un jour sa propre traduction française, elles produiront des avertissements de clé
en double, et il faudra les retirer.
