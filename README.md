# Nutcracker Fire Warning

Nutcracker Fire Warning is a BepInEx plugin for Lethal Company that provides
client-side shotgun timing warnings for Nutcracker enemies.

The plugin reads the Nutcracker's current aiming state and shotgun timing, then
displays warnings through a world-space side bar and an optional red-white model
pulse. It does not change enemy behavior, weapon damage, networking, or game
balance.

## Features

- World-space warning bar attached beside each Nutcracker.
- Countdown during Nutcracker aiming.
- Red-white final fire-window pulse.
- Optional model warning for close-range visibility.
- Optional screen-space fallback rectangle.
- Yellow pre-aim danger indicator when the local player is in a dangerous shotgun line.
- Configurable warning distances, pulse intensity, fire-window timing, and fallback scan intervals.

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

- `EnableUiFireWindow`: Enables the side-bar `FIRE` text and red-white pulse.
- `EnableModelOutlineFireWindow`: Enables the Nutcracker model warning.
- `ModelOutlineMode`: Selects `MeshSilhouette` or `ScreenBox`.
- `ModelPulseMode`: Selects `SourcePulse`, `CloneShell`, or `Both`.
- `ModelPulseIntensity`: Controls model pulse emission intensity.
- `ModelPulseAlpha`: Controls model pulse color alpha.
- `ModelWarningMaxDistance`: Limits model warning distance. Set to `0` or lower to disable distance filtering.
- `ModelWarningRequireCameraVisible`: Requires the Nutcracker to be inside the local camera viewport.
- `ModelOutlineWidth`: Controls ScreenBox line width and CloneShell expansion width.
- `MeshOutlineScale`: Extra scale multiplier for CloneShell mode.
- `FireWindowSeconds`: Seconds before predicted firing when final warnings activate.
- `PreAimMaxDistance`: Maximum distance for the yellow pre-aim danger bar.

### Performance

- `MonitorActiveScanInterval`: Fallback monitor scan interval while Nutcrackers are present.
- `MonitorIdleScanInterval`: Fallback monitor scan interval while no Nutcrackers are present.

### Debug

- `EnableDebugLogs`: Logs warning state and monitor activity.
- `DumpModelAudit`: Logs Nutcracker renderer and mesh information when model warning data is built.

Debug logging is intended for troubleshooting and should usually be disabled
during normal play.

## Notes

`SourcePulse` is the recommended model warning mode. The original Nutcracker
mesh is not readable at runtime, so precise normal-expanded mesh outlines can be
unreliable in `CloneShell` mode on unmodified game assets.

## License

Nutcracker Fire Warning is licensed under the MIT License. See `LICENSE` for
details.
