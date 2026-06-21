# 04 Health Resource

Shows `RuntimeResource` bound to a max stat. The sheet is built in code for
brevity.

Setup:
1. Create an empty GameObject and add `HealthResourceExample`.
2. Pick a `Max Change Policy` in the Inspector.
3. Enter Play Mode and read the Console.

The example registers MaxHealth (100), then calls `Reduce`, `Restore`, and
`Consume`, and finally raises MaxHealth to 200 so you can see how the chosen
`MaxChangePolicy` (KeepAbsolute / KeepPercent / ClampOnly) affects Current.
