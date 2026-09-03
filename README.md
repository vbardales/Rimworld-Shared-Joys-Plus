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

### Fixes to Shared Joys itself

Two bugs are corrected on top of the extension. Unlike the three hooks above, these are **prefixes
that replace a behaviour**. They have no switch: if Blues fixes either upstream, the prefix simply
computes the same answer he does, and if he changes a signature the patch is skipped with a line in
the log.

- **`JoyUtil.IsValidChair` recognised a seat by name.** On top of `building.isSittable`, it also
  accepted any `defName` containing `"bench"` or `"seat"`. Since that is an *or*, the heuristic
  widened nothing useful — it only added false positives, and in vanilla alone it catches seven
  workbenches: `SimpleResearchBench`, `HiTechResearchBench`, `AncientSimpleResearchBench`,
  `HandTailoringBench`, `ElectricTailoringBench`, `FabricationBench` and `AncientWorkbenchs`. A
  colonist could be sent to "sit" on a research bench. Nothing is lost by dropping it: furniture
  the game can actually seat a pawn on carries `isSittable`, or `CanReserveSittableOrSpot` would
  refuse it anyway.
- **`JoyJobFactory.FreeParticipantSlots` counted every reservation** held on the building, whatever
  the job, and subtracted that from `joyMaxParticipants`. A hauler picking something up off the
  table, a builder repairing it, a cleaner — each one made the spot look full and blocked the
  shared break. Only reservations whose job matches the recreation are counted now.

### It tells you why, instead of guessing

Shared Joys has one message for every placement failure — *"Not enough space for everyone to
chill"* — and it sends you looking for room when what is usually missing is a chair. The extension
above made that worse: one sculpture and two colonists produced the same text, when the real reason
is that an artwork can only be admired by one pawn at a time.

The message is not doubled, it is **replaced**: the reason is worked out when job creation fails,
then substituted inside `JoyUtil.Notify`. That method is only ever called on player-initiated paths
— every call site in Shared Joys is guarded by `if (manual)` — so a failed autonomous event stays as
quiet as before.

| Instead of "not enough space" | You get |
|---|---|
| chess, Ur, poker, go boards… | *needs a free chair on one of its four sides — one per player* |
| television | *needs a free chair to watch from — one per viewer* |
| a single sculpture, several pawns | *only one colonist can admire it at a time* |
| a single grave, several pawns | *only one colonist can pay respects at a time* |
| a meditation focus with no room | *no free spot to sit and meditate around it* |

The chair case is not a bug in either mod, incidentally — it is vanilla. `requireChair` defaults to
**true** and no `JoyGiverDef` in the base game sets it to false, so
`JoyGiver_InteractBuildingSitAdjacent.TryGivePlayJob` breaks out before it ever tries the bare
ground. Colonists don't play chess standing up. The message just never said so.

A third problem is reported but **not** repaired, because it cannot be from the outside:
`JoyJobFactory` caches the `MethodInfo` of two *private* vanilla methods,
`JoyGiver_InteractBuilding.CanInteractWith` and `TryGivePlayJob`. If a RimWorld version renames
either, `HasOwnWorker` simply returns false and chess, poker, billiards and instruments fall back to
the watch-a-building path — silently. Our own lookup would fail for the same reason, so all this mod
can do is turn a silent failure into a log line at startup. That is still the difference between a
bug a player reports and a bug you see when the game loads.

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
