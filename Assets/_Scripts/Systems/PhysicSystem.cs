using _Scripts.Components;
using _Scripts.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts.Systems
{
    [UpdateInGroup(typeof(CaFastSystemGroup))]
    public partial struct PhysicSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;

        public void OnUpdate(ref SystemState state)
        {
            // 获取全局数据容器引用
            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            var deltaTime = SystemAPI.Time.DeltaTime;
            var maxStep = math.max(1,
                (int)math.floor(GlobalConfig.MaxSpeed * deltaTime * GlobalConfig.PhysicsSpeedScale));

            var step = maxStep;
            while (step > 0)
            {
                // 1. 移动与碰撞
                state.Dependency = new TryMoveCellJob
                {
                    CellMap = _cellMap,
                    MassLookup = SystemAPI.GetComponentLookup<Mass>(true),
                    CellStateLookup = SystemAPI.GetComponentLookup<CellState>(true),
                    VelocityLookup = SystemAPI.GetComponentLookup<Velocity>(),
                    ImpulseBufferLookup = SystemAPI.GetBufferLookup<ImpulseBuffer>()
                }.Schedule(state.Dependency);
                state.Dependency.Complete();

                // 2. 冲量整合与速度更新
                state.Dependency = new VelocityUpdateJob
                {
                    MassLookup = SystemAPI.GetComponentLookup<Mass>(true),
                    VelocityLookup = SystemAPI.GetComponentLookup<Velocity>(),
                    ImpulseBufferLookup = SystemAPI.GetBufferLookup<ImpulseBuffer>()
                }.Schedule(state.Dependency);
                state.Dependency.Complete();

                // 3. 检查是否还有可移动 Cell
                if (SystemAPI.QueryBuilder().WithAll<IsAlive, Velocity>().Build().CalculateEntityCount() == 0) break;

                // 4. 更新计数
                step--;
            }
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive), typeof(Velocity))]
        private partial struct TryMoveCellJob : IJobEntity
        {
            public NativeHashMap<int3, Entity> CellMap;
            [ReadOnly] public ComponentLookup<Mass> MassLookup;
            [ReadOnly] public ComponentLookup<CellState> CellStateLookup;
            public ComponentLookup<Velocity> VelocityLookup;
            public BufferLookup<ImpulseBuffer> ImpulseBufferLookup;

            private void Execute(Entity self, ref LocalTransform localTransform)
            {
                var velocity = VelocityLookup[self].Value;
                var speed = math.length(velocity);
                var cellState = CellStateLookup[self].Value;
                var normalizedVelocity = math.normalize(velocity);
                var offset = (int3)math.round(normalizedVelocity);
                var currentPos = (int3)localTransform.Position;

                // 1. 所有Cell都先尝试主目标
                var primaryTarget = currentPos + offset;
                if (CellUtility.TryMoveCell(self, ref localTransform, CellMap, primaryTarget)) return;

                // 2. 确定"底面"方向（最接近速度方向的轴）
                var bottomDirection = GetPrimaryDirection(normalizedVelocity);
                var bottomCenter = currentPos + bottomDirection;

                // 3. Solid尝试底面中心
                if (cellState >= CellStateEnum.Solid && !bottomCenter.Equals(primaryTarget))
                {
                    if (CellUtility.TryMoveCell(self, ref localTransform, CellMap, bottomCenter))
                    {
                        // 应用速度损耗
                        VelocityLookup[self] = new Velocity { Value = velocity * GlobalConfig.BottomCenterDamping };
                        return;
                    }
                }

                // 4. Powder尝试底层位置（需要满足速度门槛）
                if (cellState >= CellStateEnum.Powder && speed > GlobalConfig.PowderSideMovementThreshold)
                {
                    var bottomLayer = GetBottomLayerPositions(currentPos, bottomDirection, normalizedVelocity);
                    var maxAttempts = math.min(bottomLayer.Length, GlobalConfig.MaxPowderAttempts);
                    
                    for (int i = 0; i < maxAttempts; i++)
                    {
                        if (CellUtility.TryMoveCell(self, ref localTransform, CellMap, bottomLayer[i]))
                        {
                            // 应用更大的速度损耗
                            VelocityLookup[self] = new Velocity { Value = velocity * GlobalConfig.BottomLayerDamping };
                            bottomLayer.Dispose();
                            return;
                        }
                    }
                    bottomLayer.Dispose();
                }

                // 5. Liquid尝试底层和中层位置
                if (cellState >= CellStateEnum.Liquid)
                {
                    // 5.1 先尝试底层
                    var bottomLayer = GetBottomLayerPositions(currentPos, bottomDirection, normalizedVelocity);
                    var maxBottomAttempts = math.min(bottomLayer.Length, GlobalConfig.MaxLiquidBottomAttempts);
                    
                    for (int i = 0; i < maxBottomAttempts; i++)
                    {
                        if (CellUtility.TryMoveCell(self, ref localTransform, CellMap, bottomLayer[i]))
                        {
                            VelocityLookup[self] = new Velocity { Value = velocity * GlobalConfig.BottomLayerDamping };
                            bottomLayer.Dispose();
                            return;
                        }
                    }
                    bottomLayer.Dispose();
                    
                    // 5.2 高速时尝试中层
                    if (speed > GlobalConfig.LiquidMiddleLayerThreshold)
                    {
                        var middleLayer = GetMiddleLayerPositions(currentPos, bottomDirection, normalizedVelocity);
                        var maxMiddleAttempts = math.min(middleLayer.Length, GlobalConfig.MaxLiquidMiddleAttempts);
                        
                        for (int i = 0; i < maxMiddleAttempts; i++)
                        {
                            if (CellUtility.TryMoveCell(self, ref localTransform, CellMap, middleLayer[i]))
                            {
                                VelocityLookup[self] = new Velocity { Value = velocity * GlobalConfig.MiddleLayerDamping };
                                middleLayer.Dispose();
                                return;
                            }
                        }
                        middleLayer.Dispose();
                    }
                }

                // 6. 处理碰撞
                HandleCollision(self, primaryTarget, currentPos);
            }

            private static int3 GetPrimaryDirection(float3 normalizedVelocity)
            {
                var abs = math.abs(normalizedVelocity);
                if (abs.x >= abs.y && abs.x >= abs.z) return new int3((int)math.sign(normalizedVelocity.x), 0, 0);
                if (abs.y >= abs.x && abs.y >= abs.z) return new int3(0, (int)math.sign(normalizedVelocity.y), 0);
                return new int3(0, 0, (int)math.sign(normalizedVelocity.z));
            }

            private NativeArray<int3> GetBottomLayerPositions(int3 current, int3 bottomDirection, float3 normalizedVelocity)
            {
                var positions = new NativeArray<int3>(8, Allocator.Temp);
                var index = 0;

                // 根据底面方向确定平面上的8个位置
                for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue; // 跳过中心

                    int3 pos;
                    if (bottomDirection.x != 0) // X轴为主方向
                        pos = current + new int3(bottomDirection.x, i, j);
                    else if (bottomDirection.y != 0) // Y轴为主方向
                        pos = current + new int3(i, bottomDirection.y, j);
                    else // Z轴为主方向
                        pos = current + new int3(i, j, bottomDirection.z);

                    positions[index++] = pos;
                }

                // 简单排序：根据与速度方向的相似度
                SortPositionsByDirection(positions, current, normalizedVelocity);

                return positions;
            }

            private NativeArray<int3> GetMiddleLayerPositions(int3 current, int3 bottomDirection, float3 normalizedVelocity)
            {
                var positions = new NativeArray<int3>(8, Allocator.Temp);
                var index = 0;

                // 获取与底面垂直的平面上的8个位置
                for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;

                    int3 pos;
                    if (bottomDirection.x != 0) // 底面在X方向
                        pos = current + new int3(0, i, j);
                    else if (bottomDirection.y != 0) // 底面在Y方向
                        pos = current + new int3(i, 0, j);
                    else // 底面在Z方向
                        pos = current + new int3(i, j, 0);

                    positions[index++] = pos;
                }

                // 排序
                SortPositionsByDirection(positions, current, normalizedVelocity);

                return positions;
            }
            
            // 根据与速度方向的相似度对位置进行排序
            private void SortPositionsByDirection(NativeArray<int3> positions, int3 current, float3 velocity)
            {
                // 使用简单的冒泡排序（对于8个元素足够高效）
                for (int i = 0; i < positions.Length - 1; i++)
                {
                    for (int j = 0; j < positions.Length - i - 1; j++)
                    {
                        var dir1 = math.normalize(positions[j] - current);
                        var dir2 = math.normalize(positions[j + 1] - current);
                        var dot1 = math.dot(dir1, velocity);
                        var dot2 = math.dot(dir2, velocity);
                        
                        if (dot1 < dot2)
                        {
                            (positions[j], positions[j + 1]) = (positions[j + 1], positions[j]);
                        }
                    }
                }
            }

            // 处理碰撞
            private void HandleCollision(Entity self, int3 targetCoordinate, int3 currentPos)
            {
                if (!CellMap.TryGetValue(targetCoordinate, out Entity targetEntity)) return;

                var currentVelocity = VelocityLookup[self].Value;
                var currentMass = MassLookup[self].Value;
                var targetVelocity = VelocityLookup[targetEntity].Value;
                var targetMass = MassLookup[targetEntity].Value;

                var collisionNormal = math.normalize(targetCoordinate - currentPos);
                var relativeSpeed = math.dot(currentVelocity - targetVelocity, collisionNormal);
                var impulseMagnitude = (2 * relativeSpeed) / (currentMass + targetMass);

                // 应用冲量损失
                var adjustedImpulseMagnitude = impulseMagnitude * GlobalConfig.ImpulseLossFactor;
                var currentImpulse = -adjustedImpulseMagnitude * targetMass * collisionNormal;
                var targetImpulse = adjustedImpulseMagnitude * currentMass * collisionNormal;

                ImpulseBufferLookup[self].Add(new ImpulseBuffer { Value = currentImpulse });
                ImpulseBufferLookup[targetEntity].Add(new ImpulseBuffer { Value = targetImpulse });
            }
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive))]
        private partial struct VelocityUpdateJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<Mass> MassLookup;
            public ComponentLookup<Velocity> VelocityLookup;
            public BufferLookup<ImpulseBuffer> ImpulseBufferLookup;

            private void Execute(Entity cell)
            {
                // 计算总冲量
                var totalImpulse = float3.zero;
                foreach (var impulse in ImpulseBufferLookup[cell]) totalImpulse += impulse.Value;

                // 计算新速度
                var newVelocity = VelocityLookup[cell].Value + totalImpulse / MassLookup[cell].Value;

                // 限制最大速度
                var speedSq = math.lengthsq(newVelocity);
                if (speedSq > GlobalConfig.MaxSpeed * GlobalConfig.MaxSpeed)
                    newVelocity = math.normalize(newVelocity) * GlobalConfig.MaxSpeed;

                // 更新速度
                VelocityLookup[cell] = new Velocity { Value = newVelocity };

                // 根据速度模长，启用/禁用 Velocity 组件
                VelocityLookup.SetComponentEnabled(cell, math.lengthsq(newVelocity) >= 1f);

                // 清空冲量缓冲区
                ImpulseBufferLookup[cell].Clear();
            }
        }
    }
}