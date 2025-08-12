using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaFastSystemGroup))]
    [UpdateAfter(typeof(SpaceshipVoxelizationSystem))]
    public partial struct SpaceshipCollisionPredictionSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpaceshipProxyTag>();
            state.RequireForUpdate<SpaceshipMass>();
            state.RequireForUpdate<SpaceshipVelocity>();
            state.RequireForUpdate<SpaceshipForceFeedback>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // 获取全局数据
            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            // 获取飞船数据
            var colliderBuffer = SystemAPI.GetSingletonBuffer<SpaceshipColliderBuffer>();
            var spaceshipMass = SystemAPI.GetSingleton<SpaceshipMass>().Value;
            var spaceshipVelocity = SystemAPI.GetSingleton<SpaceshipVelocity>().Value;

            // 计算预测位移
            const float deltaTime = GlobalConfig.FastUpdateRateInMS / 1000f;
            var totalDisplacement = spaceshipVelocity * deltaTime;
            var remainingDistance = math.length(totalDisplacement);

            if (remainingDistance < 0.001f)
                return;

            const float stepSize = 0.1f;
            var collisionFound = false;
            var direction = math.normalize(totalDisplacement);
            var currentPosition = float3.zero; // 相对于起始位置的偏移
            var forceFeedback = float3.zero;

            // 逐步检测路径上的碰撞
            while (remainingDistance > 0 && !collisionFound)
            {
                var currentStep = math.min(stepSize, remainingDistance);
                currentPosition += direction * currentStep;
                remainingDistance -= currentStep;

                // 检测当前步骤位置的碰撞
                var collisionCells = new NativeHashSet<int3>(8, Allocator.Temp);

                foreach (var collider in colliderBuffer)
                {
                    var testCenter = collider.Center + currentPosition;
                    CheckColliderAtPosition(testCenter, collider.Size, collider.Rotation, collisionCells);
                }

                // 如果发现碰撞，计算反作用力
                if (collisionCells.Count <= 0) continue;

                collisionFound = true;
                foreach (var cellPos in collisionCells)
                {
                    if (!_cellMap.TryGetValue(cellPos, out var cellEntity) ||
                        !SystemAPI.HasComponent<CellTag>(cellEntity)) continue;

                    // 获取目标 Cell 的物理属性
                    var cellMass = SystemAPI.GetComponent<Mass>(cellEntity).Value;
                    var cellVelocity = SystemAPI.GetComponent<Velocity>(cellEntity).Value;

                    // 计算碰撞点
                    var cellCenter = new float3(cellPos) + new float3(0.5f);
                    var spaceshipCenter = colliderBuffer[0].Center + currentPosition;

                    // 计算碰撞法线方向
                    var collisionNormal = math.normalize(cellCenter - spaceshipCenter);

                    // 计算相对速度在法线方向上的分量
                    var relativeVelocity = spaceshipVelocity - cellVelocity;
                    var relativeSpeed = math.dot(relativeVelocity, collisionNormal);

                    // 使用与 PhysicSystem 一致的冲量计算公式
                    var impulseMagnitude = (2 * relativeSpeed) / (spaceshipMass + cellMass);

                    // 放大冲量以增强反馈效果
                    var adjustedImpulseMagnitude = impulseMagnitude * GlobalConfig.ImpulseLossFactor;

                    // 计算并手动放大双方的冲量
                    var spaceshipImpulse = -adjustedImpulseMagnitude * cellMass * collisionNormal *
                                           GlobalConfig.SpaceshipImpulseUpscaleFactor;
                    var cellImpulse = adjustedImpulseMagnitude * spaceshipMass * collisionNormal *
                                      GlobalConfig.CellImpulseUpscaleFactor;

                    // 给目标单元格施加冲量
                    var impulseBuffer = SystemAPI.GetBuffer<ImpulseBuffer>(cellEntity);
                    impulseBuffer.Add(new ImpulseBuffer { Value = cellImpulse });

                    // 飞船受到反作用力
                    forceFeedback += spaceshipImpulse;
                }
            }

            // 应用力反馈
            if (!math.any(forceFeedback != float3.zero)) return;
            var currentFeedback = SystemAPI.GetSingleton<SpaceshipForceFeedback>();
            currentFeedback.Value += forceFeedback;
            SystemAPI.SetSingleton(currentFeedback);
        }

        [BurstCompile]
        private void CheckColliderAtPosition(float3 center, float3 size, quaternion rotation,
            NativeHashSet<int3> collisionCells)
        {
            var halfSize = size * 0.5f;
            var min = (int3)math.floor(center - halfSize);
            var max = (int3)math.ceil(center + halfSize);

            for (var x = min.x; x <= max.x; x++)
            for (var y = min.y; y <= max.y; y++)
            for (var z = min.z; z <= max.z; z++)
            {
                var cellPos = new int3(x, y, z);

                if (!_cellMap.ContainsKey(cellPos)) continue;

                // 网格单元的实际几何中心
                var cellActualCenter = new float3(cellPos) + new float3(0.5f);
                var localCenter = math.mul(math.inverse(rotation), cellActualCenter - center);

                if (math.all(math.abs(localCenter) <= halfSize + new float3(0.5f)))
                    collisionCells.Add(cellPos);
            }
        }
    }
}