// PlayerCombatController.cs
// Управляет атаками игрока (быстрая и заряженная) и нанесением урона.
// Без анимаций: логика таймингов, стамина, Raycast-хит.
//
// Папка: Project/Scripts/Player
// Namespace: Project.Scripts.Player

using System;
using Project.Scripts.Combat;
using UnityEngine;

namespace Project.Scripts.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStamina))]
    public sealed class PlayerCombatController : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField, Tooltip("Камера от первого лица, из центра которой идёт Raycast.")]
        private Camera _camera;

        [SerializeField, Tooltip("Текущее оружие ближнего боя (палка).")]
        private MeleeWeaponDefinition _weapon;

        [Header("Общие настройки")]
        [SerializeField, Tooltip("Максимальное количество целей за один удар.")]
        private int _maxHitCount = 1;

        [SerializeField, Tooltip("Радиус 'луча' (SphereCast) для ближнего боя.")]
        private float _hitRadius = 0.2f;

        [SerializeField, Tooltip("Задержка перед тем, как снова разрешить атаку после окончания предыдущей, сек.")]
        private float _attackRecovery = 0.1f;

        private PlayerStamina _stamina;

        private bool  _isAttacking;
        private bool  _hitApplied;
        private float _attackTimer;
        private float _currentAttackDuration;
        private float _currentHitTime;
        private float _recoveryTimer;

        private bool  _isCharging;
        private float _chargeTimer;

        private enum AttackType
        {
            None,
            Light,
            Heavy
        }

        private AttackType _currentAttackType = AttackType.None;

        #region События

        /// <summary>
        /// Началась быстрая атака.
        /// </summary>
        public event Action LightAttackStarted;

        /// <summary>
        /// Началась тяжёлая (заряженная) атака.
        /// </summary>
        public event Action HeavyAttackStarted;

        /// <summary>
        /// Сработал момент удара (hit).
        /// </summary>
        public event Action AttackHit;

        /// <summary>
        /// Атака полностью завершена (включая recovery).
        /// </summary>
        public event Action AttackFinished;

        #endregion

        private void Awake()
        {
            _stamina = GetComponent<PlayerStamina>();

            if (_camera == null)
                _camera = Camera.main;
        }

        private void Update()
        {
            if (_weapon == null || _stamina == null)
                return;

            UpdateAttackState();
            HandleInput();
        }

        private void HandleInput()
        {
            // Пока атака выполняется или recovery ещё не закончился – ввод игнорируем.
            if (_isAttacking || _recoveryTimer > 0f)
                return;

            // ЛКМ удерживается – идёт заряд.
            if (Input.GetMouseButton(0))
            {
                _isCharging = true;
                _chargeTimer += Time.deltaTime;

                // Если мы превысили порог заряда – запускаем тяжёлую атаку.
                if (_chargeTimer >= _weapon.HeavyChargeTime)
                {
                    TryStartAttack(AttackType.Heavy);
                    ResetCharge();
                }

                return;
            }

            // Если ЛКМ была отпущена и был заряд, но не дошли до HeavyChargeTime – быстрая атака.
            if (Input.GetMouseButtonUp(0) && _isCharging)
            {
                // Если тяжёлая уже не запустилась – запускаем лёгкую.
                if (!_isAttacking)
                    TryStartAttack(AttackType.Light);

                ResetCharge();
            }
        }

        private void ResetCharge()
        {
            _isCharging  = false;
            _chargeTimer = 0f;
        }

        private void UpdateAttackState()
        {
            // Управление фазой recovery после атак.
            if (!_isAttacking && _recoveryTimer > 0f)
            {
                _recoveryTimer -= Time.deltaTime;
                if (_recoveryTimer <= 0f)
                {
                    _recoveryTimer      = 0f;
                    _currentAttackType  = AttackType.None;
                    AttackFinished?.Invoke();
                }
            }

            if (!_isAttacking)
                return;

            _attackTimer += Time.deltaTime;

            // Момент удара
            if (!_hitApplied && _attackTimer >= _currentHitTime)
            {
                _hitApplied = true;
                ApplyHit();
                AttackHit?.Invoke();
            }

            // Завершение анимационной части атаки
            if (_attackTimer >= _currentAttackDuration)
            {
                _isAttacking = false;
                _attackTimer = 0f;
                _hitApplied  = false;

                // Включаем recovery
                _recoveryTimer = _attackRecovery;
            }
        }
        public void SetWeapon(MeleeWeaponDefinition weapon)
        {
            _weapon = weapon;
        }


        private void TryStartAttack(AttackType type)
        {
            if (type == AttackType.None || _isAttacking)
                return;

            float staminaCost;
            float duration;
            float hitTimeNorm;

            switch (type)
            {
                case AttackType.Light:
                    staminaCost = _weapon.LightStaminaCost;
                    duration    = _weapon.LightDuration;
                    hitTimeNorm = _weapon.LightHitTimeNormalized;
                    break;

                case AttackType.Heavy:
                    staminaCost = _weapon.HeavyStaminaCost;
                    duration    = _weapon.HeavyDuration;
                    hitTimeNorm = _weapon.HeavyHitTimeNormalized;
                    break;

                default:
                    return;
            }

            // Проверка стамины
            if (!_stamina.HasEnough(staminaCost))
                return;

            if (!_stamina.TrySpend(staminaCost))
                return;

            _currentAttackType     = type;
            _currentAttackDuration = duration;
            _currentHitTime        = Mathf.Clamp01(hitTimeNorm) * duration;

            _attackTimer = 0f;
            _hitApplied  = false;
            _isAttacking = true;

            switch (type)
            {
                case AttackType.Light:
                    LightAttackStarted?.Invoke();
                    break;
                case AttackType.Heavy:
                    HeavyAttackStarted?.Invoke();
                    break;
            }
        }

        private void ApplyHit()
        {
            if (_camera == null)
                return;

            Vector3 origin    = _camera.transform.position;
            Vector3 direction = _camera.transform.forward;

            float range = _currentAttackType == AttackType.Heavy
                ? _weapon.HeavyRange
                : _weapon.LightRange;

            float damage = _currentAttackType == AttackType.Heavy
                ? _weapon.HeavyDamage
                : _weapon.LightDamage;

            var mask = _weapon.HitMask;

            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                _hitRadius,
                direction,
                range,
                mask,
                QueryTriggerInteraction.Collide
            );

            if (hits.Length == 0)
                return;

            int applied = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null)
                    continue;

                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable == null)
                    continue;

                damageable.TakeDamage(damage);
                applied++;

                if (applied >= _maxHitCount)
                    break;
            }
        }
    }
}
