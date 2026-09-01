# Shared Joys - All Recreation

A companion mod for [Shared Joys](https://steamcommunity.com/sharedfiles/filedetails/?id=3719496210)
by Blues, for RimWorld 1.6. It extends the invitations to every kind of recreation, not just the
ones tied to a building that some `JoyGiverDef` happens to name.

It replaces nothing and copies nothing: it loads after Shared Joys and fills in what that mod's
mechanism could not reach.

## The problem

Shared Joys accepts a building as a hangout spot on one of two conditions: a `JoyGiverDef` lists
its `ThingDef` in `<thingDefs>`, or the building carries an active `CompGatherSpot`.

But of the 23 `JoyGiverDef`s in RimWorld 1.6 (Core + Royalty + Odyssey), **only 9 fill in
`thingDefs`**. The rest find their target another way — by thing group (`ThingRequestGroup.Art`,
`.Grave`), by comp (`CompMeditationFocus`), through a utility class (`BookUtility`), or they have
no target at all. They have nothing to list, so Shared Joys could not see them.

| Recreation type | Before | After |
|---|---|---|
| Gaming_Dexterity, Gaming_Cerebral, Television, Telescope, HighCulture | yes, via `thingDefs` | unchanged |
| Social | yes, via `CompGatherSpot` | unchanged |
| **Meditative** | no | art, meditation, graves, walks, skygazing, swimming, snowmen |
| **Reading** | no | shared reading |
| **Chemical / Gluttonous** | no | shared drugs and treats |

## What it adds

### Buildings Shared Joys used to turn down

Three Harmony **postfix** hooks on `Blues.JoyUtil`, all passive: they only speak up where the
original has already given up (false result, null job, zero spots).

| Hook | Role |
|---|---|
| `IsValidJoyBuilding` | makes art, graves and meditation foci acceptable |
| `MakeJoyJob` | builds the job, since no `JoyGiverDef` will |
| `GetAvailableSpots` | reports the spot count, without which autonomous events ignore the place |

These buildings then show up in "Relax at ...", in invitations and in autonomous events, exactly
like a billiards table.

**What is recognised:**

- **Art** — any `CompArt` whose props set `canBeEnjoyedAsArt`. In vanilla that means sculptures
  only (`SculptureBase`, Anomaly's `CubeSculptureBase`, and one def each in Odyssey and Anomaly):
  artistic gravestones and engraved weapons carry `CompArt` without that flag, and stay out.
- **Meditation foci** — meditation spot, throne, anima tree, and any building with a
  `CompMeditationFocus`. Royalty only.
- **Graves** — a `Building_Grave` holding a colonist.

**The fact that governs how a group is spread.** `JobDriver_VisitJoyThing` reserves its target
with `maxPawns 1`: two pawns **cannot** admire the same sculpture, nor pay respects at the same
grave. So a group spreads over the neighbouring pieces in the same room, within 12 cells — a
sculpture garden, a graveyard. Meditation is the one case of the three where the focus is
genuinely shared: only the sitting cell changes, so an anima tree can host everyone.

The list of targets already handed out is the one Shared Joys passes to every participant itself
(`takenSpots`), extended here: nothing extra to synchronise.

### Recreation with no place to click

There is no building to right-click for a walk. Hence a new float menu option on a colonist:
**"Invite ... to an activity"**, which opens the list of what is actually possible at that moment.
It works with several colonists selected too.

No behaviour is rewritten: the vanilla `JoyGiver` builds each pawn's job itself. The mod only asks
it for several pawns at once, then brings the destinations together where the activity allows it:

- **walks and swimming** — the guests take the host's route, point by point;
- **skygazing** — each takes a free, unroofed cell next to the host's;
- the rest (reading, snowmen, drugs) — everyone gets their own target, which is the nature of the
  activity.

None of those three drivers reserve anything — `TryMakePreToilReservations` returns `true` there
without taking a thing — so sharing a route cannot produce a reservation conflict.

**The list is built at load time**, not hardcoded: a `JoyGiverDef` counts as placeless when its
`giverClass` derives from none of `JoyGiver_InteractBuilding`, `JoyGiver_WatchBuilding` or
`JoyGiver_SocialRelax` — the three building-anchored families Shared Joys already covers. A
third-party mod adding placeless recreation therefore shows up on its own.

Filtering on `thingDefs` would be **wrong**: `EatChocolate` has a list of them and is still a
placeless activity.

## What it does not do

**Orphaned recreation buildings** — those carrying a `<building><joyKind>` that no `JoyGiverDef`
serves — are not handled here. That is a different problem: the giver itself has to be created, at
def generation time. Once such a giver exists, Shared Joys picks the building up on its own and
this mod has nothing to add.

## Settings

- **Extend building invitations** (art, meditation, graves) — can be switched off.
- **Enable the placeless activity menu** — can be switched off.
- **One checkbox per activity**, to drop what you would rather never be offered (drugs, typically).

The thresholds — minimum opinion, maximum recreation level, recreation drain before a shared break
— are read from Shared Joys' own settings. Nothing to set twice. If that mod is absent, fallback
values take over.

## Robustness

The mod **does not reference** Blues' assembly at compile time. Everything goes through reflection
(`AccessTools`). As a result:

- if Shared Joys is absent, the mod still loads, says so in the log, and **the activity menu stays
  usable on its own**;
- if Shared Joys changes shape in a Steam update, each missing hook is reported in the log instead
  of bringing the load down.

It always reports success too, not just failure: a silent mod is indistinguishable from a broken
one.

```
[Shared Joys+] 3/3 hooks applied, 12 placeless activities found: BuildSnowman, EatChocolate, ...
```

## Building

```
dotnet build Source/SharedJoysPlus.csproj -c Release
```

Output goes straight to `Mod/Assemblies/`. Reference assemblies come from NuGet
(`Krafs.Rimworld.Ref`), so no RimWorld installation is needed to compile — which is also why CI
can build it.

## Layout

```
Mod/            what ships to the Workshop
Source/         C# sources, never published
Art/            full-resolution source images
.build/         build intermediates, kept out of Mod/ on purpose
```

`SteamUGC.SetItemContent` uploads the mod folder as it stands, with no filtering of any kind: the
only way not to publish the sources is for them not to be in there. Hence the split, and hence the
`Directory.Build.props` that pushes intermediates out — it has to be that file, since NuGet
restore reads the property before the `.csproj`.

## Load order

After **Shared Joys**. The `loadAfter` in `About.xml` takes care of it. Harmony is required.

## Licence

MIT, see [LICENSE](LICENSE). This mod contains no file from Shared Joys — no def, no texture, no
line of code — and does not even reference its DLL at compile time. See
[ATTRIBUTION.md](ATTRIBUTION.md).
