// FirstPersonController.cs
// Базовое управление от первого лица + покачивание рук,
// интеграция со стаминой, жгучестью и доджем.

using System;
using UnityEngine;

namespace Project.Scripts.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private new Camera              camera;
        [SerializeField] private CharacterController     controller;
        [SerializeField] private PlayerStamina           stamina;
        [SerializeField] private PlayerBurn              burn;
        [SerializeField] private PlayerCombatController  combatController;

        [Header("Руки (FPS-рег)")]
        [SerializeField, Tooltip("Корневой объект рук, потомок камеры (например, Hands).")]
        private Transform handsRoot;
        [SerializeField, Tooltip("Кость ключицы левой руки (clavicle_l).")]
        private Transform leftClavicle;
        [SerializeField, Tooltip("Кость ключицы правой руки (clavicle_r).")]
        private Transform rightClavicle;

        [Header("Движение")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 3.5f;
        [SerializeField, Min(0.1f)] private float runSpeed  = 6.0f;
        [SerializeField]            private float jumpForce  = 5.0f;
        [SerializeField]            private float gravity    = -9.81f;

        [Header("Вращение камеры")]
        [SerializeField] private float mouseSensitivity = 100f;
        [SerializeField] private float verticalLimit    = 80f;

        [Header("Покачивание рук")]
        [SerializeField, Tooltip("Амплитуда вертикального покачивания всего рига рук.")]
        private float bobAmplitude   = 0.05f;
        [SerializeField, Tooltip("Частота покачивания при ходьбе/беге.")]
        private float bobFrequency   = 8.0f;
        [SerializeField, Tooltip("Скорость возврата рук в исходное положение.")]
        private float bobReturnSpeed = 10f;
        [SerializeField, Tooltip("Минимальное значение ввода движения, чтобы считать, что персонаж идёт.")]
        private float moveThreshold  = 0.1f;
        [SerializeField, Tooltip("Угол покачивания ключиц влево/вправо (в градусах).")]
        private float clavicleSwingAngle = 4f;

        [Header("Стамина")]
        [SerializeField, Tooltip("Расход стамины за секунду при беге.")]
        private float runStaminaCostPerSecond = 15f;
        [SerializeField, Tooltip("Разовый расход стамины при прыжке.")]
        private float jumpStaminaCost = 10f;
        [SerializeField, Tooltip("Множитель скорости при нулевой стамине.")]
        [Range(0.1f, 1f)]
        private float noStaminaSpeedMultiplier = 0.4f;

        [Header("Додж (рывок)")]
        [SerializeField, Tooltip("Дистанция рывка в метрах.")]
        private float dodgeDistance = 4f;
        [SerializeField, Tooltip("Длительность рывка, сек.")]
        private float dodgeDuration = 0.2f;
        [SerializeField, Tooltip("Кулдаун рывка, сек.")]
        private float dodgeCooldown = 0.5f;
        [SerializeField, Tooltip("Разовый расход стамины на рывок.")]
        private float dodgeStaminaCost = 20f;

        [Header("Анимация движения (опционально)")]
        [SerializeField] private Animator handsAnimator;
        [SerializeField] private string   dodgeTriggerName = "Dodge";

        private float      cameraPitch;
        private float      verticalVelocity;

        private Vector3    handsRootDefaultLocalPos;
        private Quaternion leftClavicleDefaultRot;
        private Quaternion rightClavicleDefaultRot;
        private float      bobTimer;

        // Додж
        private float   dodgeTimer;
        private float   dodgeCooldownTimer;
        private Vector3 dodgeVelocity;

        private int dodgeTriggerHash;

        public bool IsDodging { get; private set; }

        public event Action DodgeStarted;
        public event Action DodgeEnded;

        private void Awake()
        {
            if (handsRoot)
                handsRootDefaultLocalPos = handsRoot.localPosition;

            if (leftClavicle)
                leftClavicleDefaultRot = leftClavicle.localRotation;

            if (rightClavicle)
                rightClavicleDefaultRot = rightClavicle.localRotation;

            if (handsAnimator && !string.IsNullOrWhiteSpace(dodgeTriggerName))
                dodgeTriggerHash = Animator.StringToHash(dodgeTriggerName);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
            HandleHandBob();
        }

        #region Вращение камеры

        private void HandleLook()
        {
            var mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            var mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            transform.Rotate(Vector3.up * mouseX);

            cameraPitch -= mouseY;
            cameraPitch  = Mathf.Clamp(cameraPitch, -verticalLimit, verticalLimit);

            camera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        #endregion

        #region Движение + стамина + жгучесть + додж

        private void HandleMovement()
        {
            var inputX = Input.GetAxisRaw("Horizontal");
            var inputZ = Input.GetAxisRaw("Vertical");

            var inputDir = new Vector3(inputX, 0f, inputZ);
            inputDir     = Vector3.ClampMagnitude(inputDir, 1f);

            var hasInput        = inputDir.sqrMagnitude > 0.0001f;
            var staminaDepleted = stamina.IsDepleted;

            if (dodgeCooldownTimer > 0f)
                dodgeCooldownTimer -= Time.deltaTime;

            var dodgePressed = Input.GetKeyDown(KeyCode.LeftShift);

            if (dodgePressed &&
                !IsDodging &&
                controller.isGrounded &&
                hasInput &&
                dodgeCooldownTimer <= 0f &&
                !staminaDepleted &&
                stamina.HasEnough(dodgeStaminaCost) &&
                stamina.TrySpend(dodgeStaminaCost))
            {
                var worldDir = transform.TransformDirection(inputDir).normalized;
                StartDodge(worldDir);
                return;
            }

            if (IsDodging)
            {
                UpdateDodge();
                UpdateStaminaRegenMode(false, false);
                return;
            }

            var runRequested =
                Input.GetKey(KeyCode.LeftShift) &&
                hasInput &&
                !staminaDepleted;

            var baseSpeed = walkSpeed;

            if (runRequested)
                baseSpeed = runSpeed;
            else if (staminaDepleted)
                baseSpeed = walkSpeed * noStaminaSpeedMultiplier;

            baseSpeed *= burn.SpeedMultiplier;

            var moveDir = transform.TransformDirection(inputDir) * baseSpeed;

            if (runRequested && runStaminaCostPerSecond > 0f)
            {
                var cost = runStaminaCostPerSecond * Time.deltaTime;
                if (!stamina.TrySpend(cost))
                {
                    staminaDepleted = stamina.IsDepleted;
                    baseSpeed = staminaDepleted
                        ? walkSpeed * noStaminaSpeedMultiplier
                        : walkSpeed;

                    baseSpeed *= burn.SpeedMultiplier;
                    moveDir    = transform.TransformDirection(inputDir) * baseSpeed;
                    runRequested = false;
                }
            }

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f)
                    verticalVelocity = -2f;

                if (Input.GetButtonDown("Jump") && !staminaDepleted)
                {
                    var canJump = jumpStaminaCost <= 0f || stamina.TrySpend(jumpStaminaCost);
                    if (canJump)
                        verticalVelocity = jumpForce;
                }
            }

            verticalVelocity += gravity * Time.deltaTime;

            var velocity = moveDir;
            velocity.y   = verticalVelocity;

            controller.Move(velocity * Time.deltaTime);

            UpdateStaminaRegenMode(hasInput, runRequested);
        }

        private void StartDodge(Vector3 worldDirection)
        {
            IsDodging          = true;
            dodgeTimer         = dodgeDuration;
            dodgeCooldownTimer = dodgeCooldown;

            var dodgeSpeed = dodgeDistance / Mathf.Max(dodgeDuration, 0.01f);
            dodgeVelocity  = worldDirection * dodgeSpeed;

            verticalVelocity = 0f;

            if (handsAnimator && dodgeTriggerHash != 0)
                handsAnimator.SetTrigger(dodgeTriggerName);

            combatController?.CancelCurrentAttack();

            DodgeStarted?.Invoke();
        }

        private void UpdateDodge()
        {
            dodgeTimer -= Time.deltaTime;
            if (dodgeTimer <= 0f)
            {
                IsDodging   = false;
                dodgeVelocity = Vector3.zero;
                DodgeEnded?.Invoke();
            }

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            verticalVelocity += gravity * Time.deltaTime;

            var velocity = dodgeVelocity;
            velocity.y   = verticalVelocity;

            controller.Move(velocity * Time.deltaTime);
        }

        private void UpdateStaminaRegenMode(bool hasInput, bool isRunning)
        {
            if (!controller.isGrounded)
            {
                stamina.SetRegenMode(PlayerStamina.RegenMode.None);
                return;
            }

            if (isRunning || IsDodging)
            {
                stamina.SetRegenMode(PlayerStamina.RegenMode.None);
            }
            else if (hasInput)
            {
                stamina.SetRegenMode(PlayerStamina.RegenMode.Walk);
            }
            else
            {
                stamina.SetRegenMode(PlayerStamina.RegenMode.Idle);
            }
        }

        #endregion

        #region Покачивание рук

        private void HandleHandBob()
        {
            if (!handsRoot && !leftClavicle && !rightClavicle)
                return;

            var inputX = Input.GetAxisRaw("Horizontal");
            var inputZ = Input.GetAxisRaw("Vertical");

            var isMoving =
                controller.isGrounded &&
                new Vector2(inputX, inputZ).sqrMagnitude > moveThreshold * moveThreshold;

            if (IsDodging)
                isMoving = false;

            if (isMoving)
            {
                bobTimer += Time.deltaTime * bobFrequency;

                if (handsRoot)
                {
                    var bobOffsetY = Mathf.Sin(bobTimer) * bobAmplitude;
                    var targetPos  = handsRootDefaultLocalPos + new Vector3(0f, bobOffsetY, 0f);

                    handsRoot.localPosition = Vector3.Lerp(
                        handsRoot.localPosition,
                        targetPos,
                        Time.deltaTime * bobReturnSpeed
                    );
                }

                var swingLeft  = Mathf.Sin(bobTimer) * clavicleSwingAngle;
                var swingRight = Mathf.Sin(bobTimer + Mathf.PI) * clavicleSwingAngle;

                if (leftClavicle)
                {
                    var swingRot = Quaternion.Euler(0f, 0f, swingLeft);
                    leftClavicle.localRotation =
                        Quaternion.Slerp(
                            leftClavicle.localRotation,
                            leftClavicleDefaultRot * swingRot,
                            Time.deltaTime * bobReturnSpeed
                        );
                }

                if (rightClavicle)
                {
                    var swingRot = Quaternion.Euler(0f, 0f, -swingRight);
                    rightClavicle.localRotation =
                        Quaternion.Slerp(
                            rightClavicle.localRotation,
                            rightClavicleDefaultRot * swingRot,
                            Time.deltaTime * bobReturnSpeed
                        );
                }
            }
            else
            {
                bobTimer = 0f;

                if (handsRoot)
                {
                    handsRoot.localPosition = Vector3.Lerp(
                        handsRoot.localPosition,
                        handsRootDefaultLocalPos,
                        Time.deltaTime * bobReturnSpeed
                    );
                }

                if (leftClavicle)
                {
                    leftClavicle.localRotation = Quaternion.Slerp(
                        leftClavicle.localRotation,
                        leftClavicleDefaultRot,
                        Time.deltaTime * bobReturnSpeed
                    );
                }

                if (rightClavicle)
                {
                    rightClavicle.localRotation = Quaternion.Slerp(
                        rightClavicle.localRotation,
                        rightClavicleDefaultRot,
                        Time.deltaTime * bobReturnSpeed
                    );
                }
            }
        }

        #endregion
    }
}
