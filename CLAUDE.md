# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

这是一个基于 Unity ECS (Entity Component System) 架构的 Supernova 模拟项目，专注于细胞自动机和粒子系统模拟。项目使用 DOTS (Data-Oriented Tech Stack) 进行高性能计算，并通过 VFX Graph 实现视觉效果。

## 技术架构

### 核心框架
- **Unity DOTS/ECS**: 使用 Entities 包进行数据导向设计
- **Visual Effect Graph**: 基于 GPU 的粒子系统渲染
- **Burst Compiler**: 通过 [BurstCompile] 特性优化性能
- **Job System**: 并行计算框架

### 代码结构

```
Assets/_Scripts/
├── Components/          # ECS 组件定义
│   ├── CellComponents.cs    # 细胞相关组件
│   └── ConfigComponents.cs  # 配置相关组件
├── Systems/            # ECS 系统
│   ├── CellVFXDataSystem.cs    # 细胞VFX数据传输系统
│   ├── PhysicSystem.cs         # 物理系统
│   └── S1S1_InstantiationFromSupernovaSystem.cs # 实例化系统
├── Authorings/         # MonoBehaviour 到 ECS 的桥接
│   ├── CellConfigCreator.cs    # 细胞配置创建器
│   └── CellPrototypeCreator.cs # 细胞原型创建器
├── Aspects/            # ECS 方面定义
├── Utilities/          # 工具类和数据结构
│   ├── GlobalConfig.cs       # 全局配置
│   └── DataStructs.cs        # 数据结构定义
└── Prefabs/            # 预制体
    ├── Cell VFX.vfx         # 细胞视觉特效
    └── Cell Config Creator.prefab
```

### 主要系统流程

1. **初始化流程**: CellConfigCreator 从 CSV 文件读取配置 → 创建 CellConfigEntity
2. **实例化流程**: SupernovaAuthoring 触发实例化 → S1S1_InstantiationFromSupernovaSystem 创建细胞实体
3. **物理模拟**: PhysicSystem 处理细胞运动和交互
4. **VFX 同步**: CellVFXDataSystem 将实体数据同步到 Visual Effect Graph

## 开发命令

### Unity 编辑器命令
```bash
# 打开 Unity 项目
Unity -projectPath .

# 运行测试场景
Unity -projectPath . -executeMethod UnityEditor.SceneManagement.EditorSceneManager.OpenScene -scene Assets/Scenes/Test.unity
```

### 构建命令
```bash
# Windows 构建
Unity -quit -batchmode -projectPath . -executeMethod UnityEditor.BuildPipeline.BuildPlayer -scene Assets/Scenes/Test.unity -outputPath Build/Windows/Supernova.exe -targetPlatform StandaloneWindows64

# Android 构建
Unity -quit -batchmode -projectPath . -executeMethod UnityEditor.BuildPipeline.BuildPlayer -scene Assets/Scenes/Test.unity -outputPath Build/Android/Supernova.apk -targetPlatform Android
```

### 调试和测试
```bash
# 打开 Unity Profiler
Unity -projectPath . -profiler-enable

# 运行性能测试（如果存在）
Unity -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml
```

## 关键配置

### CSV 配置格式
位于 `Assets/Settings/CellConfigs.csv`：
```csv
ID,Type,State,Mass,...
1,RedBloodCell,Active,10
2,WhiteBloodCell,Inactive,15
```

### 全局配置
在 `Assets/_Scripts/Utilities/GlobalConfig.cs` 中定义：
- `MaxCellCount`: 最大细胞数量限制
- 各种物理和渲染参数

## 重要文件路径

- **主场景**: `Assets/Scenes/Test.unity`
- **VFX 资源**: `Assets/Prefabs/Cell VFX.vfx`
- **配置文件**: `Assets/Settings/CellConfigs.csv`
- **系统代码**: `Assets/_Scripts/Systems/`

## 开发提示

1. **ECS 调试**: 使用 Unity 的 Entity Debugger 窗口查看实体和组件状态
2. **性能优化**: 标记为 [BurstCompile] 的 Job 会自动优化，确保使用 Burst Inspector 检查生成的代码
3. **VFX 绑定**: CellVFXDataSystem 中定义的 Shader 属性名必须与 VFX Graph 中的属性名完全匹配
4. **内存管理**: 使用 NativeArray/NativeList 时务必正确 Dispose，避免内存泄漏