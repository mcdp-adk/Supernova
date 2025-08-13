using UnityEngine;

namespace _Scripts
{
    public class CanvasFacingCamera : MonoBehaviour
    {
        private Camera _targetCamera;

        private void LateUpdate()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
                if (_targetCamera == null) return;
            }
            transform.forward = _targetCamera.transform.forward;
        }
    }
}