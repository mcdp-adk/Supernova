using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaFastSystemGroup), OrderFirst = true)]
    public partial struct SpaceshipVoxelizationSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;
        private EntityQuery _tempCellQuery;
        private DynamicBuffer<SpaceshipColliderBuffer> _colliderBuffer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _tempCellQuery = SystemAPI.QueryBuilder().WithAll<SpaceshipTempCellTag>().Build();
            state.RequireForUpdate<SpaceshipProxyTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            // 销毁所有旧的临时单元格
            state.EntityManager.DestroyEntity(_tempCellQuery);

            // 结构性变更后必须重新获取 buffer
            _colliderBuffer = SystemAPI.GetSingletonBuffer<SpaceshipColliderBuffer>();

            // 使用 Entity Command Buffer 进行批量创建
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var collider in _colliderBuffer)
                CreateVoxelsForCollider(collider, ecb);
            ecb.Playback(state.EntityManager);
        }

        #region CreateVoxelsForCollider

        [BurstCompile]
        private void CreateVoxelsForCollider(SpaceshipColliderBuffer collider, EntityCommandBuffer ecb)
        {
            // 计算旋转后的 AABB 边界
            var bounds = GetRotatedBounds(collider);

            // 遍历边界内的所有整数坐标
            for (var x = bounds.min.x; x <= bounds.max.x; x++)
            for (var y = bounds.min.y; y <= bounds.max.y; y++)
            for (var z = bounds.min.z; z <= bounds.max.z; z++)
            {
                var cellPos = new int3(x, y, z);

                // 检查单元格是否与碰撞体相交
                if (IsIntersecting(cellPos, collider))
                    CreateVoxelEntity(cellPos, ecb);
            }
        }

        [BurstCompile]
        private (int3 min, int3 max) GetRotatedBounds(SpaceshipColliderBuffer collider)
        {
            var halfSize = collider.Size * 0.5f;
            var min = new float3(float.MaxValue);
            var max = new float3(float.MinValue);

            // 计算角点并找出边界
            for (var i = 0; i < 8; i++)
            {
                var corner = new float3(
                    (i & 1) == 0 ? -halfSize.x : halfSize.x,
                    (i & 2) == 0 ? -halfSize.y : halfSize.y,
                    (i & 4) == 0 ? -halfSize.z : halfSize.z
                );

                var worldPoint = math.mul(collider.Rotation, corner) + collider.Center;
                min = math.min(min, worldPoint);
                max = math.max(max, worldPoint);
            }

            // 分别对每个分量进行 floor 和 ceil 操作
            return (
                new int3(
                    (int)math.floor(min.x),
                    (int)math.floor(min.y),
                    (int)math.floor(min.z)
                ),
                new int3(
                    (int)math.ceil(max.x),
                    (int)math.ceil(max.y),
                    (int)math.ceil(max.z)
                )
            );
        }

        [BurstCompile]
        private bool IsIntersecting(int3 cellPos, SpaceshipColliderBuffer collider)
        {
            var cellCenter = new float3(cellPos) + 0.5f;
            var localCenter = math.mul(math.inverse(collider.Rotation), cellCenter - collider.Center);
            var halfSize = collider.Size * 0.5f + 0.5f; // 加上单元格半径

            return math.all(math.abs(localCenter) <= halfSize);
        }

        [BurstCompile]
        private void CreateVoxelEntity(int3 cellPos, EntityCommandBuffer ecb)
        {
            var entity = ecb.CreateEntity();
            ecb.AddComponent<SpaceshipTempCellTag>(entity);
            ecb.AddComponent(entity, LocalTransform.FromPosition(new float3(cellPos) + 0.5f));
        }

        #endregion
    }
}