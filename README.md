# Stats

A modular, reusable stats system for Unity. The core is pure C# (no
`UnityEngine`) built around a `StatSheet` of `RuntimeStat`s keyed by a
GUID-backed `StatId`, with an ordered, swappable calculation pipeline. Authoring
is ScriptableObject-driven, timed modifiers run on the Unity Player Loop, and a
small resources module binds a current value to a max stat.

## Install

Add via the Unity Package Manager:

- Git URL: `Add package from git URL...` and point it at the repository, or
- Embedded: place the package under your project's `Packages/` folder.

Then import the samples from the package page in Package Manager.

## Quick setup

1. Create `StatDefinition` assets (Create > Stats > Stat Definition), e.g.
   Strength, MaxHealth, MoveSpeed. Each gets a hidden, stable GUID automatically.
2. Create a `StatSheetPreset` (Create > Stats > Stat Sheet Preset) and add your
   definitions with base values.
3. Add a `StatSheetBehaviour` to a GameObject and assign the preset. On `Awake`
   it builds a `StatSheet`, exposed via `Sheet`.
4. Read values from gameplay code:

```csharp
var sheet = GetComponent<StatSheetBehaviour>().Sheet;
float strength = sheet.GetValue(strengthDefinition.ToStatId());
```

## Core concepts

- `StatId` — GUID-backed identity of a stat, shared across authoring and runtime.
- `StatSheet` — per-entity container of `RuntimeStat`s; the main entry point.
- `StatModifier` — an operation (Flat / PercentAdd / PercentMult / Override), a
  value, and a runtime `source` used for grouped removal.
- `IStatCalculator` / `ICalculationStep` — the calculation strategy. The default
  `PipelineStatCalculator` runs ordered steps; it is not a single hardcoded method.
- `StatSnapshot` / `StatBreakdown` — cheap aggregate / detailed per-source view.

## Formula

```
Final = Clamp(
          Override ?? ((Base + SumFlat) * (1 + SumPercentAdd) * Product(1 + PercentMult_i)),
          Min, Max)
```

Step order is fixed: Flat (100) -> PercentAdd (200) -> PercentMult (300) ->
Override (400) -> Clamp (1000). The last Override added wins. PercentAdd stacks
additively (two +20% -> x1.4); PercentMult stacks multiplicatively (two +20% ->
x1.44).

## Percent convention

- Runtime / API values are fractions: `0.5` means `+50%`.
- The inspector drawer is the only place that converts: a designer types `50`
  and it is stored as `0.5`, displayed with a `%` suffix.
- Method names carry the convention: `AddPercentAdd`, `AddPercentMult`.

## StatDefinition GUID behavior

`StatDefinition` stores a hidden, serialized GUID string and converts to `StatId`
via `ToStatId()`. The GUID is generated once (in `OnValidate`) when missing and
is never regenerated on rename. It is independent of Unity's instance/asset IDs.
Duplicating an asset copies its GUID; run `Tools > Stats > Validate Stat IDs` to
find and repair missing or duplicate IDs.

## Human-readable keys

Each `StatDefinition` has three distinct fields:

- **ID** — a hidden, stable GUID (`StatId`). The source of truth for identity,
  registration, and save/restore. Never changes.
- **Key** — an authored, lowercase `snake_case` string (e.g. `movement_speed`)
  for readable gameplay lookups. Optional; independent of the display name.
- **Display Name** — the UI label (e.g. `Movement Speed`). Never used as a key.

Use `StatId` (via `def.ToStatId()`) for identity, storage, and hot paths. Use the
key for convenience in gameplay code:

```csharp
float speed = statSheetBehaviour.GetValue("movement_speed");
statSheetBehaviour.AddFlat("movement_speed", 2f, boots);
```

`StatSheetBehaviour` builds a `key -> StatId` cache on `Awake` (ordinal string
comparison). Lookups (`TryGetStatId`, `TryGetValue`, `GetValue`) return
false/fallback for unknown keys; the `Add*(string key, ...)` helpers throw
`KeyNotFoundException` for an unknown key. `StatSheetPreset` exposes
`TryGetDefinitionByKey` / `TryGetStatIdByKey`. Keys are never derived from the
display name or asset name at runtime, and changing the display name never
changes the key. Save/restore always uses the GUID `statId`; a `statKey` field in
the DTOs is debug-only and ignored on restore.

## StatSheetPreset

An authoring asset listing definitions with base values and optional min/max
overrides. `BuildSheet()` registers each entry into a new `StatSheet`.

## StatSheetBehaviour

A MonoBehaviour that builds a `StatSheet` from its preset on `Awake` and exposes
it via `Sheet`. It also owns a `TimedModifierService` (`TimedModifiers`) and
cancels its scheduled callbacks on `OnDestroy`.

## Modifiers and source removal

```csharp
var ring = new object();                       // the runtime item instance
sheet.AddFlat(strength, 5f, ring);
sheet.AddPercentAdd(strength, 0.5f, ring);     // 0.5 = +50%
sheet.RemoveModifiersFromSource(ring);         // removes every modifier from that source
```

`AddModifier` returns a `ModifierHandle` for precise single removal via
`RemoveModifier(handle)`. Use the runtime instance as the source so two identical
items are removable independently. `StatSheet.ApplyGroup(IReadOnlyList<StatModifierData>, source)`
applies a designer-authored group. A null source throws; an unregistered stat
throws `StatNotRegisteredException` (use `TryGetValue` for the safe path).

## Timed modifiers

```csharp
var timed = new TimedModifierService(new CountdownTimerScheduler());
timed.AddTimedModifier(sheet, attack, ModifierOperation.PercentAdd, 0.5f, "rage", 8f);
```

The modifier is applied through the normal `StatSheet` API and removed by handle
when the timer expires. Timers run centrally on the Player Loop (no per-modifier
`Update`). `CancelSchedules()` disposes scheduled callbacks but leaves modifiers
applied; `ClearTimedModifiers()` disposes callbacks and removes the modifiers.
A duration of zero or less is not applied. Expiry is driven through the
`IExpiryScheduler` seam, so it can be tested without entering Play Mode.

## RuntimeResource

```csharp
var health = new RuntimeResource(sheet, maxHealth, MaxChangePolicy.KeepPercent);
health.Reduce(30f);                 // damage, clamps at 0
health.Restore(10f);                // heal, clamps at Max
bool spent = health.Consume(25f);   // spends if available
```

`Max` is read live from the bound stat. `MaxChangePolicy` controls the reaction
when the max changes: `KeepAbsolute` (current unchanged, re-clamped),
`KeepPercent` (preserves the ratio), `ClampOnly` (only trims overflow). Events:
`Changed(current, max)` and `Emptied`.

## Save / restore (interop)

Stats does not write files. It exposes serializable DTOs so an external save
system can persist and reload runtime state.

```csharp
StatSheetSaveData data = statSheetBehaviour.CaptureSaveData();
// your save system serializes `data` (e.g. JSON) ...
statSheetBehaviour.RestoreSaveData(data);   // after the sheet is built from its preset
```

`StatSheetSaveData` holds a version, base values, active modifiers (operation,
value, source id, order), timed modifiers (with remaining seconds), and
registered resource current values. For the pure-core path,
`StatSaveInterop.Capture(StatSheet)` / `Restore(StatSheet, data)` work without Unity.

Source identity: a modifier is persisted only if its source is a `string` or
implements `IStatModifierSource` (a stable `SourceId`). Modifiers whose source is
a plain `object` are treated as transient and are not captured, so equipment you
re-apply on load is not double-restored. Restored modifiers use a `RestoredSource`,
so a consumable's timed buff is restored even when the original consumable object
is gone. Register resources to capture with `RegisterResource(key, resource)`.
Restore clears current modifiers, reapplies bases and modifiers, restores timed
modifiers with their remaining time, then restores resource current values.

## Limitations / non-goals

This package does not include inventory, equipment, combat, skill trees,
networking, save/load, derived/dependent stats, an effect graph, or stack
policies. Sources are runtime references (no persistence). It provides the stat
math, authoring, timed modifiers, and resources; the game owns the rest.
