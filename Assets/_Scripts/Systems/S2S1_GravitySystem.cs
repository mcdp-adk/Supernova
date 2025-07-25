using _Scripts.Aspects;
using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CellPendingChangeSystemGroup))]
    public partial struct GravitySystem : ISystem
    {
        private struct SupernovaData
        {
            public int3 Coordinate;
            public int Mass;
        }

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

            // 获取超新星数据
            var supernovaDataList = new NativeList<SupernovaData>(Allocator.TempJob);
            foreach (var supernova in SystemAPI.Query<SupernovaAspect>())
                supernovaDataList.Add(new SupernovaData
                {
                    Coordinate = supernova.Coordinate,
                    Mass = supernova.Mass
                });

            var gravityJob = new GravityJob
            {
                CellMap = _cellMap,
                SupernovaDataArray = supernovaDataList.AsArray()
            };

            state.Dependency = gravityJob.Schedule(state.Dependency);
            state.Dependency.Complete();
            supernovaDataList.Dispose();
        }

        [BurstCompile]
        [WithAll(typeof(IsCellAlive))]
        private partial struct GravityJob : IJobEntity
        {
            public NativeHashMap<int3, Entity> CellMap;
            [ReadOnly] public NativeArray<SupernovaData> SupernovaDataArray;

            private void Execute(CellAspect cell)
            {
                foreach (var data in SupernovaDataArray)
                {
                    var _ = data.Coordinate.x + data.Coordinate.y + data.Coordinate.z + data.Mass;
                }
                
                CellUtility.TryMoveCell(cell.Self, ref cell.LocalTransform.ValueRW,
                    CellMap, cell.Coordinate + new int3(0, -1, 0));
            }
        }
    }
}