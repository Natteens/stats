# Stats — Usage Guide

## Architecture overview

The package is split into assemblies with dependencies pointing inward to a pure
core:

- `Stats.Core` (engine-free): `StatId`, `StatModifier`, `ModifierHandle`,
  `CalculationContext`, `IStatCalculator` / `ICalculationStep` /
  `PipelineStatCalculator` + steps, `RuntimeStat`, `StatSheet`, `StatSnapshot`,
  `StatBreakdown`.
- `Stats.Resources` (engine-free): `RuntimeResource`, `MaxChangePolicy`.
- `Stats.Timers` (UnityEngine): copied/adapted GameInit timers driven by the
  Player Loop, plus `IExpiryScheduler` and `CountdownTimerScheduler`.
- `Stats.Unity`: `StatDefinition`, `StatSheetPreset`, `StatModifierData`,
  `StatSheetBehaviour`, `TimedModifierService`, `StatDefinitionExtensions`.
- `Stats.Editor`: inspectors, the `StatModifierData` drawer, and the ID validator.

`Stats.Core` and `Stats.Resources` never reference `UnityEngine` and are unit
tested without it.

## Calculation pipeline

`PipelineStatCalculator.CreateDefault()` wires the ordered steps once:

| Step | Order | Behavior |
|---|---|---|
| Flat | 100 | `current += sum(flat)` |
| PercentAdd | 200 | `current *= (1 + sum(percentAdd))` |
| PercentMult | 300 | `current *= product(1 + percentMult_i)` |
| Override | 400 | last override replaces the value |
| Clamp | 1000 | `current = clamp(current, Min, Max)` |

`RuntimeStat` caches the result and recomputes only when a modifier is added or
removed or the base changes, then raises `ValueChanged` when the value changes.

## Examples

Build a sheet in code:

```csharp
var sheet = new StatSheet();
var strength = StatId.NewId();
sheet.RegisterStat(strength, baseValue: 10f, min: 0f);

sheet.AddFlat(strength, 5f, "sword");
sheet.AddPercentAdd(strength, 0.5f, "rage");     // 0.5 = +50%
sheet.AddPercentMult(strength, 0.25f, "shrine");
float value = sheet.GetValue(strength);          // (10+5) * 1.5 * 1.25 = 28.125
```

Inspect the result:

```csharp
StatSnapshot snap = sheet.GetSnapshot(strength);
// snap.Base, snap.SumFlat, snap.SumPercentAdd, snap.PercentMultProduct,
// snap.HasOverride, snap.OverrideValue, snap.Final, snap.Clamped

StatBreakdown breakdown = sheet.GetBreakdown(strength); // per-source, for UI/debug
```

Authoring conversion:

```csharp
StatId id = strengthDefinition.ToStatId();
sheet.ApplyGroup(item.modifiers, itemRuntimeInstance);   // equip
sheet.RemoveModifiersFromSource(itemRuntimeInstance);    // unequip
```

Timed modifier (testable seam):

```csharp
var timed = new TimedModifierService(new CountdownTimerScheduler());
ModifierHandle h = timed.AddTimedModifier(
    sheet, strength, ModifierOperation.PercentAdd, 0.5f, "rage", durationSeconds: 8f);
```

Resource:

```csharp
var health = new RuntimeResource(sheet, maxHealth, MaxChangePolicy.KeepAbsolute);
health.Reduce(30f);
health.Restore(10f);
bool ok = health.Consume(25f);
float pct = health.Normalized;   // Current / Max, 0..1
```

## Common workflows

Designer entity setup:
1. Create `StatDefinition` assets.
2. Create a `StatSheetPreset` listing them with base values.
3. Add `StatSheetBehaviour` to the prefab and assign the preset.

Items/buffs (game-owned):
1. The game's item type holds a `List<StatModifierData>`.
2. On equip: `sheet.ApplyGroup(item.modifiers, runtimeInstance)`.
3. On unequip: `sheet.RemoveModifiersFromSource(runtimeInstance)`.

Use the runtime instance as the source so two identical items are removed
independently.

## Percent convention

Runtime/API values are fractions (`0.5` = +50%). The inspector drawer is the
single converter: a designer types `50` and `0.5` is stored. Never convert
percent anywhere else.

## Stat keys

A `StatDefinition` carries three separate concepts:

- `Id` (`StatId`): hidden GUID, the stable identity used everywhere internally
  and for save/restore. Do not migrate this to a string.
- `Key`: an optional, authored `snake_case` string for gameplay lookups.
- `DisplayName`: the UI label, never used for lookup.

When to use which: use `StatId` for registration, modifiers on hot paths, and
persistence; use the key for readable gameplay code; never look up by display
name (it is a localizable label that can change).

```csharp
// preset / definition
StatId id = preset.TryGetStatIdByKey("movement_speed", out var s) ? s : StatId.Empty;

// behaviour convenience (cached key -> StatId, ordinal comparison)
float speed = behaviour.GetValue("movement_speed");
behaviour.AddPercentAdd("movement_speed", 0.1f, buff);   // throws KeyNotFoundException if key unknown
```

Rules: keys are lowercase `snake_case`, stable, and distinct from the display
name. The editor can suggest a key from the display name when empty but never
overwrites a key you have set and never regenerates it on rename. An empty key
simply means the stat is not findable by key (it still builds and works by
`StatId`). Duplicate keys in one preset are reported in the editor and the runtime
cache deterministically keeps the first entry. Save/restore still keys off the
GUID `statId`; the DTO `statKey` is debug-only and not used on restore, and old
save data without `statKey` remains valid.

## Save / restore interop

Stats never touches the filesystem. It captures runtime state into plain
`[Serializable]` DTOs that an external save system persists, then restores from
them. No reflection is required by callers.

Behaviour-level (composes bases, non-timed modifiers, timed modifiers, and
registered resources):

```csharp
StatSheetSaveData data = behaviour.CaptureSaveData();
behaviour.RestoreSaveData(data);
```

Core-only (engine-free, base values and non-timed modifiers):

```csharp
StatSheetSaveData data = StatSaveInterop.Capture(sheet);
StatSaveInterop.Restore(sheet, data);
```

DTO shape:

- `StatSheetSaveData { int version; List<StatBaseSaveData> bases;
  List<StatModifierSaveData> modifiers; List<RuntimeResourceSaveData> resources; }`
- `StatBaseSaveData { string statId; float baseValue; }`
- `StatModifierSaveData { string statId; int operation; float value;
  string sourceId; bool timed; float remainingSeconds; int order; }`
- `RuntimeResourceSaveData { string key; string maxStatId; float current; }`

`statId` is the stable `StatId` GUID string. No `UnityEngine.Object` or
ScriptableObject references are stored, and runtime handles are never persisted.

Source identity: capture stores a `sourceId` derived from a `string` source or an
`IStatModifierSource.SourceId`. A modifier whose source is a plain `object` is not
captured (treated as transient and re-applied by your own equip logic on load).
Restored modifiers are given a `RestoredSource` carrying the saved id, so a buff
survives even if its original source object was destroyed.

Restore order is deterministic: clear current modifiers, apply base values, apply
non-timed modifiers sorted by `order` (so the last override wins as before),
reschedule timed modifiers with `remainingSeconds`, then set resource current
values last (after max-stat modifiers are in place so clamping is correct).
Saved entries for stats that are not registered on the rebuilt sheet are skipped.

Register a resource for capture with `behaviour.RegisterResource(key, resource)`.

## Troubleshooting

- `StatNotRegisteredException` when reading a stat: the `StatId` was not
  registered on that sheet. Register it first or use `TryGetValue`.
- A modifier is not removed on unequip: pass the same runtime instance you used
  on equip as the source (sources are compared by reference).
- A timed buff never expires in Play Mode: ensure you used the package's
  `CountdownTimerScheduler` (or another `IExpiryScheduler`); timers tick on the
  Player Loop. A duration of zero or less is intentionally not applied.
- A `StatDefinition` shows "Missing ID" or "Duplicate ID": use the inspector
  buttons or `Tools > Stats > Validate Stat IDs`. Duplicating an asset copies its
  GUID, which the validator detects.
- An `Override` modifier hides everything else: by design the last override wins;
  `StatSnapshot.HasOverride` surfaces this in the debug inspector.
