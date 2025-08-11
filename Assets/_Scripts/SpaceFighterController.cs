using System.Collections;
using _Scripts.Components;
using _Scripts.Systems;
using _Scripts.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace _Scripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class SpaceFighterController : MonoBehaviour, InputSystem_Actions.IPlayerActions
    {
        #region 变量和属性

        [Header("飞船挂点")] [SerializeField] private Transform weaponTransform;

        [Header("移动设置")] [SerializeField] private float maxForwardSpeed = 50f;
        [SerializeField] private float maxBackwardSpeed = 25f;
        [SerializeField] private float thrustAcceleration = 2000f;
        [SerializeField] private float strafeAcceleration = 1500f;
        [SerializeField] private float elevationAcceleration = 1500f;
        [SerializeField] private float inertialDamping = 200f;

        [Header("旋转设置")] [SerializeField] private float turnRate = 45f;

        [Header("输入设置")] [SerializeField] private float selectionInterval = 0.15f;
        private int _selectionDirection;
        private float _lastSelectionTime;
        private float _thrustInput;
        private float _strafeInput;
        private float _elevationInput;
        private bool _isLaserActive;

        [Header("Laser VFX 设置")] [SerializeField]
        private GameObject laserVFX;

        [SerializeField] private Transform laserVFXTransform01;
        [SerializeField] private Transform laserVFXTransform02;
        [SerializeField] private Transform laserVFXTransform03;
        [SerializeField] private Transform laserVFXTransform04;
        [SerializeField] private float maxLaserRange = 50f;
        [SerializeField] private float laserDamage = 500f;
        private bool _hasTargetCell;
        private int3 _laserTargetCell;
        private Vector3 _laserEndPoint;

        [Header("资源相关设置")] [SerializeField] private float optimalTemperature = 37f; // 飞船的“适宜”温度

        // [SerializeField] private float heatTransferCoefficient = 0.01f; // 飞船向最终目标温度变化的速度
        [SerializeField] private float inventoryConsumptionFactor = 0.01f; // 方块数量因为温差过大而消耗的速度
        [SerializeField] private float tempTolerance = 10f; // 定义背包方块能容忍多大的温差而不被消耗
        [SerializeField] private float massPenaltyFactor = 0.05f; // 每单位质量增加的能量消耗系数
        [SerializeField] private float propulsionEnergyCost = 20f; // 每秒推进消耗的基础能量
        [SerializeField] private float lifeSupportMoistureCost = 0.02f; // 每秒维生消耗的基础水分
        [SerializeField] private float moistureCostFactor = 0.05f; // 相比飞船“适宜”温度，每度温差消耗的水分系数
        [SerializeField] private float energyMax = 99999f; // 飞船能量的最大值
        public CellInventoryData[] CellInventory { get; private set; } = new CellInventoryData[19];
        public int CurrentCellIndex { get; private set; }
        public float CurrentOxygen { get; private set; } = 100f;
        public float MaxOxygen { get; private set; } = 300f;
        public float UltimateOxygen { get; private set; } = 1000f;
        public float ShipT { get; private set; } = 20f;
        public float ShipM { get; private set; } = 50f;
        public float Energy { get; private set; } = 1000f;
        private float _invT;
        private float _invM;
        private float _invMass;
        private float _envT;
        private float _envMass;
        private bool _isDead;

        // 组件引用
        private Rigidbody _rigidbody;
        private InputSystem_Actions _actions;
        private Camera _mainCamera;
        private BoxCollider[] _boxColliders;

        // ECS 相关
        private NativeHashMap<int3, Entity> _cellMap;
        private NativeArray<CellConfig> _cellConfigs;
        private NativeQueue<Entity> _cellPoolQueue;
        private World _world;
        private EntityManager _entityManager;
        private Entity _spaceshipProxyEntity;

        #endregion

        #region Mono 生命周期

        private void Awake()
        {
            _actions = new InputSystem_Actions();
            _actions.Player.AddCallbacks(this);
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            // 确保禁用 Unity 内置的阻尼
            _rigidbody.linearDamping = 0f;
            _rigidbody.angularDamping = 0f;

            _mainCamera = Camera.main;
            _boxColliders = GetComponentsInChildren<BoxCollider>();

            // ECS 相关初始化
            _world = World.DefaultGameObjectInjectionWorld;
            _entityManager = _world.EntityManager;
            InitializeSpaceshipProxyEntity();

            // 获取 CellMap 和 CellConfigs 引用
            var globalDataSystem = _world.GetExistingSystemManaged<GlobalDataInitSystem>();
            _cellMap = globalDataSystem.CellMap;
            _cellConfigs = globalDataSystem.CellConfigs;
            _cellPoolQueue = globalDataSystem.CellPoolQueue;
        }

        private void OnEnable()
        {
            _actions.Player.Enable();
        }

        private void OnDisable()
        {
            _actions.Player.Disable();
        }

        private void Update()
        {
            UpdateSelection();
            PerformLaser();
            UpdateLaserVFX();
            RecoverOxygen();

            if (CurrentOxygen <= 0 && !_isDead) StartCoroutine(RespawnAfterDelay());
        }


        private void FixedUpdate()
        {
            ApplyForceFeedback();
            HandleRotation();
            HandleMovement();
            CalculateResources();
            SyncSpaceshipDataToEcs();
        }

        #endregion

        #region 资源

        private void CalculateResources()
        {
            // 始终扫描环境，因为环境会影响飞船
            ScanEnvironment();

            // 计算库存资源（如果没有库存，这些值会是 0）
            CalculateInventoryResources();

            // 更新飞船的各项参数
            UpdateTemperature();
            UpdateMoisture();
            UpdateEnergy();

            // 只有当有库存时才更新库存消耗
            var hasInventory = false;
            foreach (var item in CellInventory)
            {
                if (item.Count > 0)
                {
                    hasInventory = true;
                    break;
                }
            }

            if (hasInventory)
            {
                UpdateInventoryCount();
            }
        }

        private void ScanEnvironment()
        {
            // 重置环境数据
            _envMass = 0;
            _envT = 0;
            float totalWeightedTemp = 0;

            // 获取飞船当前位置
            var shipPosition = transform.position;
            var scanRadius = 10f;

            // 计算扫描范围的网格坐标
            var minGrid = new int3(
                Mathf.FloorToInt(shipPosition.x - scanRadius),
                Mathf.FloorToInt(shipPosition.y - scanRadius),
                Mathf.FloorToInt(shipPosition.z - scanRadius)
            );
            var maxGrid = new int3(
                Mathf.CeilToInt(shipPosition.x + scanRadius),
                Mathf.CeilToInt(shipPosition.y + scanRadius),
                Mathf.CeilToInt(shipPosition.z + scanRadius)
            );

            // 遍历范围内的网格坐标
            for (int x = minGrid.x; x <= maxGrid.x; x++)
            {
                for (int y = minGrid.y; y <= maxGrid.y; y++)
                {
                    for (int z = minGrid.z; z <= maxGrid.z; z++)
                    {
                        var gridPos = new int3(x, y, z);
                        var worldPos = new Vector3(x, y, z);

                        // 检查距离是否在范围内
                        if (Vector3.Distance(shipPosition, worldPos) > scanRadius) continue;

                        // 查询该位置是否有 Cell
                        if (_cellMap.TryGetValue(gridPos, out var cellEntity))
                        {
                            // 确保实体存在且有必要的组件
                            if (_entityManager.Exists(cellEntity) &&
                                _entityManager.HasComponent<CellTag>(cellEntity) &&
                                _entityManager.HasComponent<Mass>(cellEntity) &&
                                _entityManager.HasComponent<Temperature>(cellEntity))
                            {
                                // 直接从 Mass 组件获取质量
                                var cellMass = _entityManager.GetComponentData<Mass>(cellEntity).Value;
                                var cellTemp = _entityManager.GetComponentData<Temperature>(cellEntity).Value;

                                // 累加质量和加权温度
                                _envMass += cellMass;
                                totalWeightedTemp += cellTemp * cellMass;
                            }
                        }
                    }
                }
            }

            // 计算质量加权平均温度
            if (_envMass > 0)
            {
                _envT = totalWeightedTemp / _envMass;
            }
        }

        private void CalculateInventoryResources()
        {
            // 重置库存数据
            _invMass = 0;
            _invT = 0;
            _invM = 0;
            float totalWeightedTemp = 0;
            float totalWeightedMoisture = 0;

            // 确保 CellConfigs 已初始化
            if (!_cellConfigs.IsCreated) return;

            // 遍历所有背包堆栈
            for (var i = 0; i < CellInventory.Length; i++)
            {
                var stack = CellInventory[i];
                if (stack.Count <= 0) continue;

                // 获取对应的 CellType（通过索引映射）
                var cellType = (CellTypeEnum)(-(i + 1)); // 映射索引到 CellType
                var cellConfig = _cellConfigs.GetCellConfig(cellType);

                // 计算该堆栈的总质量
                var stackMass = stack.Count * cellConfig.Mass;

                // 累加总质量
                _invMass += stackMass;

                // 累加加权温度
                totalWeightedTemp += stack.AvgTemperature * stackMass;

                // 累加加权水分
                totalWeightedMoisture += stack.AvgMoisture * stackMass;
            }

            // 计算质量加权平均值
            if (_invMass > 0)
            {
                _invT = totalWeightedTemp / _invMass;
                _invM = totalWeightedMoisture / _invMass;
            }
        }

        private void UpdateTemperature()
        {
            // 如果没有环境质量和库存质量，不进行温度更新
            var totalMass = _invMass + _envMass;
            if (totalMass <= 0) return;

            // 计算库存和环境的权重
            var invWeight = _invMass / totalMass;
            var envWeight = _envMass / totalMass;

            // 计算目标温度（加权平均）
            var targetTemperature = _invT * invWeight + _envT * envWeight;

            // 使用热传导系数让飞船温度向目标温度平滑变化
            var deltaT = (targetTemperature - ShipT) * 0.01f * Time.fixedDeltaTime;
            ShipT += deltaT;

            // 限制温度范围（避免极端值）
            ShipT = Mathf.Clamp(ShipT, -273.15f, 9999f);
        }

        private void UpdateMoisture()
        {
            // 1. 消耗水分
            // 基础维生消耗
            var moistureConsumption = lifeSupportMoistureCost * Time.fixedDeltaTime;

            // 温差消耗（距离适宜温度越远，消耗越多）
            var tempDifference = Mathf.Abs(ShipT - optimalTemperature);
            moistureConsumption += tempDifference * moistureCostFactor * Time.fixedDeltaTime;

            ShipM -= moistureConsumption;

            // 2. 补充水分（从库存缓慢补充）
            if (_invM > ShipM)
            {
                // 向库存水分靠拢
                var moistureSupply = (_invM - ShipM) * 0.1f * Time.fixedDeltaTime;
                ShipM += moistureSupply;
            }

            // 3. 约束范围
            ShipM = Mathf.Clamp(ShipM, 0f, 100f);

            // 4. 惩罚机制：如果水分为 0，缓慢消耗氧气
            if (ShipM <= 0f)
            {
                // 每秒消耗氧气
                CurrentOxygen -= 5f * Time.fixedDeltaTime;
                CurrentOxygen = Mathf.Max(CurrentOxygen, 0f);
            }
        }

        private void UpdateEnergy()
        {
            // 1. 消耗能量
            var energyConsumption = 0f;

            // 推进消耗（检查是否在移动）
            var isMoving = Mathf.Abs(_thrustInput) > 0.01f ||
                           Mathf.Abs(_strafeInput) > 0.01f ||
                           Mathf.Abs(_elevationInput) > 0.01f;

            if (isMoving)
            {
                energyConsumption += propulsionEnergyCost * Time.fixedDeltaTime;
            }

            // 质量惩罚消耗（背包越重，消耗越多）
            energyConsumption += _invMass * massPenaltyFactor * Time.fixedDeltaTime;

            Energy -= energyConsumption;

            // 2. 约束范围
            Energy = Mathf.Clamp(Energy, 0f, energyMax);

            // 3. 惩罚机制：如果能量为 0，缓慢消耗氧气
            if (Energy <= 0f)
            {
                // 每秒消耗氧气（比水分消耗稍少）
                CurrentOxygen -= 3f * Time.fixedDeltaTime;
                CurrentOxygen = Mathf.Max(CurrentOxygen, 0f);
            }
        }

        private void UpdateInventoryCount()
        {
            // 如果没有库存平均温度，不进行消耗计算
            if (_invMass <= 0) return;

            // 遍历每个背包堆栈
            for (var i = 0; i < CellInventory.Length; i++)
            {
                var stack = CellInventory[i];
                if (stack.Count <= 0) continue;

                // 计算该堆栈温度与全局背包平均温度的温差
                var tempDifference = Mathf.Abs(stack.AvgTemperature - _invT);

                // 如果温差超过容忍度，进行消耗
                if (tempDifference > tempTolerance)
                {
                    // 计算消耗量
                    var consumptionRate = (tempDifference - tempTolerance) *
                                          inventoryConsumptionFactor *
                                          Time.fixedDeltaTime;

                    // 减少堆栈数量（转换为整数）
                    var consumptionAmount = Mathf.CeilToInt(consumptionRate);
                    stack.Count = Mathf.Max(0, stack.Count - consumptionAmount);

                    // 更新回数组
                    CellInventory[i] = stack;
                }
            }
        }

        private void RecalculateInventoryProperties()
        {
            // 重新计算库存总属性
            CalculateInventoryResources();
        }

        #endregion

        #region ECS

        private void InitializeSpaceshipProxyEntity()
        {
            _spaceshipProxyEntity = _entityManager.CreateEntity();
            _entityManager.SetName(_spaceshipProxyEntity, "Spaceship_Proxy");

            // 添加组件
            _entityManager.AddComponent<SpaceshipProxyTag>(_spaceshipProxyEntity);
            _entityManager.AddComponent<SpaceshipMass>(_spaceshipProxyEntity);
            _entityManager.AddComponent<SpaceshipVelocity>(_spaceshipProxyEntity);
            _entityManager.AddBuffer<SpaceshipColliderBuffer>(_spaceshipProxyEntity);

            // 用于数据读取
            _entityManager.AddComponent<SpaceshipForceFeedback>(_spaceshipProxyEntity);
            _entityManager.SetComponentData(_spaceshipProxyEntity, new SpaceshipForceFeedback { Value = float3.zero });
        }

        private void ApplyForceFeedback()
        {
            if (!_entityManager.Exists(_spaceshipProxyEntity)) return;
            if (!_entityManager.HasComponent<SpaceshipForceFeedback>(_spaceshipProxyEntity)) return;

            var forceFeedback = _entityManager.GetComponentData<SpaceshipForceFeedback>(_spaceshipProxyEntity);
            _rigidbody.AddForce(forceFeedback.Value, ForceMode.Impulse);

            // 清零力反馈，避免重复应用
            _entityManager.SetComponentData(_spaceshipProxyEntity, new SpaceshipForceFeedback { Value = float3.zero });
        }

        private void SyncSpaceshipDataToEcs()
        {
            if (!_entityManager.Exists(_spaceshipProxyEntity)) return;

            // 更新质量
            _entityManager.SetComponentData(_spaceshipProxyEntity, new SpaceshipMass { Value = (int)_rigidbody.mass });

            // 更新速度
            _entityManager.SetComponentData(_spaceshipProxyEntity,
                new SpaceshipVelocity { Value = _rigidbody.linearVelocity });

            // 更新碰撞体数据
            var colliderBuffer = _entityManager.GetBuffer<SpaceshipColliderBuffer>(_spaceshipProxyEntity);
            colliderBuffer.Clear();

            // 获取所有 BoxCollider
            foreach (var boxCollider in _boxColliders)
            {
                // 计算考虑缩放的实际大小
                var localScale = boxCollider.transform.lossyScale;
                var scaledSize = Vector3.Scale(boxCollider.size, localScale);

                colliderBuffer.Add(new SpaceshipColliderBuffer
                {
                    Center = boxCollider.transform.TransformPoint(boxCollider.center),
                    Size = scaledSize,
                    Rotation = boxCollider.transform.rotation
                });
            }
        }

        #endregion

        #region Movement

        private void HandleRotation()
        {
            if (!_mainCamera) return;
            var cameraForward = _mainCamera.transform.forward;

            // 计算目标旋转
            if (cameraForward == Vector3.zero) return;
            var targetRotation = Quaternion.LookRotation(cameraForward);

            // 使用转向速度平滑旋转到目标朝向
            var rotationSpeed = turnRate * Time.fixedDeltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed);
        }

        private void HandleMovement()
        {
            var force = Vector3.zero;

            // 基于飞船自身方向的前进后退
            if (Mathf.Abs(_thrustInput) > 0.01f)
            {
                // 计算当前在飞船前进方向上的速度
                var forwardVelocity = Vector3.Dot(_rigidbody.linearVelocity, transform.forward);

                // 检查是否已达到速度限制
                if ((_thrustInput > 0 && forwardVelocity < maxForwardSpeed) ||
                    (_thrustInput < 0 && forwardVelocity > -maxBackwardSpeed))
                    force += _thrustInput * thrustAcceleration * transform.forward;
            }

            // 基于摄像头的左右环绕移动
            if (Mathf.Abs(_strafeInput) > 0.01f && _mainCamera)
                force += _strafeInput * strafeAcceleration * _mainCamera.transform.right;

            // 上下移动
            if (Mathf.Abs(_elevationInput) > 0.01f)
                force += _elevationInput * elevationAcceleration * transform.up;

            // 如果没有输入，施加线性阻尼
            if (Mathf.Abs(_thrustInput) < 0.01f && Mathf.Abs(_strafeInput) < 0.01f &&
                Mathf.Abs(_elevationInput) < 0.01f)
                force = -_rigidbody.linearVelocity * inertialDamping;

            _rigidbody.AddForce(force, ForceMode.Force);
        }

        #endregion

        #region Laser

        private void PerformLaser()
        {
            if (!_isLaserActive)
            {
                _hasTargetCell = false;
                return;
            }

            // 从屏幕中心发射射线
            var ray = _mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

            // 执行步进式射线检测
            var hitResult = CellDetectionByLaser(weaponTransform.position, ray.direction);

            _hasTargetCell = hitResult.hitFound;
            _laserTargetCell = hitResult.cellCoordinate;
            _laserEndPoint = hitResult.endPoint;

            // 如果有目标方块，进行 Laser 伤害
            if (_hasTargetCell)
            {
                ApplyLaserDamageToCell(_laserTargetCell);
            }
        }

        private (bool hitFound, int3 cellCoordinate, Vector3 endPoint) CellDetectionByLaser(Vector3 weaponPosition,
            Vector3 direction)
        {
            const float stepSize = 0.1f; // 步进大小
            var step = Mathf.CeilToInt(maxLaserRange / stepSize); // 根据最大范围计算最大步数

            // 计算武器位置在射线上的投影点作为起点
            var ray = _mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            var currentPos = ray.origin + Vector3.Project(weaponPosition - ray.origin, ray.direction);

            while (step > 0)
            {
                currentPos += direction * stepSize;
                var gridCoordinate = WorldToGridCoordinate(currentPos); // 计算当前位置的网格坐标

                // 检查这个网格位置是否有 Cell，且具有 CellTag 组件
                if (_cellMap.TryGetValue(gridCoordinate, out var cellEntity) &&
                    _entityManager.HasComponent<CellTag>(cellEntity))
                    return (true, gridCoordinate,
                        new float3(gridCoordinate.x, gridCoordinate.y + 0.5f, gridCoordinate.z));

                step--;
            }

            return (false, int3.zero, currentPos);
        }

        private void ApplyLaserDamageToCell(int3 targetCellCoordinate)
        {
            if (!_cellMap.TryGetValue(targetCellCoordinate, out var cellEntity)) return;
            if (!_entityManager.Exists(cellEntity)) return;
            if (!_entityManager.HasComponent<Health>(cellEntity)) return;

            // 获取当前 Health 值
            var healthComponent = _entityManager.GetComponentData<Health>(cellEntity);
            var currentHealth = healthComponent.Value;

            // 应用 Laser 伤害
            var damageAmount = laserDamage * Time.deltaTime;
            currentHealth -= damageAmount;

            // 更新 Health 组件
            healthComponent.Value = Mathf.Max(0, currentHealth);
            _entityManager.SetComponentData(cellEntity, healthComponent);

            // 如果 Health <= 0，开采方块
            if (currentHealth <= 0)
            {
                MineCell(targetCellCoordinate, cellEntity);
            }
        }

        private void MineCell(int3 targetCellCoordinate, Entity cellEntity)
        {
            // 获取方块类型和当前属性
            if (!_entityManager.HasComponent<CellType>(cellEntity) ||
                !_entityManager.HasComponent<Temperature>(cellEntity) ||
                !_entityManager.HasComponent<Moisture>(cellEntity) ||
                !_entityManager.HasComponent<Energy>(cellEntity) ||
                !_entityManager.HasComponent<Mass>(cellEntity)) return;

            var cellTypeComponent = _entityManager.GetComponentData<CellType>(cellEntity);
            var temperatureComponent = _entityManager.GetComponentData<Temperature>(cellEntity);
            var moistureComponent = _entityManager.GetComponentData<Moisture>(cellEntity);
            var energyComponent = _entityManager.GetComponentData<Energy>(cellEntity);
            var massComponent = _entityManager.GetComponentData<Mass>(cellEntity);

            var cellType = cellTypeComponent.Value;

            // 忽略 None 类型
            if (cellType == CellTypeEnum.None) return;

            var cellMass = massComponent.Value;
            var currentTemperature = temperatureComponent.Value;
            var currentMoisture = moistureComponent.Value;
            var currentEnergy = energyComponent.Value;

            // 使用 GameManager 的映射获取正确的背包索引
            if (!GameManager.cellTypeIndexMap.TryGetValue(cellType, out var inventoryIndex))
                return;

            // 确保索引有效
            if (inventoryIndex < 0 || inventoryIndex >= CellInventory.Length) return;

            var oldTotalMass = _invMass; // 更新库存加权平均温度和水分
            _invMass += cellMass; // 更新库存总质量
            if (oldTotalMass <= 0)
            {
                // 第一次添加
                _invT = currentTemperature;
                _invM = currentMoisture;
            }
            else
            {
                // 计算新的加权平均值
                _invT = (_invT * oldTotalMass + currentTemperature * cellMass) / _invMass;
                _invM = (_invM * oldTotalMass + currentMoisture * cellMass) / _invMass;
            }

            // 将方块添加到背包
            var currentInventoryData = CellInventory[inventoryIndex];
            currentInventoryData.Count++;

            // 更新 CellInventoryData 的平均温度和湿度
            var totalCount = currentInventoryData.Count;
            if (totalCount == 1)
            {
                currentInventoryData.AvgTemperature = currentTemperature;
                currentInventoryData.AvgMoisture = currentMoisture;
            }
            else
            {
                currentInventoryData.AvgTemperature =
                    (currentInventoryData.AvgTemperature * (totalCount - 1) + currentTemperature) / totalCount;
                currentInventoryData.AvgMoisture =
                    (currentInventoryData.AvgMoisture * (totalCount - 1) + currentMoisture) / totalCount;
            }

            CellInventory[inventoryIndex] = currentInventoryData;

            // 将能量直接添加到飞船总能量
            Energy += currentEnergy;
            Energy = Mathf.Clamp(Energy, 0f, energyMax);

            // 调用 CellUtility.SetCellTypeToNone 移除方块
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            CellUtility.SetCellTypeToNone(cellEntity, ecb, _cellMap, targetCellCoordinate);
            ecb.Playback(_entityManager);
        }

        #endregion

        #region VFX

        private void UpdateLaserVFX()
        {
            laserVFX.SetActive(_isLaserActive);
            if (!_isLaserActive) return;

            var laserVector = _laserEndPoint - weaponTransform.position;
            var laserDirection = laserVector.normalized;
            var perpendicular = Vector3.Cross(laserDirection, Vector3.up).normalized;

            // 起点
            laserVFXTransform01.position = weaponTransform.position;
            laserVFXTransform01.rotation = weaponTransform.rotation;

            // 33% 位置 + 随机偏移
            var offset1 = perpendicular * UnityEngine.Random.Range(-0.5f, 0.5f) +
                          Vector3.up * UnityEngine.Random.Range(-0.5f, 0.5f);
            laserVFXTransform02.position =
                weaponTransform.position + laserDirection * (laserVector.magnitude * 0.33f) + offset1;
            laserVFXTransform02.rotation = weaponTransform.rotation;

            // 66% 位置 + 随机偏移
            var offset2 = perpendicular * UnityEngine.Random.Range(-0.5f, 0.5f) +
                          Vector3.up * UnityEngine.Random.Range(-0.5f, 0.5f);
            laserVFXTransform03.position =
                weaponTransform.position + laserDirection * (laserVector.magnitude * 0.66f) + offset2;
            laserVFXTransform03.rotation = weaponTransform.rotation;

            // 终点
            laserVFXTransform04.position = _laserEndPoint;
            laserVFXTransform04.rotation = weaponTransform.rotation;
        }

        #endregion

        #region Input System

        public void OnMove(InputAction.CallbackContext context)
        {
            var input = context.ReadValue<Vector2>();
            _thrustInput = input.y;
            _strafeInput = input.x;
        }

        public void OnElevation(InputAction.CallbackContext context)
        {
            _elevationInput = context.ReadValue<float>();
        }

        public void OnLaser(InputAction.CallbackContext context)
        {
            if (context.started) _isLaserActive = true;
            else if (context.canceled) _isLaserActive = false;
        }

        public void OnProjectile(InputAction.CallbackContext context)
        {
            if (context.performed)
                GameManager.Instance.ShotProjectile(weaponTransform.position, _mainCamera.transform.forward, 5f);
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            // 检查是否有足够的方块可以发射
            if (CurrentCellIndex < 0 || CurrentCellIndex >= CellInventory.Length) return;
            if (CellInventory[CurrentCellIndex].Count <= 0) return;

            // 获取当前选中的方块类型
            CellTypeEnum cellTypeToFire = CellTypeEnum.None;
            foreach (var kvp in GameManager.cellTypeIndexMap)
            {
                if (kvp.Value == CurrentCellIndex)
                {
                    cellTypeToFire = kvp.Key;
                    break;
                }
            }

            if (cellTypeToFire == CellTypeEnum.None) return;

            // 计算发射位置（武器位置往前一点）
            var firePosition = weaponTransform.position + _mainCamera.transform.forward * 0.5f;
            var gridCoordinate = WorldToGridCoordinate(firePosition);

            // 创建实体命令缓冲区
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 设置初始冲量为相机前方方向
            var initialImpulse = _mainCamera.transform.forward * 200f; // 调整力度

            if (_cellPoolQueue.TryDequeue(out var cell))
            {
                // 获取 CellConfigTag 实体
                var query = _entityManager.CreateEntityQuery(typeof(CellConfigTag));
                var cellConfigEntity = query.GetSingletonEntity();

                // 使用 CellUtility 添加方块到世界
                var success = CellUtility.TryAddCellToWorld(
                    cell,
                    _entityManager,
                    ecb,
                    _cellMap,
                    cellConfigEntity,
                    cellTypeToFire,
                    gridCoordinate,
                    initialImpulse
                );

                if (success)
                {
                    // 减少背包中的方块数量
                    var inventoryData = CellInventory[CurrentCellIndex];
                    inventoryData.Count--;
                    CellInventory[CurrentCellIndex] = inventoryData;

                    // 更新库存总属性
                    RecalculateInventoryProperties();
                }
            }

            ecb.Playback(_entityManager);
        }

        public void OnNext(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                CurrentCellIndex = (CurrentCellIndex + 1) % CellInventory.Length;
            }
            else if (context.performed)
            {
                _selectionDirection = 1;
                _lastSelectionTime = Time.time;
            }
            else if (context.canceled)
            {
                _selectionDirection = 0;
            }
        }

        public void OnPrevious(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                CurrentCellIndex = (CurrentCellIndex - 1 + CellInventory.Length) % CellInventory.Length;
            }
            else if (context.performed)
            {
                _selectionDirection = -1;
                _lastSelectionTime = Time.time;
            }
            else if (context.canceled)
            {
                _selectionDirection = 0;
            }
        }

        public void OnMenu(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (GameManager.Instance.IsMenuOpened)
                GameManager.Instance.OnGameResume();
            else
                GameManager.Instance.OnGamePause();
        }

        public void OnTool(InputAction.CallbackContext context)
        {
            if (context.performed) GameManager.Instance.ChangeToolActive();
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            // 确保 EntityManager 已初始化
            if (_entityManager == default)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null)
                    _entityManager = world.EntityManager;
                else
                    return;
            }

            // 设置 Gizmos 颜色
            Gizmos.color = Color.cyan;

            // 查询所有包含 SpaceshipTempCellTag 组件的实体
            using var query = _entityManager.CreateEntityQuery(typeof(SpaceshipTempCellTag), typeof(LocalTransform));
            var entities = query.ToEntityArray(Allocator.Temp);
            var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            for (var i = 0; i < entities.Length; i++)
            {
                var localTransform = transforms[i];
                var position = localTransform.Position;

                // 计算网格坐标（与 CellMap 中的键一致）
                var gridPos = (int3)math.floor(position);

                // 网格单元的实际占用区域
                var cellCenter = new float3(gridPos) + new float3(0, 0.5f, 0);

                // 绘制网格单元的实际占用区域
                Gizmos.DrawWireCube(cellCenter, Vector3.one);
            }

            entities.Dispose();
            transforms.Dispose();
        }

        #endregion

        #region 辅助方法

        private void RecoverOxygen()
        {
            // 获取地球中心和半径
            var earthCenter = GameManager.Instance.earthCalculator.EarthCenter;
            var earthRadius = GameManager.Instance.earthCalculator.EarthRadius;
            var recoverRadius = earthRadius + 15f;
            var shipPos = transform.position;
            var distance = Vector3.Distance(shipPos, new Vector3(earthCenter.x, earthCenter.y, earthCenter.z));

            // 在母星范围内，逐渐恢复氧气
            if (!(distance <= recoverRadius) || !(CurrentOxygen < MaxOxygen)) return;
            const float recoverRate = 30f;
            CurrentOxygen += recoverRate * Time.deltaTime;
            if (CurrentOxygen > MaxOxygen) CurrentOxygen = MaxOxygen;
        }

        private IEnumerator RespawnAfterDelay()
        {
            // 禁用输入
            _actions.Player.Disable();
            _isDead = true;
            GameManager.Instance.OnGameOver(true);

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            Debug.Log("飞船死亡 - 3 秒后复活...");

            // 等待 3 秒
            yield return new WaitForSeconds(3f);

            // 复活
            RespawnSpaceship();

            // 恢复输入
            _actions.Player.Enable();
            _isDead = false;
            GameManager.Instance.OnGameOver(false);
        }

        private void RespawnSpaceship()
        {
            // 重置飞船位置和速度
            var earthCenter = GameManager.Instance.earthCalculator.EarthCenter;
            var earthRadius = GameManager.Instance.earthCalculator.EarthRadius;
            transform.position = new Vector3(earthCenter.x, earthCenter.y + earthRadius + 10f, earthCenter.z);

            // 重置飞船速度
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            // 重置资源
            ShipT = 20f;
            ShipM = 50f;
            Energy = 1000f;
            CurrentOxygen = MaxOxygen;

            // 清空背包
            for (var i = 0; i < CellInventory.Length; i++)
                CellInventory[i] = new CellInventoryData { Count = 0, AvgTemperature = 20f, AvgMoisture = 50f };

            // 重置激光状态
            _isLaserActive = false;
        }

        private static int3 WorldToGridCoordinate(Vector3 worldPosition)
        {
            return new int3(
                Mathf.RoundToInt(worldPosition.x),
                Mathf.RoundToInt(worldPosition.y),
                Mathf.RoundToInt(worldPosition.z)
            );
        }

        private void UpdateSelection()
        {
            if (_selectionDirection == 0) return;
            if (!(Time.time - _lastSelectionTime >= selectionInterval)) return;

            CurrentCellIndex = (CurrentCellIndex + _selectionDirection + CellInventory.Length) % CellInventory.Length;
            _lastSelectionTime = Time.time;
        }

        #endregion
    }
}