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

        [Header("旋转设置")] [SerializeField] private float maxTurnRate = 90f; // 每秒最大角度

        [Header("摄像头设置")] [SerializeField] private CinemachineCamera freelookCamera;
        [SerializeField] private float autoRecenterDelay = 3f; // 自动回中延迟时间（秒）
        [SerializeField] private float recenterThreshold = 10f; // 摄像头偏移多少度才触发自动回中
        [SerializeField] private float cameraResetSpeed = 20f; // 摄像头重置速度
        private CinemachineOrbitalFollow _orbitalFollow;
        private float _lastLookInputTime; // 摄像头自动回中状态

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

            if (freelookCamera != null)
                _orbitalFollow = freelookCamera.GetComponent<CinemachineOrbitalFollow>();
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
            HandleCameraRecenter();
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

        private void HandleCameraRecenter()
        {
            if (!_orbitalFollow) return;

            // 检查是否超过延迟时间
            if (Time.time - _lastLookInputTime < autoRecenterDelay) return;

            var currentYaw = _orbitalFollow.HorizontalAxis.Value;
            var currentPitch = _orbitalFollow.VerticalAxis.Value;

            // 检查是否需要回中（任一轴超过阈值就执行回中）
            if (!(Mathf.Abs(currentYaw) > recenterThreshold) &&
                !(Mathf.Abs(currentPitch - 17.5f) > recenterThreshold)) return;
            // 逐渐回中
            var targetYaw = Mathf.MoveTowards(currentYaw, 0, cameraResetSpeed * Time.deltaTime);
            var targetPitch = Mathf.MoveTowards(currentPitch, 17.5f, cameraResetSpeed * Time.deltaTime);

            _orbitalFollow.HorizontalAxis.Value = targetYaw;
            _orbitalFollow.VerticalAxis.Value = targetPitch;
        }

        private void ResetCamera()
        {
            if (!_orbitalFollow) return;

            // 重置轨道角度到默认值
            _orbitalFollow.HorizontalAxis.Value = 0f;
            _orbitalFollow.VerticalAxis.Value = 17.5f;
        }

        #endregion

        #region Input System

        public void OnLook(InputAction.CallbackContext context)
        {
            if (context.performed) _lastLookInputTime = Time.time;
        }

        public void OnPitchYaw(InputAction.CallbackContext context)
        {
            _inputPitchYaw = context.ReadValue<Vector2>();
        }

        public void OnRoll(InputAction.CallbackContext context)
        {
            _inputRoll = context.ReadValue<float>();
        }

        public void OnResetCamera(InputAction.CallbackContext context)
        {
            if (context.performed) ResetCamera();
        }

        public void OnAccelerate(InputAction.CallbackContext context)
        {
            _inputThrust = context.ReadValueAsButton() ? 1 : 0;
        }

        public void OnDecelerate(InputAction.CallbackContext context)
        {
            _inputThrust = context.ReadValueAsButton() ? -1 : 0;
        }

        public void OnNext(InputAction.CallbackContext context)
        {
        }

        public void OnPrevious(InputAction.CallbackContext context)
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

        public void OnMenu(InputAction.CallbackContext context)
        {
        }

        public void OnTool(InputAction.CallbackContext context)
        {
        }

        #endregion
    }
}