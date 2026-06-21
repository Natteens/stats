# 03 Timed Buff

Shows `TimedModifierService` driving an auto-expiring buff with the package
timers (no custom timer system). The sheet is built in code for brevity.

Setup:
1. Create an empty GameObject and add `TimedBuffExample`.
2. Enter Play Mode.

A `+50%` PercentAdd buff is applied to a base-10 Attack stat for 3 seconds via
`AddTimedModifier`. The Console shows Attack at 15 during the buff and back to
10 after it expires. Expiry is driven by `CountdownTimerScheduler` on the Unity
Player Loop.
