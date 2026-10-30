using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InspectionDemo
{
    /// <summary>
    /// 第一人称移动 + 视角控制。
    ///
    /// 两个刻意的选择：
    /// 1. 移动用 Input.GetKey 而不是 Input.GetAxis —— 不依赖 InputManager.asset 里的轴配置，
    ///    这样即使用户没提交 ProjectSettings/InputManager.asset 也能正常走。
    /// 2. 转向用鼠标位置差（按住右键拖动）—— 光标不锁定时依然可用，
    ///    也不需要 Mouse X / Mouse Y 轴。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动")]
        public float moveSpeed = 4.5f;
        public float sprintMultiplier = 1.8f;

        [Header("视角")]
        [Tooltip("鼠标每移动 1 像素转多少度")]
        public float lookSensitivity = 0.12f;
        public float maxPitch = 80f;
        public Transform cameraPivot;

        private CharacterController controller;
        private float pitch;
        private float verticalSpeed;
        private Vector3 lastMousePosition;
        private bool dragging;

        [Tooltip("重力加速度，负值向下")]
        public float gravity = -18f;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraPivot == null && Camera.main != null)
            {
                cameraPivot = Camera.main.transform;
            }
        }

        private void Update()
        {
            // Do not move or rotate while the user is typing in a form field.
            if (IsTypingInInputField)
            {
                dragging = false;
                return;
            }

            HandleLook();
            HandleMove();
        }

        public bool IsTypingInInputField
        {
            get
            {
                if (EventSystem.current == null) return false;
                var selected = EventSystem.current.currentSelectedGameObject;
                if (selected == null || selected.activeInHierarchy == false) return false;
                var input = selected.GetComponent<InputField>();
                return input != null && input.isFocused;
            }
        }

        private void HandleLook()
        {
            if (!Input.GetMouseButton(1))
            {
                dragging = false;
                return;
            }

            if (!dragging)
            {
                dragging = true;
                lastMousePosition = Input.mousePosition;
                return;
            }

            Vector2 delta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            float yaw = delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -maxPitch, maxPitch);

            transform.Rotate(0f, yaw, 0f, Space.Self);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void HandleMove()
        {
            float x = 0f;
            float z = 0f;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) z -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;

            Vector3 direction = (transform.right * x + transform.forward * z).normalized;
            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

            // CharacterController.Move 收的是位移不是速度，所以重力要自己积分。
            // 不加这一段的话，角色走出平台会直接悬在半空。
            if (controller.isGrounded && verticalSpeed <= 0f)
            {
                verticalSpeed = -2f; // 贴地时给一点点向下的速度，保证 isGrounded 稳定
            }
            else
            {
                verticalSpeed += gravity * Time.deltaTime;
            }

            Vector3 motion = direction * speed * Time.deltaTime;
            motion.y = verticalSpeed * Time.deltaTime;
            controller.Move(motion);
        }
    }
}
