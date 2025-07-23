using _Scripts.Aspects;
using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(VariableRateCellularAutomataSystemGroup))]
    [UpdateAfter(typeof(CellInstantiationFromSupernovaSystem))]
    public partial struct GravitySystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<IsCellAlive>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // 获取全局数据容器引用
            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            var gravityJob = new GravityJob
            {
                manager = state.EntityManager,
                cellMap = _cellMap
            };

            state.Dependency = gravityJob.Schedule(state.Dependency);
            state.Dependency.Complete();
        }

        [BurstCompile]
        [WithAll(typeof(IsCellAlive))]
        private partial struct GravityJob : IJobEntity
        {
            public EntityManager manager;
            public NativeHashMap<int3, Entity> cellMap;

            private void Execute(CellAspect cell)
            {
                CellUtility.TryMoveCell(cell.Self, manager, cellMap,
                    cell.Coordinate - new int3(1, 0, 0));
            }
        }
    }
}