using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

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

        [Header("摄像头设置")] [SerializeField] private CinemachineCamera freelookCamera; // Cinemachine 3 的摄像头组件
        [SerializeField] private float cameraResetSpeed = 2f; // 摄像头重置速度

        // 输入状态
        private Vector2 _inputPitchYaw;
        private float _inputRoll;
        private int _inputThrust; // 1=加速, -1=减速, 0=滑行

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
            if (_inputPitchYaw.sqrMagnitude > 0.01f || Mathf.Abs(_inputRoll) > 0.01f)
            {
                // 基于输入的即时旋转
                var inputPitch = _inputPitchYaw.y;
                var inputYaw = _inputPitchYaw.x;
                var inputRoll = -_inputRoll;

                // 转换为角速度（每秒度数转换为每秒弧度）
                var targetAngularVelocity = new Vector3(
                    inputPitch * maxTurnRate * Mathf.Deg2Rad,
                    inputYaw * maxTurnRate * Mathf.Deg2Rad,
                    inputRoll * maxTurnRate * Mathf.Deg2Rad
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

            if (_inputThrust != 0)
            {
                // 施加推力（正值为前进，负值为后退）
                force = transform.forward * (_inputThrust * acceleration);

                // 检查速度限制
                var currentSpeed = Vector3.Dot(_rigidbody.linearVelocity, transform.forward);
                var maxSpeed = _inputThrust > 0 ? maxForwardSpeed : maxBackwardSpeed;
                var isAtMaxSpeed = _inputThrust > 0 ? currentSpeed >= maxSpeed : currentSpeed <= -maxSpeed;

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

        #region 摄像头控制

        private void ResetCamera()
        {
            if (freelookCamera == null) return;

            // 获取 Orbital Follow 组件
            var orbitalFollow = freelookCamera.GetComponent<CinemachineOrbitalFollow>();
            if (orbitalFollow != null)
            {
                // 重置轨道角度到默认值
                orbitalFollow.HorizontalAxis.Value = 0f;
                orbitalFollow.VerticalAxis.Value = 17.5f;
            }
        }

        #endregion

        #region Input System

        public void OnPitchYaw(InputAction.CallbackContext context)
        {
            _inputPitchYaw = context.ReadValue<Vector2>();
        }

        public void OnRoll(InputAction.CallbackContext context)
        {
            _inputRoll = context.ReadValue<float>();
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
            _inputThrust = context.ReadValueAsButton() ? 1 : 0;
        }

        public void OnDecelerate(InputAction.CallbackContext context)
        {
            _inputThrust = context.ReadValueAsButton() ? -1 : 0;
        }

        public void OnMenu(InputAction.CallbackContext context)
        {
        }

        public void OnTool(InputAction.CallbackContext context)
        {
        }

        public void OnResetCamera(InputAction.CallbackContext context)
        {
            if (context.performed)
                ResetCamera();
        }

        #endregion
    }
}