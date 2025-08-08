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
        private int _spaceshipMassValue;
        private float3 _spaceshipVelocityValue;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _tempCellQuery = SystemAPI.QueryBuilder().WithAll<SpaceshipTempCellTag>().Build();
            state.RequireForUpdate<SpaceshipProxyTag>();
            state.RequireForUpdate<SpaceshipMass>();
            state.RequireForUpdate<SpaceshipVelocity>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            // 清理 TempCell
            foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<SpaceshipTempCellTag>())
                _cellMap.Remove((int3)math.floor(transform.ValueRO.Position));
            state.EntityManager.DestroyEntity(_tempCellQuery);

            // 结构性变更后必须重新获取组件
            _colliderBuffer = SystemAPI.GetSingletonBuffer<SpaceshipColliderBuffer>();
            _spaceshipMassValue = SystemAPI.GetSingleton<SpaceshipMass>().Value;
            _spaceshipVelocityValue = SystemAPI.GetSingleton<SpaceshipVelocity>().Value;

            // 使用 Entity Command Buffer 进行批量创建
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var pendingCells = new NativeHashSet<int3>(64, Allocator.Temp); // 跟踪即将创建 TempCell 的位置
            
            foreach (var collider in _colliderBuffer)
                CreateVoxelsForCollider(collider, ecb, pendingCells);
            
            ecb.Playback(state.EntityManager);
            pendingCells.Dispose();

            // 更新 CellMap
            foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<SpaceshipTempCellTag>().WithEntityAccess())
                _cellMap.TryAdd((int3)math.floor(transform.ValueRO.Position), entity);
        }

        #region CreateVoxelsForCollider

        [BurstCompile]
        private void CreateVoxelsForCollider(SpaceshipColliderBuffer collider, EntityCommandBuffer ecb, NativeHashSet<int3> pendingCells)
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
                if (_cellMap.ContainsKey(cellPos) || pendingCells.Contains(cellPos)) continue;
                if (!IsIntersecting(cellPos, collider)) continue;
                CreateVoxelEntity(cellPos, ecb);
                pendingCells.Add(cellPos);
            }
        }

        [BurstCompile]
        private (int3 min, int3 max) GetRotatedBounds(SpaceshipColliderBuffer collider)
        {
            var halfSize = collider.Size * 0.5f;
            var minFloat = new float3(float.MaxValue);
            var maxFloat = new float3(float.MinValue);

            // 计算角点并找出边界
            for (var i = 0; i < 8; i++)
            {
                var corner = new float3(
                    (i & 1) == 0 ? -halfSize.x : halfSize.x,
                    (i & 2) == 0 ? -halfSize.y : halfSize.y,
                    (i & 4) == 0 ? -halfSize.z : halfSize.z
                );

                var worldPoint = math.mul(collider.Rotation, corner) + collider.Center;
                minFloat = math.min(minFloat, worldPoint);
                maxFloat = math.max(maxFloat, worldPoint);
            }

            return ((int3)math.floor(minFloat), (int3)math.ceil(maxFloat));
        }

        [BurstCompile]
        private bool IsIntersecting(int3 cellPos, SpaceshipColliderBuffer collider)
        {
            var cellCenter = new float3(cellPos) + new float3(0.5f, 0.5f, 0.5f);

            // 将网格单元中心转换到碰撞体本地空间
            var localCenter = math.mul(math.inverse(collider.Rotation), cellCenter - collider.Center);
            var halfColliderSize = collider.Size * 0.5f;
            var halfCellSize = new float3(0.5f);

            // 在本地空间检查 AABB 相交
            return math.all(math.abs(localCenter) <= halfColliderSize + halfCellSize);
        }

        [BurstCompile]
        private Entity CreateVoxelEntity(int3 cellPos, EntityCommandBuffer ecb)
        {
            var entity = ecb.CreateEntity();

            // 添加基础组件
            ecb.AddComponent<SpaceshipTempCellTag>(entity);
            ecb.AddComponent(entity, LocalTransform.FromPosition(new float3(cellPos) + new float3(0.5f, 0.5f, 0.5f)));

            // 添加物理系统需要的组件
            ecb.AddComponent(entity, new Mass { Value = _spaceshipMassValue });
            ecb.AddComponent(entity, new Velocity { Value = _spaceshipVelocityValue });
            ecb.AddBuffer<ImpulseBuffer>(entity);

            return entity;
        }

        #endregion
    }
}