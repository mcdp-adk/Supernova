using _Scripts.Components;
using _Scripts.Systems;
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
        [Header("飞船挂点")] [SerializeField] private Transform weaponTransform;

        [Header("移动设置")] [SerializeField] private float maxForwardSpeed = 50f;
        [SerializeField] private float maxBackwardSpeed = 25f;
        [SerializeField] private float thrustAcceleration = 2000f;
        [SerializeField] private float strafeAcceleration = 1500f;
        [SerializeField] private float elevationAcceleration = 1500f;
        [SerializeField] private float inertialDamping = 200f;

        [Header("旋转设置")] [SerializeField] private float turnRate = 45f;

        [Header("Laser VFX 设置")] [SerializeField]
        private GameObject laserVFX;

        [SerializeField] private Transform laserVFXTransform01;
        [SerializeField] private Transform laserVFXTransform02;
        [SerializeField] private Transform laserVFXTransform03;
        [SerializeField] private Transform laserVFXTransform04;
        [SerializeField] private float maxLaserRange = 50f;

        // 输入状态
        private float _thrustInput;
        private float _strafeInput;
        private float _elevationInput;
        private bool _isLaserActive;

        // 组件引用
        private Rigidbody _rigidbody;
        private InputSystem_Actions _actions;
        private Camera _mainCamera;
        private BoxCollider[] _boxColliders;

        // ECS 相关
        private NativeHashMap<int3, Entity> _cellMap;
        private World _world;
        private EntityManager _entityManager;
        private Entity _spaceshipProxyEntity;

        // 射线检测相关
        private bool _hasTargetCell;
        private int3 _laserTargetCell;
        private Vector3 _laserEndPoint;

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

            // 获取 CellMap 引用
            _cellMap = _world.GetExistingSystemManaged<GlobalDataInitSystem>().CellMap;
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
            PerformLaser();
            UpdateLaserVFX();
        }

        private void FixedUpdate()
        {
            ApplyForceFeedback();
            HandleRotation();
            HandleMovement();
            SyncSpaceshipDataToEcs();
        }

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
            laserVFXTransform02.position = weaponTransform.position + laserDirection * (laserVector.magnitude * 0.33f) + offset1;
            laserVFXTransform02.rotation = weaponTransform.rotation;

            // 66% 位置 + 随机偏移
            var offset2 = perpendicular * UnityEngine.Random.Range(-0.5f, 0.5f) +
                          Vector3.up * UnityEngine.Random.Range(-0.5f, 0.5f);
            laserVFXTransform03.position = weaponTransform.position + laserDirection * (laserVector.magnitude * 0.66f) + offset2;
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
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
        }

        public void OnNext(InputAction.CallbackContext context)
        {
        }

        public void OnPrevious(InputAction.CallbackContext context)
        {
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

        private static int3 WorldToGridCoordinate(Vector3 worldPosition)
        {
            return new int3(
                Mathf.RoundToInt(worldPosition.x),
                Mathf.RoundToInt(worldPosition.y),
                Mathf.RoundToInt(worldPosition.z)
            );
        }

        #endregion
    }
}

