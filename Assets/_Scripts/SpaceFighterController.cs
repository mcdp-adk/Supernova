using _Scripts.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace _Scripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class SpaceFighterController : MonoBehaviour, InputSystem_Actions.IPlayerActions
    {
        [Header("移动设置")] [SerializeField] private float acceleration = 20f;
        [SerializeField] private float maxForwardSpeed = 50f;
        [SerializeField] private float maxBackwardSpeed = 25f;
        [SerializeField] private float strafeSpeed = 15f;
        [SerializeField] private float elevationSpeed = 15f; // 上升下降速度
        [SerializeField] private float inertialDamping = 2f;


        [Header("旋转设置")] [SerializeField] private float turnRate = 45f;

        [Header("摄像头设置")] [SerializeField] private CinemachineCamera followCamera;

        [Header("鼠标设置")] [SerializeField] private bool lockCursorOnStart = true;

        // 输入状态
        private float _thrustInput;
        private float _strafeInput;
        private float _elevationInput;

        // 组件引用
        private Rigidbody _rigidbody;
        private InputSystem_Actions _actions;
        private Camera _mainCamera;

        // ECS 相关
        private EntityManager _entityManager;
        private Entity _spaceshipProxyEntity;

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

            // 缓存主摄像头
            _mainCamera = Camera.main;

            if (lockCursorOnStart)
                Cursor.lockState = CursorLockMode.Locked;

            // 初始化 ECS 代理实体
            InitializeSpaceshipProxyEntity();
        }

        private void OnEnable()
        {
            _actions.Player.Enable();
        }

        private void OnDisable()
        {
            _actions.Player.Disable();
        }

        private void FixedUpdate()
        {
            HandleRotation();
            HandleMovement();
            SyncSpaceshipDataToEcs();
        }

        #region ECS

        private void InitializeSpaceshipProxyEntity()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _spaceshipProxyEntity = _entityManager.CreateEntity();

            // 设置实体名称（用于调试）
            _entityManager.SetName(_spaceshipProxyEntity, "Spaceship_Proxy");

            // 添加组件
            _entityManager.AddComponent<SpaceshipProxyTag>(_spaceshipProxyEntity);
            _entityManager.AddComponent<SpaceshipMass>(_spaceshipProxyEntity);
            _entityManager.AddComponent<SpaceshipVelocity>(_spaceshipProxyEntity);
            _entityManager.AddBuffer<SpaceshipColliderBuffer>(_spaceshipProxyEntity);

            // 用于数据读取
            _entityManager.AddComponent<SpaceshipForceFeedback>(_spaceshipProxyEntity);
        }

        private void SyncSpaceshipDataToEcs()
        {
            if (!_entityManager.Exists(_spaceshipProxyEntity))
                return;

            // 更新质量
            _entityManager.SetComponentData(_spaceshipProxyEntity, new SpaceshipMass { Value = (int)_rigidbody.mass });

            // 更新速度
            _entityManager.SetComponentData(_spaceshipProxyEntity,
                new SpaceshipVelocity { Value = _rigidbody.linearVelocity });

            // 更新碰撞体数据
            var colliderBuffer = _entityManager.GetBuffer<SpaceshipColliderBuffer>(_spaceshipProxyEntity);
            colliderBuffer.Clear();

            // 获取所有 BoxCollider
            var boxColliders = GetComponentsInChildren<BoxCollider>();
            foreach (var boxCollider in boxColliders)
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

        #region 移动控制

        private void HandleRotation()
        {
            if (!_mainCamera) return;

            // 获取摄像头的完整前向方向（包括上下角度）
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
                    force += _thrustInput * acceleration * transform.forward;
            }

            // 基于摄像头的左右环绕移动
            if (Mathf.Abs(_strafeInput) > 0.01f && _mainCamera)
                force += _strafeInput * strafeSpeed * _mainCamera.transform.right;

            // 上下移动
            if (Mathf.Abs(_elevationInput) > 0.01f)
                force += _elevationInput * elevationSpeed * transform.up;

            // 如果没有输入，施加惯性阻尼
            if (Mathf.Abs(_thrustInput) < 0.01f && Mathf.Abs(_strafeInput) < 0.01f &&
                Mathf.Abs(_elevationInput) < 0.01f)
                force = -_rigidbody.linearVelocity * inertialDamping;

            _rigidbody.AddForce(force, ForceMode.Force);
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

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void OnTool(InputAction.CallbackContext context)
        {
        }

        #endregion

        #region 调试可视化

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;

            // 查询所有临时飞船单元格
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<SpaceshipTempCellTag>(),
                ComponentType.ReadOnly<LocalTransform>());
            var entities = query.ToEntityArray(Allocator.Temp);

            Gizmos.color = Color.cyan;
            foreach (var entity in entities)
            {
                var localTransform = entityManager.GetComponentData<LocalTransform>(entity);
                Gizmos.DrawCube(localTransform.Position, Vector3.one);
            }

            entities.Dispose();
        }

        #endregion
    }
}