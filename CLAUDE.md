# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个基于 Unity DOTS/ECS 架构的 2D 细胞自动机物理模拟项目，模拟 Supernova 超新星的物质演化过程。项目使用 Unity 6000.0.49f1 版本，通过 ECS + VFX Graph 实现高性能的粒子系统模拟。

## 技术栈

- **Unity 版本**: 6000.0.49f1
- **核心架构**: DOTS (Data-Oriented Tech Stack)
- **ECS 版本**: Entities 1.3.14
- **物理系统**: Unity Physics 1.3.14
- **渲染管线**: URP 17.0.4 + VFX Graph 17.0.4
- **Burst 编译器**: 启用 [BurstCompile] 优化
- **热重载**: Hot Reload 支持实时调试

## 架构概览

### 系统分组架构
```
SimulationSystemGroup
├── GlobalDataSystem (初始化)
├── CaSlowSystemGroup (500ms 更新)
│   └── GravitySystem (重力计算)
└── CaFastSystemGroup (20ms 更新)
    └── PhysicSystem (碰撞与运动)
```

### 数据流
1. **配置加载**: CellConfigCreator 读取 CSV → 创建 CellConfigEntity
2. **实体池**: GlobalDataSystem 预创建实体池 → CellPoolQueue
3. **实例化**: SupernovaAuthoring → SCA_InstantiationFromSupernovaSystem
4. **物理模拟**: PhysicSystem 处理细胞运动和碰撞
5. **VFX 同步**: LS_CellVFXDataSystem 将 ECS 数据同步到 VFX Graph

## 核心数据模型：无状态物理系统

### 设计哲学
本系统**没有状态机**，所有行为由物理法则驱动。Cell 的"类型变化"只是物理属性达到阈值时的自然结果。

### 属性分类

**动态属性**（每 tick 实时计算）：
- `Temperature` - 热传导系统计算
- `Moisture` - 湿度扩散系统计算  
- `Energy` - 燃烧系统计算消耗
- `Velocity` - 物理系统计算运动

**静态基准**（CSV 物理常数）：
- `Mass_Default` - 基础质量
- `HeatConductivity` - 导热系数
- `Fluidity/Viscosity` - 流体/摩擦系数
- 相变阈值：`Temperature_Min/Max`, `IgnitionPoint`

### 物质物理状态
- **Solid** - 刚体，不可流动
- **Liquid** - 可流动，密度分层
- **Powder** - 颗粒，可堆叠

### 无状态转换机制
```
温度变化 → 达到阈值 → 物质类型改变
湿度变化 → 达到阈值 → 物质类型改变  
能量消耗 → 燃尽阈值 → 物质类型改变
```

### 空间数据结构
- **CellMap**: NativeHashMap<int3, Entity> - 3D 空间哈希
- **实体池**: NativeQueue<Entity> - 预创建池
- **最大容量**: 100,000 个 Cell

## 关键配置文件

### Cell 配置表 (Assets/Settings/CellConfigs.csv)
包含 21 种物质类型，每种定义：
- 基础属性: 质量、状态、流体性、粘度
- 热力学: 温度范围、导热系数、燃点、爆点
- 资源掉落: 金、银、铜、铁的掉落概率

### 全局配置 (GlobalConfig.cs)
```csharp
MaxCellCount = 100000          // 最大实体数
CellMapInitialCapacity = 65536 // 空间哈希容量
MaxSpeed = 50f                // 最大速度限制
PhysicsSpeedScale = 1f        // 物理倍率
```

## 开发工作流

### 运行项目
```bash
# 直接打开 Unity 项目
Unity -projectPath .

# 打开特定场景
Unity -projectPath . -executeMethod UnityEditor.SceneManagement.EditorSceneManager.OpenScene -scene Assets/Scenes/Test.unity
```

### 构建项目
```bash
# Windows 64位构建
Unity -quit -batchmode -projectPath . -executeMethod UnityEditor.BuildPipeline.BuildPlayer -scene Assets/Scenes/Test.unity -outputPath Build/Windows/Supernova.exe -targetPlatform StandaloneWindows64

# Android 构建
Unity -quit -batchmode -projectPath . -executeMethod UnityEditor.BuildPipeline.BuildPlayer -scene Assets/Scenes/Test.unity -outputPath Build/Android/Supernova.apk -targetPlatform Android
```

### 调试工具
```bash
# 启用 Unity Profiler
Unity -projectPath . -profiler-enable

# 运行性能测试
Unity -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml
```

## 重要文件路径

- **主场景**: `Assets/Scenes/Test.unity`
- **Cell 配置**: `Assets/Settings/CellConfigs.csv`
- **VFX 资源**: `Assets/Prefabs/Cell VFX.vfx`
- **系统代码**: `Assets/_Scripts/Systems/`
- **配置预制体**: `Assets/Prefabs/Cell Config Creator.prefab`

## 开发提示

### ECS 调试
- 使用 **Entity Debugger** 窗口查看实体和组件状态
- 检查 **Systems** 标签页的更新频率和性能
- 关注 **Memory** 面板中的 NativeContainer 使用情况

### 性能优化
- 所有核心系统启用 [BurstCompile] 和 [UpdateInGroup] 优化
- 使用 NativeContainer 时务必在 OnDestroy 中 Dispose
- CellMap 使用 int3 作为 key，避免浮点精度问题

### VFX 绑定
- LS_CellVFXDataSystem 负责 ECS → VFX Graph 数据同步
- Shader 属性名必须与 VFX Graph 中的 Exposed Property 精确匹配
- 支持属性: Position, Scale, Color, CellType

### 内存管理
- GlobalDataSystem 负责所有 NativeContainer 的生命周期
- CellPoolQueue 预创建实体避免运行时实例化开销
- 使用 EntityCommandBuffer 批量处理实体操作