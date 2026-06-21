# 02 Item Modifiers

Shows grouped modifier application and source-based removal. `SampleItem` is a
game-side ScriptableObject holding a `List<StatModifierData>` (configured with
the one-line drawer). The package itself never references items.

Setup:
1. Create an empty GameObject.
2. Add `StatSheetBehaviour` and assign `Stats/ItemEntityPreset`.
3. Add `ItemModifierExample`, assign `RingOfMight` to `Item` and the
   `Stats/Strength` definition to `Tracked Stat`.
4. Enter Play Mode and read the Console.

The example equips the same item as two separate runtime instances (`ringA`,
`ringB`), then removes one with `RemoveModifiersFromSource`, showing the other
stays applied. Equip uses `StatSheet.ApplyGroup(item.modifiers, source)`.
