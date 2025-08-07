using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Systems
{
    // 自定义AABB结构
    public struct AABB
    {
        public float3 Min;
        public float3 Max;
    }

    [UpdateInGroup(typeof(CaFastSystemGroup), OrderFirst = true)]
    public partial struct SpaceshipVoxelizationSystem : ISystem
    {
        private EntityQuery _tempCellQuery;
        private Entity _spaceshipProxyEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _tempCellQuery = SystemAPI.QueryBuilder().WithAll<SpaceshipTempCellTag>().Build();
            state.RequireForUpdate<SpaceshipProxyTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 销毁所有具有 SpaceshipTempCellTag 的实体
            state.EntityManager.DestroyEntity(_tempCellQuery);

            _spaceshipProxyEntity = SystemAPI.GetSingletonEntity<SpaceshipProxyTag>();

            // 先获取碰撞体数据到本地数组，避免结构性变更后的访问问题
            var colliderBuffer = SystemAPI.GetBuffer<SpaceshipColliderBuffer>(_spaceshipProxyEntity);
            var colliderArray = colliderBuffer.ToNativeArray(Allocator.Temp);

            // 遍历每个碰撞体，创建包裹它的临时单元格
            foreach (var collider in colliderArray)
            {
                CreateSurfaceCellsForCollider(ref state, collider);
            }

            colliderArray.Dispose();
        }

        [BurstCompile]
        private void CreateSurfaceCellsForCollider(ref SystemState state, SpaceshipColliderBuffer collider)
        {
            // 计算旋转后碰撞体的轴对齐包围盒(AABB)
            var aabb = CalculateRotatedAABB(collider);

            // 计算需要创建的单元格范围（基于int3坐标）
            var minBounds = new int3(
                (int)math.floor(aabb.Min.x),
                (int)math.floor(aabb.Min.y),
                (int)math.floor(aabb.Min.z)
            );

            var maxBounds = new int3(
                (int)math.ceil(aabb.Max.x),
                (int)math.ceil(aabb.Max.y),
                (int)math.ceil(aabb.Max.z)
            );

            // 创建完全覆盖碰撞体的单元格（实心填充）
            for (var x = minBounds.x; x <= maxBounds.x; x++)
            for (var y = minBounds.y; y <= maxBounds.y; y++)
            for (var z = minBounds.z; z <= maxBounds.z; z++)
            {
                var cellPosition = new int3(x, y, z);

                // 检查单元格是否与旋转后的碰撞体相交
                if (IsCellIntersectingRotatedCollider(cellPosition, collider))
                {
                    CreateTempCellEntity(ref state, cellPosition);
                }
            }
        }

        [BurstCompile]
        private AABB CalculateRotatedAABB(SpaceshipColliderBuffer collider)
        {
            // 获取碰撞体的8个角点（本地空间）
            var halfSize = collider.Size * 0.5f;

            // 直接计算8个角点，避免创建托管数组
            var minPoint = new float3(float.MaxValue);
            var maxPoint = new float3(float.MinValue);

            // 手动展开8个角点的计算，避免数组分配
            var corner0 = new float3(-halfSize.x, -halfSize.y, -halfSize.z);
            var corner1 = new float3(+halfSize.x, -halfSize.y, -halfSize.z);
            var corner2 = new float3(-halfSize.x, +halfSize.y, -halfSize.z);
            var corner3 = new float3(+halfSize.x, +halfSize.y, -halfSize.z);
            var corner4 = new float3(-halfSize.x, -halfSize.y, +halfSize.z);
            var corner5 = new float3(+halfSize.x, -halfSize.y, +halfSize.z);
            var corner6 = new float3(-halfSize.x, +halfSize.y, +halfSize.z);
            var corner7 = new float3(+halfSize.x, +halfSize.y, +halfSize.z);

            // 转换每个角点到世界空间并更新边界
            var worldPoint0 = math.mul(collider.Rotation, corner0) + collider.Center;
            minPoint = math.min(minPoint, worldPoint0);
            maxPoint = math.max(maxPoint, worldPoint0);

            var worldPoint1 = math.mul(collider.Rotation, corner1) + collider.Center;
            minPoint = math.min(minPoint, worldPoint1);
            maxPoint = math.max(maxPoint, worldPoint1);

            var worldPoint2 = math.mul(collider.Rotation, corner2) + collider.Center;
            minPoint = math.min(minPoint, worldPoint2);
            maxPoint = math.max(maxPoint, worldPoint2);

            var worldPoint3 = math.mul(collider.Rotation, corner3) + collider.Center;
            minPoint = math.min(minPoint, worldPoint3);
            maxPoint = math.max(maxPoint, worldPoint3);

            var worldPoint4 = math.mul(collider.Rotation, corner4) + collider.Center;
            minPoint = math.min(minPoint, worldPoint4);
            maxPoint = math.max(maxPoint, worldPoint4);

            var worldPoint5 = math.mul(collider.Rotation, corner5) + collider.Center;
            minPoint = math.min(minPoint, worldPoint5);
            maxPoint = math.max(maxPoint, worldPoint5);

            var worldPoint6 = math.mul(collider.Rotation, corner6) + collider.Center;
            minPoint = math.min(minPoint, worldPoint6);
            maxPoint = math.max(maxPoint, worldPoint6);

            var worldPoint7 = math.mul(collider.Rotation, corner7) + collider.Center;
            minPoint = math.min(minPoint, worldPoint7);
            maxPoint = math.max(maxPoint, worldPoint7);

            return new AABB { Min = minPoint, Max = maxPoint };
        }

        [BurstCompile]
        private bool IsCellIntersectingRotatedCollider(int3 cellPosition, SpaceshipColliderBuffer collider)
        {
            // 单元格的世界坐标（大小为1的立方体）
            var cellCenter = new float3(cellPosition) + 0.5f;

            // 将单元格中心转换到碰撞体的本地空间
            var localCellCenter = math.mul(math.inverse(collider.Rotation), cellCenter - collider.Center);

            // 在本地空间中进行AABB检测
            var halfSize = collider.Size * 0.5f;
            var halfCellSize = new float3(0.5f); // 单元格半径

            // 检查本地空间中的相交
            return math.all(math.abs(localCellCenter) <= halfSize + halfCellSize);
        }

        [BurstCompile]
        private void CreateTempCellEntity(ref SystemState state, int3 cellPosition)
        {
            // 创建新的临时单元格实体
            var tempCellEntity = state.EntityManager.CreateEntity();

            // 添加临时单元格标签
            state.EntityManager.AddComponent<SpaceshipTempCellTag>(tempCellEntity);

            // 添加位置组件
            state.EntityManager.AddComponent<LocalTransform>(tempCellEntity);
            state.EntityManager.SetComponentData(tempCellEntity,
                LocalTransform.FromPosition(new float3(cellPosition) + 0.5f));
        }
    }
}