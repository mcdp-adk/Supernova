# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Supernova is a Unity DOTS (Data-Oriented Technology Stack) project that simulates supernova explosions and subsequent physical/chemical processes using a 3D cellular automaton system. It leverages Unity Entities (ECS), Burst compiler, and Job System for high-performance particle simulation.

## Technology Stack

- **Unity Version**: 6000.0.49f1
- **Core Framework**: Unity DOTS/ECS (Entities 1.3.14)
- **Rendering**: Universal Render Pipeline (URP 17.0.4)
- **Physics**: Unity Physics 1.3.14
- **Compilation**: Burst Compiler
- **Input**: Unity Input System 1.14.1

## Architecture Overview

The project follows ECS (Entity Component System) architecture with these main components:

### System Groups
```
SimulationSystemGroup
├── GlobalDataInitSystem
├── CellPoolQueueSystem
├── CaSlowSystemGroup (1s update frequency)
│   ├── SupernovaInstantiationSystem
│   ├── HeatTransferSystem
│   ├── MoistureDiffusionSystem
│   ├── EvaporationSystem
│   ├── CombustionSystem
│   ├── ExplosionSystem
│   └── CellTypeUpdateSystem
├── CaFastSystemGroup (20ms update frequency)
│   ├── GravitySystem
│   └── PhysicSystem
└── CellVFXDataSystem
```

### Key Directories
- `Assets/_Scripts/Components/` - ECS component definitions
- `Assets/_Scripts/Systems/` - Core simulation systems
- `Assets/_Scripts/Authorings/` - MonoBehaviour authoring components
- `Assets/_Scripts/Aspects/` - Entity aspects for high-level operations
- `Assets/_Scripts/Utilities/` - Helper classes and data structures
- `Assets/Settings/` - Configuration files including CSV cell configs

## Development Commands

### Build Commands
```bash
# Unity CLI build (Windows standalone)
"C:\Program Files\Unity\Hub\Editor\6000.0.49f1\Editor\Unity.exe" -quit -batchmode -buildWindows64Player "Builds/Windows/Supernova.exe"

# Clean build
Delete Library/ and Temp/ directories
```

### Testing Commands
```bash
# Run Unity Test Framework tests
"C:\Program Files\Unity\Hub\Editor\6000.0.49f1\Editor\Unity.exe" -runTests -testPlatform EditMode -testResults Results.xml

# Run play mode tests
"C:\Program Files\Unity\Hub\Editor\6000.0.49f1\Editor\Unity.exe" -runTests -testPlatform PlayMode -testResults Results.xml
```

### Performance Analysis
```bash
# Unity Profiler (requires Unity Editor)
# Use Window > Analysis > Profiler in Unity Editor

# Frame Debugger
# Use Window > Analysis > Frame Debugger in Unity Editor
```

### Common Development Tasks

#### Adding New Cell Types
1. Add new enum value in `DataStructs.cs:CellTypeEnum`
2. Add configuration in `Assets/Settings/CellConfigs.csv`
3. Update material mappings in `CellUtility.cs`
4. Add new physics rules in `PhysicSystem.cs` if needed

#### Modifying System Update Frequency
- Edit `SystemGroups.cs` to adjust `CaSlowSystemGroup` and `CaFastSystemGroup` update rates
- Modify `[UpdateInGroup]` attributes in system classes as needed

#### Debugging ECS Systems
- Use Unity Entity Debugger: Window > DOTS > Entity Debugger
- Enable Burst Inspector: Jobs > Burst > Inspector
- Use Unity Profiler DOTS mode for performance analysis

### Configuration Files
- `Assets/Settings/CellConfigs.csv` - Cell type properties and configurations
- `Assets/Settings/DefaultVolumeProfile.asset` - Post-processing settings
- `ProjectSettings/` - Unity project settings

### Performance Considerations
- All core systems use Burst compilation and Job System
- Native Collections (NativeArray, NativeHashMap) for memory efficiency
- Object pooling via CellPoolQueueSystem for entity lifecycle management
- GPU Instancing for rendering large numbers of cells

### Scene Setup
- Main scene: `Assets/Scenes/Game.unity`
- Cellular automata scene: `Assets/Scenes/Game/CellularAutomata.unity`
- Space fighter test: `Assets/Scenes/SpaceFighterTest.unity`

### Key Prefabs
- `Assets/Prefabs/Supernova Core.prefab` - Main simulation controller
- `Assets/Prefabs/Cell Config Creator.prefab` - Configuration loader
- `Assets/Prefabs/Cell Prototype Creator.prefab` - Cell prototype manager