# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述
Supernova 是一个 Unity ECS（实体组件系统）项目，用于模拟具有物理、燃烧和爆炸系统的细胞自动机。该项目使用 Unity DOTS（面向数据的技术栈）实现高性能模拟。

## 关键技术
- **Unity 版本**：6000.0.49f1
- **ECS**：Unity.Entities 包（1.3.14）
- **物理**：Unity.Physics 包
- **渲染**：Universal Render Pipeline（URP）
- **输入**：Input System 包
- **特效**：Visual Effect Graph

## 系统架构

### 核心系统结构
- **CaSlowSystemGroup**：每 1000ms 更新一次，用于重型计算（热传递、燃烧、爆炸）
- **CaFastSystemGroup**：每 20ms 更新一次，用于响应式物理和交互
- **VariableRateSimulationSystemGroup**：可变频率系统的基础组

### 关键组件
- **CellComponents.cs**：核心 ECS 组件（CellType、Temperature、Energy 等）
- **ConfigComponents.cs**：配置数据组件
- **SupernovaComponents.cs**：Supernova 特定组件

### 主要系统（按执行顺序）
1. **S0_GlobalDataInitSystem**：初始化全局模拟数据和单元格配置
2. **S1_CellPoolQueueSystem**：管理单元格池以实现高效生成
3. **SCA1_SupernovaInstantiationSystem**：创建超新星爆炸
4. **SCA2_RandomInstantiationSystem**：随机单元格生成
5. **SCA3_HeatTransferSystem**：单元格间的热力学
6. **SCA4_MoistureDiffusionSystem**：水/水分移动
7. **SCA5_EvaporationSystem**：基于温度的水蒸发
8. **SCA6_CombustionSystem**：带能量释放的火灾/燃烧模拟
9. **SCA7_ExplosionSystem**：带冲击波的爆炸反应
10. **FCA9_PhysicSystem**：移动和碰撞的物理模拟

### 数据流
1. **CellConfig**：从 CSV 加载的配置数据（Assets/Settings/CellConfigs.csv）
2. **GlobalConfig**：代码中的静态配置常量
3. **单元格地图**：NativeHashMap<int3, Entity> 用于空间查找
4. **缓冲区**：ImpulseBuffer、HeatBuffer、MoistureBuffer 用于单元格间通信

## 构建命令
- **Unity 构建**：使用 Unity 编辑器 → 文件 → 构建并运行
- **测试场景**：Assets/Scenes/Test/CellularAutomata.unity
- **主场景**：Assets/Scenes/Test.unity

## 开发设置
1. **在 Unity 中打开**：使用 Unity 6000.0.49f1 或更新版本
2. **包安装**：所有包通过 manifest.json 管理
3. **配置**：编辑 CellConfigs.csv 以修改单元格属性
4. **常量**：修改 GlobalConfig.cs 中的模拟参数

## 关键配置文件
- **CellConfigs.csv**：定义所有单元格类型及其属性
- **GlobalConfig.cs**：模拟常量和系数
- **InputSystem_Actions.inputactions**：输入系统配置
- **Settings/*.asset**：URP 和渲染配置

## 常见开发任务
- **添加新单元格类型**：添加条目到 CellConfigs.csv 和 CellTypeEnum
- **修改物理**：编辑 GlobalConfig.cs 中的物理常量
- **调整燃烧**：修改 GlobalConfig 中的燃烧系数
- **测试**：使用 Test.unity 场景进行快速迭代

## 性能考量
- **最大单元格数**：100,000（在 GlobalConfig.MaxCellCount 中定义）
- **单元格池大小**：65,536（在 GlobalConfig.MaxCellPoolSize 中定义）
- **更新频率**：慢系统 1Hz，快系统 50Hz
- **内存**：使用 NativeArray/NativeHashMap 进行 burst 编译