# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Supernova** is a Unity 3D cellular automata simulation project using Unity DOTS (Data-Oriented Technology Stack) with Entities, Physics, and Universal Render Pipeline. The project simulates complex particle interactions including heat transfer, moisture diffusion, combustion, and explosions.

## Key Technologies
- Unity 6000.0.49f1
- Unity Entities (DOTS) 1.3.14
- Unity Physics 1.3.14
- Universal Render Pipeline (URP) 17.0.4
- Visual Effect Graph (VFX Graph)
- Burst Compiler
- Cinemachine 3.1.4

## Architecture Overview

### System Organization
The project uses a hierarchical system organization with custom system groups:

- **CaSlowSystemGroup**: Updates at 500ms intervals for slow processes
- **CaFastSystemGroup**: Updates at 20ms intervals for fast processes
- **FCA (Fast Cellular Automata) Systems**: Physics and gravity systems
- **SCA (Slow Cellular Automata) Systems**: State updates and complex interactions
- **LS (Late Systems)**: VFX and rendering updates

### Core Components
- **Cell-based Simulation**: Uses ECS entities representing different material types
- **Material Properties**: Temperature, moisture, energy, mass, velocity
- **Physics Simulation**: Gravity, collision detection, impulse-based movement
- **State Management**: Cell type transitions based on environmental conditions

### Data Flow
1. **Initialization**: `GlobalDataInitSystem` sets up cell pool and configurations
2. **Physics**: `FCA0_PhysicSystem` and `FCA1_GravitySystem` handle movement
3. **State Updates**: `SCA0_CellTypeUpdateSystem` determines cell type changes
4. **Interactions**: Heat transfer, moisture diffusion, combustion, explosions
5. **Rendering**: `LS0_CellVFXDataSystem` updates visual effects

## Development Commands

### Unity Editor Commands
- **Open Project**: Use Unity Hub with version 6000.0.49f1
- **Play Mode**: Press Play in Unity Editor to run simulation
- **Scene**: Open `Assets/Scenes/Test/CellularAutomata.unity` for main simulation

### Build Process
- **Build Location**: Unity standard build process via File > Build Settings
- **Platforms**: Windows (primary), supports cross-platform via URP
- **Build Settings**: Use PC_RPAsset for desktop builds

### Configuration Files
- **Cell Types**: `Assets/Settings/CellConfigs.csv` - material properties and behaviors
- **Render Pipeline**: `Assets/Settings/PC_RPAsset.asset` for desktop rendering
- **Scene**: `Assets/Scenes/Test/CellularAutomata.unity` - main test scene

### Key Scripts Structure
```
Assets/_Scripts/
├── Components/          # ECS component definitions
├── Systems/            # System implementations organized by update frequency
├── Authorings/         # MonoBehaviour-to-ECS conversion scripts
├── Aspects/           # ECS aspects for efficient queries
└── Utilities/         # Shared utilities and constants
```

### System Categories
- **FCA Systems** (Fast): Physics, gravity, movement
- **SCA Systems** (Slow): Type updates, heat transfer, moisture, combustion
- **LS Systems** (Late): VFX updates and rendering

## Testing
- **Test Scene**: `Assets/Scenes/Test.unity` contains test setup
- **Prefab**: `Assets/Prefabs/Supernova Core.prefab` - main simulation prefab
- **Cell Creator**: `Assets/Prefabs/Cell Config Creator.prefab` - configuration tool

## Performance Considerations
- Uses Entity pooling to avoid allocation overhead
- Native containers for high-performance data access
- Burst-compiled jobs for parallel processing
- Variable update rates for different system types