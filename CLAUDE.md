# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目核心架构

**Supernova** 是基于 Unity DOTS 的超新星粒子物理模拟，使用 ECS 架构实现高效细胞自动机系统。

### 系统更新频率
- **CaSlowSystemGroup**: 500ms 更新 - 引力计算 + 粒子生成
- **CaFastSystemGroup**: 20ms 更新 - 物理模拟 + 碰撞检测
- **LateSimulation**: VFX 数据同步

### 核心数据流
```mermaid
graph LR
    A[SupernovaAuthoring] --> B[CellPrototypeCreator]
    B --> C[GlobalDataSystem]
    C --> D[对象池初始化]
    D --> E[65,536 细胞实体]
    E --> F[InstantiationFromSupernovaSystem]
    F --> G[PhysicSystem]
    G --> H[CellVFXDataSystem]
```

## 开发命令

### Unity 编辑器
```bash
# 打开项目
Unity.exe -projectPath .

# 命令行构建（需创建 BuildScript）
Unity.exe -quit -batchmode -executeMethod BuildScript.Build
```

### 调试工具
- **Entity Debugger**: Window → DOTS → Entity Debugger
- **Profiler**: Window → Analysis → Profiler → 启用 DOTS 模块
- **Scene 视图**: 显示超新星生成范围 Gizmos

### 关键配置常量
- `MaxCellPoolSize = 65536` - 最大细胞数量
- `MaxSpeed = 5f` - 粒子最大速度
- `MaxCellCount = 10000` - VFX 最大渲染数量

## 系统依赖关系
```mermaid
graph TD
    GlobalDataSystem --> |初始化| CaSlowSystemGroup
    GlobalDataSystem --> |初始化| CaFastSystemGroup
    S1S1_InstantiationFromSupernovaSystem --> |使用| GlobalDataSystem.CellPoolQueue
    PhysicSystem --> |使用| GlobalDataSystem.CellMap
    GravitySystem --> |使用| SupernovaAspect
    CellVFXDataSystem --> |收集| 所有 IsAlive 实体
```

## 快速开发检查清单

### 新增功能前检查
1. 确认系统分组归属（Slow/Fast/Late）
2. 验证 NativeContainer 生命周期管理
3. 检查 BurstCompile 兼容性
4. 测试 Entity Debugger 中组件状态

### 常见修改点
- **新增粒子类型**: 扩展 CellTypeEnum 和 CellStateEnum
- **修改物理规则**: 编辑 PhysicSystem.TryMoveCellJob
- **调整引力公式**: 修改 GravitySystem 计算逻辑
- **增强 VFX**: 扩展 CellVFXDataSystem 数据收集

### 性能调优要点
- 使用 Burst 编译所有 Job
- 验证 NativeContainer 正确释放
- 监控 Entity Debugger 中实体数量
- Profiler 中检查 System 执行时间