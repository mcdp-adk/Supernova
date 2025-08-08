# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
Supernova is a Unity 6000.0.49f1 project that combines a cellular automata simulation with space fighter combat. The project uses Unity's DOTS (Data-Oriented Technology Stack) including Entities, Physics, and URP for high-performance simulation of cell-based interactions.

## Architecture

### Core Systems
- **ECS Architecture**: Uses Unity Entities for high-performance simulation
- **Cellular Automata**: Grid-based simulation with heat transfer, moisture diffusion, combustion, and explosion mechanics
- **Space Fighter Integration**: Hybrid ECS/GameObject system with physics-based movement
- **Dual Update Rates**: Separate fast (20ms) and slow (1000ms) system groups for performance optimization

### Key Components

#### Cell Simulation
- **Cell Types**: Lava, Water, Snow, Ice, Wood, Soil, Rock, etc. (defined in CellConfigs.csv)
- **State Management**: Liquid, Solid, Powder states with transitions
- **Physics**: Heat transfer, moisture diffusion, evaporation, combustion, explosions
- **Voxelization**: Dynamic voxel-based collision detection for spaceship-cell interactions

#### Space Fighter
- **Input System**: New Input System with WASD movement and mouse look
- **Physics**: Rigidbody-based movement with custom damping
- **ECS Bridge**: Hybrid component that syncs GameObject data to ECS entities

### System Groups
- **CaSlowSystemGroup**: Handles cell physics (1000ms update rate)
- **CaFastSystemGroup**: Handles spaceship physics (20ms update rate)
- **FCA Systems**: Flight Combat Aircraft systems (voxelization, collision, gravity, physics)
- **SCA Systems**: Supernova Cellular Automata systems (heat, moisture, combustion, explosions)

## Development Commands

### Unity Commands
```bash
# Open Unity project
Unity.exe -projectPath .

# Build for Windows
Unity.exe -quit -batchmode -executeMethod BuildScript.PerformBuild

# Run tests
Unity.exe -quit -batchmode -runTests -testPlatform EditMode
```

### Code Structure
```
Assets/
├── _Scripts/
│   ├── Components/          # ECS component definitions
│   ├── Systems/             # ECS system implementations  
│   ├── Authorings/          # MonoBehaviour to ECS conversion
│   ├── Aspects/             # ECS aspect definitions
│   └── Utilities/           # Shared utilities and config
├── Settings/
│   ├── CellConfigs.csv      # Cell type definitions and properties
│   └── DefaultVolumeProfile.asset
├── Prefabs/
│   ├── SpaceFighter.prefab
│   └── Supernova Core.prefab
└── Scenes/
    ├── Game.unity          # Main scene
    └── SpaceFighterTest.unity
```

### Key Files
- **GlobalConfig.cs**: Contains all simulation constants (update rates, physics parameters)
- **CellComponents.cs**: Defines all ECS components for cell simulation
- **CellConfigs.csv**: Defines cell types, properties, and behaviors
- **SpaceFighterController.cs**: Main player controller script
- **SystemGroups.cs**: Defines update rate management for ECS systems

### Configuration
- **Update Rates**: Fast systems (20ms), Slow systems (1000ms)
- **Max Cells**: 100,000 cells supported
- **Physics Scale**: Custom physics scaling with impulse factors
- **Temperature/Heat**: Full thermal simulation with ignition/explosion points
- **Moisture**: Water cycle simulation with evaporation/condensation

### ECS System Flow
1. **Initialization**: S0_GlobalDataInitSystem loads config and initializes data
2. **Pool Management**: S1_CellPoolQueueSystem manages cell entity lifecycle
3. **Physics**: FCA systems handle spaceship voxelization and collision
4. **Simulation**: SCA systems process cell interactions in order:
   - Heat transfer → Moisture diffusion → Evaporation → Combustion → Explosion
5. **Rendering**: Custom VFX systems for cell visualization

### Performance Notes
- Uses Burst compilation for performance-critical systems
- Native containers for efficient memory access
- Variable update rates to balance accuracy vs performance
- Entity pooling for cell lifecycle management