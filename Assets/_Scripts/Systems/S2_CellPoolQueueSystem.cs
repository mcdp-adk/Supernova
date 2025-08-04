using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace _Scripts.Systems
{
    public partial struct CellPoolQueueSystem : ISystem
    {
        private NativeQueue<Entity> _cellPoolQueue;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            // 初始化 CellPoolQueue
            if (!_cellPoolQueue.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellPoolQueue = globalDataSystem.CellPoolQueue;
            }
        }
    }
}