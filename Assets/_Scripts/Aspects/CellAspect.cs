using _Scripts.Components;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace _Scripts.Aspects
{
    public readonly partial struct CellAspect : IAspect
    {
        // 实体引用
        public readonly Entity Self;

        // 系统组件
        public readonly RefRW<LocalTransform> LocalTransform;
        public readonly RefRW<MaterialMeshInfo> MaterialMeshInfo;

        // 状态标识
        public readonly EnabledRefRW<IsAlive> IsAlive;

        // 基本属性
        public readonly RefRW<CellType> CellType;
        public readonly RefRW<CellState> CellState;

        // 物理属性
        public readonly RefRW<Mass> Mass;
        public readonly RefRW<Velocity> Velocity;
        public readonly EnabledRefRW<Velocity> VelocityEnabled;
        public readonly RefRW<Temperature> Temperature;

        // 化学属性
        public readonly RefRW<Energy> Energy;

        // Buffer
        public readonly DynamicBuffer<ImpulseBuffer> ImpulseBuffer;
    }
}