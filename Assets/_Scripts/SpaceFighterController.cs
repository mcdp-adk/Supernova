using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class SpaceFighterController : MonoBehaviour, InputSystem_Actions.IPlayerActions
    {
        [Header("移动设置")] [SerializeField] private float acceleration = 20f;
        [SerializeField] private float maxForwardSpeed = 50f;
        [SerializeField] private float maxBackwardSpeed = 25f;
        [SerializeField] private float inertialDamping = 2f;

        [Header("旋转设置")] [SerializeField] private float maxTurnRate = 90f; // 每秒度数

        // 输入状态
        private Vector2 _moveInput;
        private int _thrustInput; // 1=加速, -1=减速, 0=滑行

        // 组件引用
        private Rigidbody _rigidbody;
        private InputSystem_Actions _actions;

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
        }

        #region 战斗机控制

        private void HandleRotation()
        {
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                // 基于输入的即时旋转
                var pitchInput = -_moveInput.y; // 反转Y轴以获得直观控制
                var yawInput = _moveInput.x;

                // 转换为角速度（每秒度数转换为每秒弧度）
                var targetAngularVelocity = new Vector3(
                    pitchInput * maxTurnRate * Mathf.Deg2Rad,
                    yawInput * maxTurnRate * Mathf.Deg2Rad,
                    0f
                );

                // 在本地空间应用旋转
                _rigidbody.angularVelocity = transform.TransformDirection(targetAngularVelocity);
            }
            else
            {
                // 无输入时立即停止旋转
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void HandleMovement()
        {
            Vector3 force;

            if (_thrustInput != 0)
            {
                // 施加推力（正值为前进，负值为后退）
                force = transform.forward * (_thrustInput * acceleration);

                // 检查速度限制
                var currentSpeed = Vector3.Dot(_rigidbody.linearVelocity, transform.forward);
                var maxSpeed = _thrustInput > 0 ? maxForwardSpeed : maxBackwardSpeed;
                var isAtMaxSpeed = _thrustInput > 0 ? currentSpeed >= maxSpeed : currentSpeed <= -maxSpeed;

                if (isAtMaxSpeed)
                {
                    force = Vector3.zero;
                }
            }
            else
            {
                // 滑行时施加惯性阻尼
                force = -_rigidbody.linearVelocity * inertialDamping;
            }

            _rigidbody.AddForce(force, ForceMode.Force);
        }

        #endregion

        #region Input System

        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
        }

        public void OnPrevious(InputAction.CallbackContext context)
        {
        }

        public void OnNext(InputAction.CallbackContext context)
        {
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

        public void OnAccelerate(InputAction.CallbackContext context)
        {
            _thrustInput = context.ReadValueAsButton() ? 1 : 0;
        }

        public void OnDecelerate(InputAction.CallbackContext context)
        {
            _thrustInput = context.ReadValueAsButton() ? -1 : 0;
        }

        public void OnMenu(InputAction.CallbackContext context)
        {
        }

        public void OnTool(InputAction.CallbackContext context)
        {
        }

        #endregion
    }
}