# Attribution

## Shared Joys

- **Author:** Blues
- **Source:** Steam Workshop [3719496210](https://steamcommunity.com/sharedfiles/filedetails/?id=3719496210),
  `blues.sharedjoys`, RimWorld 1.6.
- **Licence:** none declared. No public repository either: neither `About.xml`, nor the assembly,
  nor the Workshop page carries a link to sources.

**Nothing from that mod is reused here.** Not a def, not a texture, not a line of code. This mod
contains only code written for the purpose, and grafts onto Blues' code through Harmony, in
postfix only.

Two things are borrowed by reference, never by copy:

| Borrowed | Why |
|---|---|
| `Blues_SharedJoy_Thought`, `Blues_InviteHangout` | Fetched with `DefDatabase.GetNamed(..., false)`. Declaring our own thoughts would stack two competing memories for the same event, and the `stackLimit` of each would be wrong. |
| `Blues.JoyousSet` (thresholds, recreation drain) | Read by reflection. Without this, the player would set the same thresholds in two places, with different results depending on which path was taken. |

The three French translation keys in `Languages/French/Keyed/` — `BluesAlreadySatisfied`,
`BluesNotEnoughFren`, `BluesCantReach` — are **original translations** of Blues' English strings,
not a copy of his file. The original mod ships in English only; without them, those three messages
would stay in English in a French game.

**If Blues updates his mod**, this one has nothing to resynchronise: it follows three method
signatures only, and reports in the log any it can no longer find.

## RimWorld

The vanilla classes this mod relies on (`JoyGiver`, `MeditationUtility`, `JobDriver_VisitJoyThing`,
`FloatMenuOptionProvider`) belong to **Ludeon Studios**. They are called, never copied.

## Licence

This mod's code is MIT licensed (`LICENSE`). That obviously does not extend to Blues' mod, which
it depends on.

**Publishable as it stands.** The absence of a licence on Blues' mod is no obstacle: hooking
Harmony onto a third-party mod without taking any of its files does not fall under its licence,
and it is standard practice in this community. This mod is useless without his, but it does not
redistribute it.

One reservation worth keeping in mind: the three French keys above translate his English strings.
If Blues ever ships his own French translation, they will produce duplicate-key warnings and will
have to be removed.
