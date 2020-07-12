using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniVoxel.GamePlay
{
    [RequireComponent(typeof(PlayerCore), typeof(CharacterController))]
    public class FirstPersonPlayerMovement : MonoBehaviour
    {
        PlayerCore _player;
        CharacterController _characterController;

        [SerializeField]
        bool _hideCursor = true;

        [SerializeField]
        float _mouseSensitivityX = 100f;

        [SerializeField]
        float _mouseSensitivityY = 100f;

        [SerializeField]
        float _moveSpeed = 12f;

        [SerializeField]
        float _jumpHeight = 3f;

        [SerializeField]
        LayerMask _groundMask;

        float _groundDistance = 0.4f;

        public Vector3 PlayerBottom
        {
            get
            {
                return transform.position + new Vector3(0f, -_characterController.height / 2f, 0f);
            }
        }

        Camera _playerCamera;

        public Vector3 Velocity { get; protected set; }

        public bool IsGrounded { get; protected set; }
        protected virtual void Awake()
        {
            _player = GetComponent<PlayerCore>();
            _characterController = GetComponent<CharacterController>();

        }

        protected virtual void Start()
        {
            _playerCamera = Camera.main;

            if (_hideCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            IsGrounded = CheckIfGounded();

            if (IsGrounded && Velocity.y < 0f)
            {
                Velocity = new Vector3(0, -2f, 0);
            }

            var mouseX = Input.GetAxis("Mouse X") * _mouseSensitivityX * Time.deltaTime;
            var mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivityY * Time.deltaTime;

            UpdateRotation(mouseX, mouseY);

            var horizontal = Input.GetAxis("Horizontal");
            var vertical = Input.GetAxis("Vertical");

            Move(horizontal, vertical);

            ApplyVelocity();
        }

        protected virtual bool CheckIfGounded()
        {
            return Physics.CheckSphere(PlayerBottom, _groundDistance, _groundMask);
        }

        protected virtual void UpdateRotation(float moveX, float moveY)
        {
            var horizontalRotation = transform.localRotation * Quaternion.Euler(0f, moveX, 0f);
            var verticalRotation = _playerCamera.transform.localRotation * Quaternion.Euler(-moveY, 0f, 0f);

            ClampRotationAroundXAxis(verticalRotation);

            transform.localRotation = horizontalRotation;
            _playerCamera.transform.localRotation = verticalRotation;
        }

        protected Quaternion ClampRotationAroundXAxis(Quaternion q)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

            angleX = Mathf.Clamp(angleX, -90f, 90f);

            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            return q;
        }

        protected virtual void Move(float rightMove, float forwardMove)
        {
            var move = transform.right * rightMove + transform.forward * forwardMove;

            _characterController.Move(move.normalized * _moveSpeed * Time.deltaTime);
        }

        protected virtual void ApplyVelocity()
        {
            if (Input.GetButtonDown("Jump") && IsGrounded)
            {
                Jump();
            }

            ApplyGravity();

            // s = v0 * t + 1/2 * a * t^2 so the value given should be multiplied by Time.deltaTime again
            _characterController.Move(Velocity * Time.deltaTime);
        }

        protected virtual void ApplyGravity()
        {
            Velocity += Physics.gravity * Time.deltaTime;
        }

        // velocity = sqrt(jumpHeight * -2 * gravity)
        protected virtual void Jump()
        {
            var v = Velocity;
            v.y = Mathf.Sqrt(_jumpHeight * -2f * Physics.gravity.y);
            Velocity = v;
        }
    }
}
