# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Supernova** is a Unity 6000.0.49f1 project using DOTS/ECS architecture for a cellular automata simulation game. The project simulates 80,000+ cells with realistic physics, heat transfer, moisture diffusion, combustion, and explosion effects.

## Unity Version & Dependencies

- **Unity Version**: 6000.0.49f1
- **Key Packages**:
  - `com.unity.entities` (1.3.14) - ECS framework
  - `com.unity.physics` (1.3.14) - Physics simulation
  - `com.unity.render-pipelines.universal` (17.0.4) - URP rendering
  - `com.unity.visualeffectgraph` (17.0.4) - VFX system
  - `com.unity.inputsystem` (1.14.1) - Input handling

## Architecture Overview

### ECS System Architecture

The project uses a **three-tier ECS system** organized by update frequency:

```
VariableRateSimulationSystemGroup
├── CaSlowSystemGroup (1000ms updates)
│   └── SCA* Systems (Cellular automata)
├── CaFastSystemGroup (20ms updates)  
│   └── FCA* Systems (Physics & collision)
└── Lifecycle Systems (LS*, S*)
    ├── Global initialization
    └── Entity pooling
```

### System Categories

- **FCA (Fast Cellular Automata)**: High-frequency physics (collision, gravity, movement)
- **SCA (Slow Cellular Automata)**: Low-frequency simulation (heat, moisture, combustion, explosions)
- **LS (Lifecycle Systems)**: Entity management and VFX processing

### Core Components

- **Cell Components**: `CellType`, `CellState`, `Temperature`, `Moisture`, `Energy`, `Mass`, `Velocity`
- **Buffer Components**: `HeatBuffer`, `MoistureBuffer`, `ImpulseBuffer` for state accumulation
- **Lifecycle**: `IsAlive`, `IsBurning`, `ShouldExplosion`, `PendingDequeue`

## Development Commands

### Build Commands
```bash
# Unity CLI build (if available)
Unity.exe -quit -batchmode -executeMethod BuildScript.Build

# Manual build process
1. Open Unity Editor
2. File → Build Settings
3. Select platform and build
```

### Testing Commands
```bash
# Unity Test Framework
Window → General → Test Runner
# or use Unity CLI:
Unity.exe -runTests -testPlatform EditMode -testResults results.xml
```

### Common Development Tasks

#### Running the Game
1. Open `Scenes/Game.unity`
2. Press Play in Unity Editor
3. Use WASD for spaceship movement, mouse for aiming
4. Press Tab to open tool menu

#### Adding New Cell Types
1. Add to `CellTypeEnum` in `CellComponents.cs`
2. Update `cellTypeIndexMap` in `GameManager.cs:27`
3. Add configuration in `Settings/CellConfigs.csv`
4. Update relevant systems (SCA9_CellTypeUpdateSystem)

#### Modifying System Timing
- Update rates: `GlobalConfig.cs` (lines 11-16)
- Physics scaling: `GlobalConfig.PhysicsSpeedScale`

#### Debugging ECS
- Use Entity Debugger: Window → Analysis → Entity Debugger
- Check system execution order and component data
- Monitor entity count and memory usage

### Key Files Structure

```
Assets/_Scripts/
├── Components/          # ECS components (CellComponents, ConfigComponents)
├── Systems/            # ECS systems (FCA*, SCA*, LS*)
├── Aspects/            # ECS aspects (SupernovaAspect)
├── Authorings/         # MonoBehaviour → ECS conversion
├── Utilities/          # GlobalConfig, SystemGroups, DataStructs
└── GameManager.cs      # Main game controller (non-ECS)
```

### Performance Monitoring
- **Entity Count**: 80,000 max cells (GlobalConfig.MaxCellCount)
- **Update Rates**: Slow (1s), Fast (20ms)
- **Memory**: Pre-allocated pools for entities and buffers
- **Parallel Processing**: All systems use Burst + Job System

### Configuration Files
- `Settings/CellConfigs.csv` - Cell type physical properties
- `GlobalConfig.cs` - Simulation constants and tuning parameters
- `ProjectSettings/` - Unity project configuration

## Development Notes

- **Spatial Indexing**: Uses `NativeHashMap<int3, Entity>` for O(1) spatial lookups
- **State Management**: Double buffering prevents race conditions
- **Entity Pooling**: Pre-allocated entities managed via `NativeQueue<Entity>`
- **Enableable Components**: Used for efficient filtering and state management