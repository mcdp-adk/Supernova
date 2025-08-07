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
    [UpdateBefore(typeof(SpaceshipFinalizationUpdateSystem))]
    public partial struct PhysicSystem : ISystem
    {
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeArray<CellConfig> _cellConfigs;
        private EntityQuery _cellQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _cellQuery = SystemAPI.QueryBuilder().WithAll<IsAlive, Velocity>().Build();
            state.RequireForUpdate<CellConfigTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!_cellMap.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellMap = globalDataSystem.CellMap;
            }

            if (!_cellConfigs.IsCreated)
            {
                var globalDataSystem = state.World.GetExistingSystemManaged<GlobalDataInitSystem>();
                _cellConfigs = globalDataSystem.CellConfigs;
            }

            var deltaTime = SystemAPI.Time.DeltaTime;
            var maxStep = math.max(1,
                (int)math.floor(GlobalConfig.MaxSpeed * deltaTime * GlobalConfig.PhysicsSpeedScale));

            var step = maxStep;
            while (step > 0)
            {
                // 1. 冲量整合与速度更新
                state.Dependency = new VelocityUpdateJob
                {
                    MaxStep = maxStep
                }.ScheduleParallel(state.Dependency);
                state.Dependency.Complete();

                // 2. 检查是否还有可移动 Cell
                if (_cellQuery.CalculateEntityCount() == 0) break;

                // 3. 移动与碰撞
                state.Dependency = new TryMoveCellJob
                {
                    CellMap = _cellMap,
                    CellConfigs = _cellConfigs,
                    CellStateLookup = SystemAPI.GetComponentLookup<CellState>(true),
                    MassLookup = SystemAPI.GetComponentLookup<Mass>(true),
                    LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                    VelocityLookup = SystemAPI.GetComponentLookup<Velocity>(),
                    ImpulseBufferLookup = SystemAPI.GetBufferLookup<ImpulseBuffer>(),
                }.Schedule(state.Dependency);
                state.Dependency.Complete();

                // 4. 更新计数
                step--;
            }
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive), typeof(Velocity))]
        private partial struct TryMoveCellJob : IJobEntity
        {
            public NativeHashMap<int3, Entity> CellMap;
            [ReadOnly] public NativeArray<CellConfig> CellConfigs;
            [ReadOnly] public ComponentLookup<CellState> CellStateLookup;
            [ReadOnly] public ComponentLookup<Mass> MassLookup;
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            public ComponentLookup<Velocity> VelocityLookup;
            public BufferLookup<ImpulseBuffer> ImpulseBufferLookup;

            private void Execute(Entity self, in CellType cellType)
            {
                var cellState = CellStateLookup[self];
                var movementDebt = VelocityLookup[self].MovementDebt;
                var direction = math.normalize(movementDebt);
                var offset = (int3)math.round(direction);
                var currentCoordinate = (int3)LocalTransformLookup[self].Position;

                // 1. 尝试移动到目标位置
                var targetCoordinate = currentCoordinate + offset;
                if (TryMoveCell(self, targetCoordinate)) return;

                // 2. 获取最接近速度方向的轴为主方向
                var primaryDirection = GetPrimaryDirection(direction);

                // 3. 尝试主方向移动，失败则尝试下沉交换
                var primaryTargetCoordinate = currentCoordinate + primaryDirection;
                if (TryMoveCell(self, primaryTargetCoordinate)) return;
                if (TrySettlementSwap(self, primaryTargetCoordinate)) return;

                // 4. 获取 Cell 配置以获得 Fluidity
                var cellConfig = CellConfigs.GetCellConfig(cellType.Value);
                var fluidity = cellConfig.Fluidity;

                // 5. 获取并根据 Fluidity 控制遍历的可用坐标
                var coordinates =
                    GetAvailableCoordinates(currentCoordinate, primaryDirection, direction, cellState.Value);
                var tryRatio = fluidity * fluidity; // 平方关系，低端更陡峭
                var maxTries = math.max(1, (int)(coordinates.Length * tryRatio));

                for (var i = 0; i < maxTries; i++)
                {
                    if (!TryMoveCell(self, coordinates[i])) continue;

                    // 移动成功后，应用 Viscosity 影响
                    var movementEfficiency = 1.0f - cellConfig.Viscosity;
                    var currentVelocity = VelocityLookup[self];
                    var newVelocity = new Velocity
                    {
                        Value = currentVelocity.Value * movementEfficiency,
                        MovementDebt = currentVelocity.MovementDebt
                    };
                    VelocityLookup[self] = newVelocity;
                    return;
                }

                // 6. 处理碰撞
                HandleCollision(self, targetCoordinate, currentCoordinate);
            }

            #region 辅助方法

            private bool TryMoveCell(Entity cell, int3 targetCoordinate)
            {
                if (!CellMap.TryAdd(targetCoordinate, cell)) return false;

                var localTransform = LocalTransformLookup[cell];
                var actualMovement = targetCoordinate - (int3)localTransform.Position;
                CellMap.Remove((int3)localTransform.Position);
                localTransform.Position = targetCoordinate;
                LocalTransformLookup[cell] = localTransform;

                // 减少移动债务
                ReduceMovementDebt(cell, actualMovement);
                return true;
            }

            private bool TrySwapCell(Entity currentCell, Entity targetCell)
            {
                var currentTransform = LocalTransformLookup[currentCell];
                var targetTransform = LocalTransformLookup[targetCell];

                var currentCoordinate = (int3)currentTransform.Position;
                var targetCoordinate = (int3)targetTransform.Position;
                var actualMovement = targetCoordinate - currentCoordinate;

                // 移除旧映射
                CellMap.Remove(currentCoordinate);
                CellMap.Remove(targetCoordinate);

                // 添加新映射
                CellMap.TryAdd(targetCoordinate, currentCell);
                CellMap.TryAdd(currentCoordinate, targetCell);

                // 交换位置
                currentTransform.Position = targetCoordinate;
                targetTransform.Position = currentCoordinate;

                LocalTransformLookup[currentCell] = currentTransform;
                LocalTransformLookup[targetCell] = targetTransform;

                // 减少移动债务
                ReduceMovementDebt(currentCell, actualMovement);
                return true;
            }

            private void ReduceMovementDebt(Entity cell, int3 actualMovement)
            {
                var velocity = VelocityLookup[cell];
                velocity.MovementDebt -= actualMovement;
                VelocityLookup[cell] = velocity;
            }

            private bool TrySettlementSwap(Entity self, int3 targetCoordinate)
            {
                // 检查目标位置是否有细胞
                if (!CellMap.TryGetValue(targetCoordinate, out Entity targetEntity)) return false;

                // 检查目标细胞是否为液体状态
                var targetState = CellStateLookup[targetEntity];
                if (targetState.Value != CellStateEnum.Liquid) return false;

                // 检查当前细胞质量是否大于目标细胞（沉降条件）
                var currentMass = MassLookup[self].Value;
                var targetMass = MassLookup[targetEntity].Value;

                // 如果当前细胞质量大于目标细胞，则尝试交换位置
                return currentMass > targetMass && TrySwapCell(self, targetEntity);
            }

            private static int3 GetPrimaryDirection(float3 normalizedVelocity)
            {
                var abs = math.abs(normalizedVelocity);
                if (abs.x >= abs.y && abs.x >= abs.z) return new int3((int)math.sign(normalizedVelocity.x), 0, 0);
                if (abs.y >= abs.x && abs.y >= abs.z) return new int3(0, (int)math.sign(normalizedVelocity.y), 0);
                return new int3(0, 0, (int)math.sign(normalizedVelocity.z));
            }

            private static NativeArray<int3> GetAvailableCoordinates(int3 currentCoordinate,
                int3 primaryDirection, float3 direction, CellStateEnum cellState)
            {
                var maxCoordinates = cellState switch
                {
                    CellStateEnum.Solid => 1,
                    CellStateEnum.Powder => 9,
                    CellStateEnum.Liquid => 17,
                    _ => 1
                };

                var coordinates = new NativeArray<int3>(maxCoordinates, Allocator.Temp);
                var index = 0;

                // 1. 主方向中心块
                if (cellState >= CellStateEnum.Solid)
                {
                    coordinates[index++] = currentCoordinate + primaryDirection;
                }

                // 2. 底层坐标
                if (cellState >= CellStateEnum.Powder)
                {
                    for (var i = -1; i <= 1; i++)
                    for (var j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0) continue; // 跳过中心

                        var pos = currentCoordinate;
                        // X 轴为主方向
                        if (primaryDirection.x != 0) pos += new int3(primaryDirection.x, i, j);
                        // Y 轴为主方向
                        else if (primaryDirection.y != 0) pos += new int3(i, primaryDirection.y, j);
                        // Z 轴为主方向
                        else pos += new int3(i, j, primaryDirection.z);

                        coordinates[index++] = pos;
                    }
                }

                // 3. 中层坐标
                if (cellState >= CellStateEnum.Liquid)
                {
                    for (var i = -1; i <= 1; i++)
                    for (var j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0) continue; // 跳过中心

                        var pos = currentCoordinate;
                        // X 轴为主方向
                        if (primaryDirection.x != 0) pos += new int3(0, i, j);
                        // Y 轴为主方向
                        else if (primaryDirection.y != 0) pos += new int3(i, 0, j);
                        // Z 轴为主方向
                        else pos += new int3(i, j, 0);

                        coordinates[index++] = pos;
                    }
                }

                SortPositionsByDirection(coordinates, currentCoordinate, direction);
                return coordinates;
            }

            private static void SortPositionsByDirection(NativeArray<int3> coordinates,
                int3 currentCoordinate, float3 direction)
            {
                // 插入排序
                for (var i = 1; i < coordinates.Length; i++)
                {
                    var key = coordinates[i];
                    var keyDot = math.dot(math.normalize(key - currentCoordinate), direction);
                    var j = i - 1;

                    while (j >= 0)
                    {
                        var currentDot = math.dot(math.normalize(coordinates[j] - currentCoordinate), direction);
                        if (currentDot >= keyDot) break;
                        coordinates[j + 1] = coordinates[j];
                        j--;
                    }

                    coordinates[j + 1] = key;
                }
            }

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

            #endregion
        }

        [BurstCompile]
        [WithAll(typeof(IsAlive))]
        [WithPresent(typeof(Velocity))]
        private partial struct VelocityUpdateJob : IJobEntity
        {
            [ReadOnly] public int MaxStep;

            private void Execute(in Mass mass, ref Velocity velocity, EnabledRefRW<Velocity> velocityEnabled,
                DynamicBuffer<ImpulseBuffer> impulseBuffer)
            {
                // 计算总冲量
                var totalImpulse = float3.zero;
                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var impulse in impulseBuffer) totalImpulse += impulse.Value;

                // 计算并限制新速度
                var newVelocity = velocity.Value + totalImpulse / mass.Value;
                var speedSq = math.lengthsq(newVelocity);
                if (speedSq > GlobalConfig.MaxSpeed * GlobalConfig.MaxSpeed)
                    newVelocity = math.normalize(newVelocity) * GlobalConfig.MaxSpeed;

                // 计算并限制 MovementDebt
                var newDebt = velocity.MovementDebt +
                              newVelocity * ((GlobalConfig.FastUpdateRateInMS / 1000.0f) / MaxStep);
                var debtSq = math.lengthsq(newDebt);
                if (debtSq > GlobalConfig.MaxSpeed * GlobalConfig.MaxSpeed)
                    newDebt = math.normalize(newDebt) * GlobalConfig.MaxSpeed;

                // 更新 Velocity 组件
                velocity = new Velocity { Value = newVelocity, MovementDebt = newDebt };

                // 根据 MovementDebt 模长，启用/禁用 Velocity 组件
                velocityEnabled.ValueRW = math.lengthsq(newDebt) >= 1f;

                // 清空冲量缓冲区
                impulseBuffer.Clear();
            }
        }
    }
}