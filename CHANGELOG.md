# [0.3.0](https://github.com/Natteens/stats/compare/v0.2.0...v0.3.0) (2026-06-21)


### Features

* Add core stats system, docs, and samples ([d44bd4e](https://github.com/Natteens/stats/commit/d44bd4e1d10c120dc022f1eee562545f063d03e1))

# [0.2.0](https://github.com/Natteens/stats/compare/v0.1.1...v0.2.0) (2026-06-21)


### Features

* Add editor UI, timer system, and timed modifiers ([38b760f](https://github.com/Natteens/stats/commit/38b760f0ebafa5c3aa1b0bbd5d70a569aae42281))

# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Core stats calculation: GUID-backed `StatId`, `StatSheet`/`RuntimeStat`, and an
  ordered, swappable pipeline (`IStatCalculator`, `ICalculationStep`,
  `PipelineStatCalculator`) with Flat, PercentAdd, PercentMult, Override, and
  Clamp steps; `StatSnapshot` and `StatBreakdown`.
- Modifier API with handle-based and source-based removal.
- `RuntimeResource` and `MaxChangePolicy` (KeepAbsolute, KeepPercent, ClampOnly).
- Unity authoring: `StatDefinition` (hidden serialized GUID), `StatSheetPreset`,
  `StatModifierData`, `StatSheetBehaviour`, `StatDefinitionExtensions`.
- Editor UX (UI Toolkit): custom inspectors, one-line percent-aware
  `StatModifierData` drawer, and the `Tools > Stats > Validate Stat IDs` tool.
- Timed modifiers: `TimedModifierService` over an `IExpiryScheduler` seam, with
  Player-Loop timers copied/adapted from the GameInit package.
- Four importable samples: Basic Entity, Item Modifiers, Timed Buff, Health
  Resource; README and usage documentation.
