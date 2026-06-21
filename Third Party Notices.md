# Third Party Notices

This package includes third-party software components.

## GameInit Timers

The timer module under `Runtime/Timers` is copied and adapted from the
`com.natteens.gameinit` package (https://github.com/Natteens/GameInit,
commit 4597d189), originally under `Runtime/Timers`.

Adapted files: `Timer`, `CountdownTimer`, `StopwatchTimer`, `FrequencyTimer`,
`IntervalTimer`, and the `Core` plumbing `TimerManager`, `ListExtensions`,
`PlayerLoopUtils`, `TimerBootstrapper`.

Adaptations: namespaces re-mapped from `GameInit.Timers` / `GameInit.Timers.Core`
to `Stats` / `Stats.Timers.Core`; XML documentation comments removed; the
`UnityEditor` using directive in `TimerBootstrapper` guarded with `#if UNITY_EDITOR`.
Timer runtime behavior is unchanged.

License: MIT

```
MIT License

Copyright (c) Nathan da Silva Miranda

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
