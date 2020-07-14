using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

namespace UniVoxel.GamePlay
{
    [RequireComponent(typeof(PlayerCore), typeof(CharacterController))]
    public class FirstPersonPlayerMovement : MonoBehaviour
    {
        PlayerCore _playerCore;
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

        [SerializeField]
        bool _isMoveable = true;
        public bool IsMoveable { get => _isMoveable && _playerCore.IsInitialized; set => _isMoveable = value; }

        protected Vector2 _movementInput;
        protected Vector2 _rotationInput;
        protected bool _jumpInput;

        protected virtual void Awake()
        {
            _playerCore = GetComponent<PlayerCore>();
            _characterController = GetComponent<CharacterController>();
        }

        protected virtual void Start()
        {
            _playerCamera = Camera.main;

            if (_hideCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            _playerCore.IsInitializedRP
                   .Where(initialized => initialized)
                   .FirstOrDefault()
                   .Subscribe(_ =>
                   {
                       _characterController.height = _playerCore.Sizes.y;
                       _characterController.radius = (_playerCore.Sizes.x + _playerCore.Sizes.z) / 2f / 2f;
                   });
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            UpdateInput();

            IsGrounded = CheckIfGounded();

            UpdateRotation();

            if (IsMoveable)
            {
                UpdatePlayerMovement(Time.deltaTime);
            }
        }

        protected virtual void UpdateInput()
        {
            _movementInput.x = Input.GetAxis("Horizontal");
            _movementInput.y = Input.GetAxis("Vertical");

            _rotationInput.x = Input.GetAxis("Mouse X") * _mouseSensitivityX;
            _rotationInput.y = Input.GetAxis("Mouse Y") * _mouseSensitivityY;

            _jumpInput = Input.GetButtonDown("Jump");
        }

        protected virtual void UpdateRotation()
        {
            var horizontalRotation = transform.localRotation * Quaternion.Euler(0f, _rotationInput.x, 0f);
            var verticalRotation = _playerCamera.transform.localRotation * Quaternion.Euler(-_rotationInput.y, 0f, 0f);

            verticalRotation = ClampRotationAroundXAxis(verticalRotation);

            transform.localRotation = horizontalRotation;
            _playerCamera.transform.localRotation = verticalRotation;
        }

        protected virtual void UpdatePlayerMovement(float deltaTime)
        {

            if (IsGrounded && Velocity.y < 0f)
            {
                Velocity = new Vector3(0, -2f, 0);
            }

            var move = CalculateMovementFromInput(deltaTime);
            move += ApplyVelocity(deltaTime);

            _characterController.Move(move);
        }

        protected virtual bool CheckIfGounded()
        {
            return Physics.CheckSphere(PlayerBottom, _characterController.radius / 2f, _groundMask);
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

        protected virtual Vector3 CalculateMovementFromInput(float deltaTime)
        {
            var move = transform.right * _movementInput.x + transform.forward * _movementInput.y;

            return move.normalized * _moveSpeed * deltaTime;
        }

        protected virtual Vector3 ApplyVelocity(float deltaTime)
        {
            if (_jumpInput && IsGrounded)
            {
                Jump();
            }

            ApplyGravity(deltaTime);

            // s = v0 * t + 1/2 * a * t^2 so the value given should be multiplied by deltaTime again
            return Velocity * deltaTime;
        }

        protected virtual void ApplyGravity(float deltaTime)
        {
            Velocity += Physics.gravity * deltaTime;
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
