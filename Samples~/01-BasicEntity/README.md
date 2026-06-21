# 01 Basic Entity

Shows the authoring workflow: `StatDefinition` assets -> `StatSheetPreset` ->
`StatSheetBehaviour` on a GameObject, with a script that logs the built values.

Setup:
1. Create an empty GameObject.
2. Add `StatSheetBehaviour` and assign `Stats/BasicEntityPreset`.
3. Add `BasicEntityExample` and assign the three definitions in `Stats/`
   (Strength, MaxHealth, MoveSpeed) to its `Stats To Log` list.
4. Enter Play Mode. The Console logs each stat's value (10, 100, 5).

The `StatSheetBehaviour` builds the `StatSheet` from the preset on `Awake` and
exposes it through its `Sheet` property.
