// FirstPersonController.cs
// Базовое управление от первого лица + покачивание рук при движении.
//
// Папка: Project/Scripts/Player
// Namespace: Project.Scripts.Player

using UnityEngine;

namespace Project.Scripts.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Camera   _camera;

        [Header("Руки (FPS-рег)")]
        [SerializeField, Tooltip("Корневой объект рук, который является потомком камеры (например, Hands).")]
        private Transform _handsRoot;
        [SerializeField, Tooltip("Кость ключицы левой руки (clavicle_l).")]
        private Transform _leftClavicle;
        [SerializeField, Tooltip("Кость ключицы правой руки (clavicle_r).")]
        private Transform _rightClavicle;

        [Header("Движение")]
        [SerializeField] private float _walkSpeed        = 3.5f;
        [SerializeField] private float _runSpeed         = 6.0f;
        [SerializeField] private float _jumpForce        = 5.0f;
        [SerializeField] private float _gravity          = -9.81f;

        [Header("Вращение камеры")]
        [SerializeField] private float _mouseSensitivity = 100f;
        [SerializeField] private float _verticalLimit    = 80f;

        [Header("Покачивание рук")]
        [SerializeField, Tooltip("Амплитуда вертикального покачивания всего рига рук.")]
        private float _bobAmplitude      = 0.05f;
        [SerializeField, Tooltip("Частота покачивания при ходьбе/беге.")]
        private float _bobFrequency      = 8.0f;
        [SerializeField, Tooltip("Скорость возврата рук в исходное положение.")]
        private float _bobReturnSpeed    = 10f;
        [SerializeField, Tooltip("Минимальное значение ввода движения, чтобы считать, что персонаж идёт.")]
        private float _moveThreshold     = 0.1f;
        [SerializeField, Tooltip("Угол покачивания ключиц влево/вправо (в градусах).")]
        private float _clavicleSwingAngle = 4f;

        private CharacterController _controller;
        private float               _cameraPitch;
        private float               _verticalVelocity;

        private Vector3             _handsRootDefaultLocalPos;
        private Quaternion          _leftClavicleDefaultRot;
        private Quaternion          _rightClavicleDefaultRot;
        private float               _bobTimer;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (_handsRoot != null)
                _handsRootDefaultLocalPos = _handsRoot.localPosition;

            if (_leftClavicle != null)
                _leftClavicleDefaultRot = _leftClavicle.localRotation;

            if (_rightClavicle != null)
                _rightClavicleDefaultRot = _rightClavicle.localRotation;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
            HandleHandBob();
        }

        private void HandleLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity * Time.deltaTime;

            transform.Rotate(Vector3.up * mouseX);

            _cameraPitch -= mouseY;
            _cameraPitch  = Mathf.Clamp(_cameraPitch, -_verticalLimit, _verticalLimit);

            if (_camera != null)
                _camera.transform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");

            Vector3 inputDir = new Vector3(inputX, 0f, inputZ);
            inputDir         = Vector3.ClampMagnitude(inputDir, 1f);

            bool  isRunning  = Input.GetKey(KeyCode.LeftShift);
            float speed      = isRunning ? _runSpeed : _walkSpeed;

            Vector3 moveDir  = transform.TransformDirection(inputDir) * speed;

            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f)
                    _verticalVelocity = -2f;

                if (Input.GetButtonDown("Jump"))
                    _verticalVelocity = _jumpForce;
            }

            _verticalVelocity += _gravity * Time.deltaTime;

            Vector3 velocity = moveDir;
            velocity.y       = _verticalVelocity;

            _controller.Move(velocity * Time.deltaTime);
        }

        private void HandleHandBob()
        {
            if (_handsRoot == null && _leftClavicle == null && _rightClavicle == null)
                return;

            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");

            bool isMoving =
                _controller.isGrounded &&
                new Vector2(inputX, inputZ).sqrMagnitude > _moveThreshold * _moveThreshold;

            if (isMoving)
            {
                _bobTimer += Time.deltaTime * _bobFrequency;

                // Вертикальное покачивание всего рига рук
                if (_handsRoot != null)
                {
                    float bobOffsetY = Mathf.Sin(_bobTimer) * _bobAmplitude;
                    Vector3 targetPos = _handsRootDefaultLocalPos + new Vector3(0f, bobOffsetY, 0f);

                    _handsRoot.localPosition = Vector3.Lerp(
                        _handsRoot.localPosition,
                        targetPos,
                        Time.deltaTime * _bobReturnSpeed
                    );
                }

                // Лёгкое раздельное покачивание через поворот ключиц
                float swingLeft  = Mathf.Sin(_bobTimer) * _clavicleSwingAngle;
                float swingRight = Mathf.Sin(_bobTimer + Mathf.PI) * _clavicleSwingAngle;

                if (_leftClavicle != null)
                {
                    Quaternion swingRot = Quaternion.Euler(0f, 0f, swingLeft);
                    _leftClavicle.localRotation =
                        Quaternion.Slerp(_leftClavicle.localRotation,
                                         _leftClavicleDefaultRot * swingRot,
                                         Time.deltaTime * _bobReturnSpeed);
                }

                if (_rightClavicle != null)
                {
                    Quaternion swingRot = Quaternion.Euler(0f, 0f, -swingRight);
                    _rightClavicle.localRotation =
                        Quaternion.Slerp(_rightClavicle.localRotation,
                                         _rightClavicleDefaultRot * swingRot,
                                         Time.deltaTime * _bobReturnSpeed);
                }
            }
            else
            {
                _bobTimer = 0f;

                if (_handsRoot != null)
                {
                    _handsRoot.localPosition = Vector3.Lerp(
                        _handsRoot.localPosition,
                        _handsRootDefaultLocalPos,
                        Time.deltaTime * _bobReturnSpeed
                    );
                }

                if (_leftClavicle != null)
                {
                    _leftClavicle.localRotation = Quaternion.Slerp(
                        _leftClavicle.localRotation,
                        _leftClavicleDefaultRot,
                        Time.deltaTime * _bobReturnSpeed
                    );
                }

                if (_rightClavicle != null)
                {
                    _rightClavicle.localRotation = Quaternion.Slerp(
                        _rightClavicle.localRotation,
                        _rightClavicleDefaultRot,
                        Time.deltaTime * _bobReturnSpeed
                    );
                }
            }
        }
    }
}
