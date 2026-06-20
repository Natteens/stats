# Stats

Modular stats system for Unity.

Layers: a pure C# Core (engine-free calculation pipeline, GUID-backed `StatId`,
`StatSheet`/`RuntimeStat`), an engine-free `Resources` module, a Player-Loop
`Timers` module, a `Unity` authoring/bridge layer (ScriptableObjects), and an
Editor layer.

Percent convention: runtime/API percent values are fractions (`0.5` = +50%).
The inspector drawer is the only place that displays/edits percent as `%`.

## Install

Add via Package Manager using the Git URL or as an embedded package under `Packages/`.

## Status

Scaffold only (Phase 0). Runtime, authoring, editor, timers, resources, samples,
and tests are not implemented yet.
