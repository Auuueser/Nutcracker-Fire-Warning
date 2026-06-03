# Changelog

All notable changes to Nutcracker Fire Warning are documented in this file.

## 1.0.4

- Added `EnableMod` as a default-on master enable option.
- Reduced idle overhead by delaying warning object creation until aiming, reloading, or pre-aim danger tracking is relevant.
- Replaced per-frame reflection value reads with cached Harmony field references.
- Removed `GetType().Name` renderer checks and switched to strong renderer type checks.
- Reduced pre-aim danger checks by throttling line-of-sight tests and using cheaper angle filtering.
- Reduced countdown text allocations by caching countdown labels.
- Cached eligible model renderers for screen-box bounds and mesh warning updates.
- Increased fallback monitor defaults to reduce scan frequency.

## 1.0.3

- Fixed `EnableUiFireWindow=false` so it hides the entire world-space side warning bar instead of only disabling the final red-white pulse.
- Allowed `PreAimMaxDistance=0` or lower to disable the yellow pre-aim danger bar.
- Updated config descriptions to match the actual UI behavior.

## 1.0.2

- Renamed the plugin to Nutcracker Fire Warning.
- Added configurable model warning modes:
  - `SourcePulse`
  - `CloneShell`
  - `Both`
- Added configurable model pulse intensity and alpha.
- Added model warning distance filtering and optional camera-visibility filtering.
- Added configurable fallback monitor scan intervals.
- Added configurable pre-aim warning distance.
- Improved material restoration when warning states end, components are disabled, or enemies are removed.
- Changed the default model warning path to `SourcePulse` for better runtime reliability with non-readable game meshes.

## 1.0.1

- Added a world-space Nutcracker shotgun warning bar.
- Added final fire-window UI pulse and countdown.
- Added Nutcracker model fire-window warning support.
- Added debug logging and renderer audit output.
- Added fallback monitor support for observing Nutcracker combat state.

## 1.0.0

- Initial internal implementation.
