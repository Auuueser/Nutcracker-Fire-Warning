# Nutcracker Fire Warning

Nutcracker Fire Warning is a BepInEx plugin for Lethal Company that provides
client-side shotgun timing warnings for Nutcracker enemies.

The plugin reads the Nutcracker's current aiming state and shotgun timing, then
displays warnings through a world-space side bar, optional red-white model pulse,
and optional model state tint. It does not change enemy behavior, weapon damage,
networking, or game balance.

## Features

- World-space warning bar attached beside each Nutcracker.
- Countdown during Nutcracker aiming.
- Red-white final fire-window pulse.
- Recommended Nutcracker model state tint: white while chasing, red during the final fire window.
- Optional extra fire-window overlay for red-white pulse, clone shell, or screen-space fallback.
- Optional screen-space fallback rectangle.
- Yellow pre-aim danger indicator when the local player is in a dangerous shotgun line.
- Configurable warning distances, pulse intensity, fire-window timing, and fallback scan intervals.
- English config descriptions by default, with automatic Simplified Chinese descriptions when LC Chinese Project is detected.

## Timing

The warning timer follows the Nutcracker's in-game firing logic:

- `0.5s` when the Nutcracker has `enemyHP <= 1`
- `1.3s` when the held shotgun has one shell loaded
- `1.75s` otherwise

## Requirements

- Lethal Company
- BepInEx
- .NET SDK with `netstandard2.1` support

## Build

Before building, update `GameDir` in
`NutcrackerFireWarning/NutcrackerFireWarning.csproj` so it points to a local
Lethal Company installation or BepInEx profile containing the required managed
assemblies.

```powershell
dotnet build .\NutcrackerFireWarning\NutcrackerFireWarning.csproj
```

The compiled plugin is written to:

```text
NutcrackerFireWarning\tmpbin\NutcrackerFireWarning.dll
```

Install the DLL into:

```text
BepInEx\plugins\
```

## Configuration

The plugin creates this config file after first launch:

```text
BepInEx\config\aueser.lethalcompany.nutcrackerfirewarning.cfg
```

### Warnings

- `EnableMod`: Enables Nutcracker Fire Warning as a whole. When disabled, warning UI, model warnings, and fallback scans stop running.
- `EnableUiFireWindow`: Enables the world-space side warning bar, including countdown, `FIRE` pulse, reload bar, and pre-aim danger bar.
- `EnableModelStateTint`: Enables the recommended model state warning. Default: `true`. The model turns white while chasing a target and red during the final fire window.
- `EnableModelOutlineFireWindow`: Enables an extra fire-window-only model overlay. Default: `false`. Use it only when you also want the old red-white pulse, clone shell, or screen-space fallback.
- `ModelOutlineMode`: Selects `MeshSilhouette` or `ScreenBox`.
- `ModelPulseMode`: Selects `SourcePulse`, `CloneShell`, or `Both`.
- `ModelPulseIntensity`: Controls model pulse emission intensity.
- `ModelPulseAlpha`: Controls model pulse color alpha.
- `ModelChaseTintAlpha`: Controls white chase-state model tint alpha.
- `ModelChaseTintIntensity`: Controls white chase-state model tint emission intensity.
- `ModelFireWindowTintAlpha`: Controls red fire-window model tint alpha.
- `ModelFireWindowTintIntensity`: Controls red fire-window model tint emission intensity.
- `ModelWarningMaxDistance`: Limits model warning distance. Set to `0` or lower to disable distance filtering.
- `ModelWarningRequireCameraVisible`: Requires the Nutcracker to be inside the local camera viewport.
- `ModelOutlineWidth`: Controls ScreenBox line width and CloneShell expansion width.
- `MeshOutlineScale`: Extra scale multiplier for CloneShell mode.
- `FireWindowSeconds`: Seconds before predicted firing when final warnings activate.
- `PreAimMaxDistance`: Maximum distance for the yellow pre-aim danger bar. Set to `0` or lower to disable the pre-aim bar.

### Performance

- `MonitorActiveScanInterval`: Fallback monitor scan interval while Nutcrackers are present. Default: `0.5`.
- `MonitorIdleScanInterval`: Fallback monitor scan interval while no Nutcrackers are present. Default: `2.0`.

### Debug

- `EnableDebugLogs`: Logs warning state and monitor activity.
- `DumpModelAudit`: Logs Nutcracker renderer and mesh information when model warning data is built.

Debug logging is intended for troubleshooting and should usually be disabled
during normal play.

### Config Language

Config entry names remain stable in English to avoid creating duplicate sections
after upgrades. Config descriptions are written in English by default.

When LC Chinese Project is installed, new config files and newly added config
entries use Simplified Chinese descriptions. Detection checks the stable plugin
GUID `cn.codex.v81testchn` or the installed `V81TestChn.dll` path; it does not
depend on the localization mod version number.

## Notes

`SourcePulse` is the recommended model warning mode. The original Nutcracker
mesh is not readable at runtime, so precise normal-expanded mesh outlines can be
unreliable in `CloneShell` mode on unmodified game assets.

## License

Nutcracker Fire Warning is licensed under the MIT License. See `LICENSE` for
details.
