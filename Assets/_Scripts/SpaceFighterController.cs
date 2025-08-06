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
        [SerializeField] private float autoRecenterDelay = 1f; // 自动回中延迟时间（秒）
        [SerializeField] private float recenterThreshold = 10f; // 摄像头偏移多少度才触发自动回中
        [SerializeField] private float cameraResetSpeed = 30f; // 摄像头重置速度

        [Header("鼠标设置")] [SerializeField] private bool lockCursorOnStart = true; // 开始时是否锁定鼠标

        private CinemachineOrbitalFollow _orbitalFollow;
        private float _lastLookInputTime; // 摄像头自动回中状态

        // 输入状态
        private Vector2 _inputMove;        // 左摇杆：移动控制
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

            if (lockCursorOnStart)
                Cursor.lockState = CursorLockMode.Locked;
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
            HandleMovement();
        }

        #region 战斗机控制

        private void HandleMovement()
        {
            Vector3 force = Vector3.zero;

            if (_inputMove.sqrMagnitude > 0.01f)
            {
                // 基于主摄像机方向的移动
                Vector3 cameraForward = Camera.main.transform.forward;
                Vector3 cameraRight = Camera.main.transform.right;
                
                // 保持水平移动（移除Y分量）
                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();
                
                // 计算移动方向
                Vector3 moveDirection = cameraForward * _inputMove.y + cameraRight * _inputMove.x;
                force = moveDirection * acceleration;
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

        private static void ToggleCursorLock()
        {
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

        #region Input System

        public void OnMove(InputAction.CallbackContext context)
        {
            _inputMove = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (context.performed) _lastLookInputTime = Time.time;
        }

        public void OnRoll(InputAction.CallbackContext context)
        {
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
            if (context.performed)
            {
                ToggleCursorLock();
            }
        }

        public void OnTool(InputAction.CallbackContext context)
        {
        }

        #endregion
    }
}