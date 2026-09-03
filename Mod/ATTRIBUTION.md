# Attribution

## Shared Joys

- **Author:** Blues
- **Source:** Steam Workshop [3719496210](https://steamcommunity.com/sharedfiles/filedetails/?id=3719496210),
  `blues.sharedjoys`, RimWorld 1.6.
- **Licence:** none declared. No public repository either: neither `About.xml`, nor the assembly,
  nor the Workshop page carries a link to sources.

Blues was told about this mod on 2026-09-02 and agreed to it, on one condition: that Shared Joys be
declared a **requirement, never a replacement**. It is — `modDependencies`, no `incompatibleWith`,
and the description says so in its first line.

**Nothing from that mod is reused here.** Not a def, not a texture, not a line of code. This mod
contains only code written for the purpose, and grafts onto Blues' code through Harmony.

The extension hooks are **postfixes only**, and passive: they speak up where his code has already
given up. The two bug fixes are **prefixes that replace a behaviour** — a different thing, and each
one documents in the source why the original is wrong, not merely what it does instead. If Blues
fixes either upstream, the prefix ends up computing the same answer he does; if he changes a
signature, the patch is skipped and says so in the log.

Two things are borrowed by reference, never by copy:

| Borrowed | Why |
|---|---|
| `Blues_SharedJoy_Thought`, `Blues_InviteHangout` | Fetched with `DefDatabase.GetNamed(..., false)`. Declaring our own thoughts would stack two competing memories for the same event, and the `stackLimit` of each would be wrong. |
| `Blues.JoyousSet` (thresholds, recreation drain) | Read by reflection. Without this, the player would set the same thresholds in two places, with different results depending on which path was taken. |

`Languages/French/Keyed/BluesSharedJoys.xml` is a **full French translation of Shared Joys' 47
keys**. It is an original translation of Blues' English strings, not a copy of his file — his mod
ships in English only, so without it the whole interface stays English in a French game. The file
is named after its source rather than after us, so its origin is obvious at a glance, and it is
kept apart from our own keys: putting both in one file produced duplicate-key warnings, since
RimWorld merges the Keyed data of every active mod for a given language.

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

One reservation worth keeping in mind: `BluesSharedJoys.xml` translates his English strings. If
Blues ever ships his own French translation, that file will produce duplicate-key warnings and will
have to be removed.
