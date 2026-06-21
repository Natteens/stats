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
