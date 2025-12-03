// PlayerCombatController.cs
// Управляет атаками игрока (быстрая и заряженная) и нанесением урона.
// Учитывает стамину, приоритет доджа и лечения.

using System;
using Project.Scripts.Combat;
using UnityEngine;

namespace Project.Scripts.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatController : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private new Camera             camera;
        [SerializeField] private PlayerStamina          stamina;
        [SerializeField] private FirstPersonController  movement;
        [SerializeField] private PlayerHealController   healController;
        [SerializeField] private MeleeWeaponDefinition  weapon;

        [Header("Общие настройки")]
        [SerializeField] private int   maxHitCount    = 1;
        [SerializeField] private float hitRadius      = 0.2f;
        [SerializeField] private float attackRecovery = 0.1f;

        private bool  isAttacking;
        private bool  hitApplied;
        private float attackTimer;
        private float currentAttackDuration;
        private float currentHitTime;
        private float recoveryTimer;

        private bool  isCharging;
        private float chargeTimer;

        private enum AttackType
        {
            None,
            Light,
            Heavy
        }

        private AttackType currentAttackType = AttackType.None;

        #region Свойства и события

        public bool IsAttacking => isAttacking || recoveryTimer > 0f;

        public event Action LightAttackStarted;
        public event Action HeavyAttackStarted;
        public event Action AttackHit;
        public event Action AttackFinished;

        #endregion

        private void Update()
        {
            if (movement != null && movement.IsDodging)
                return;

            if (healController != null && healController.IsHealing)
                return;

            UpdateAttackState();
            HandleInput();
        }

        private void HandleInput()
        {
            if (movement != null && movement.IsDodging)
                return;

            if (isAttacking || recoveryTimer > 0f)
                return;

            if (Input.GetMouseButton(0))
            {
                isCharging = true;
                chargeTimer += Time.deltaTime;

                if (chargeTimer >= weapon.HeavyChargeTime)
                {
                    TryStartAttack(AttackType.Heavy);
                    ResetCharge();
                }

                return;
            }

            if (Input.GetMouseButtonUp(0) && isCharging)
            {
                if (!isAttacking)
                    TryStartAttack(AttackType.Light);

                ResetCharge();
            }
        }

        private void ResetCharge()
        {
            isCharging  = false;
            chargeTimer = 0f;
        }

        private void UpdateAttackState()
        {
            if (!isAttacking && recoveryTimer > 0f)
            {
                recoveryTimer -= Time.deltaTime;
                if (recoveryTimer <= 0f)
                {
                    recoveryTimer     = 0f;
                    currentAttackType = AttackType.None;
                    AttackFinished?.Invoke();
                }
            }

            if (!isAttacking)
                return;

            attackTimer += Time.deltaTime;

            if (!hitApplied && attackTimer >= currentHitTime)
            {
                hitApplied = true;
                ApplyHit();
                AttackHit?.Invoke();
            }

            if (attackTimer >= currentAttackDuration)
            {
                isAttacking  = false;
                attackTimer  = 0f;
                hitApplied   = false;
                recoveryTimer = attackRecovery;
            }
        }

        public void SetWeapon(MeleeWeaponDefinition newWeapon)
        {
            weapon = newWeapon;
        }

        public void CancelCurrentAttack()
        {
            if (!isAttacking && recoveryTimer <= 0f)
                return;

            isAttacking      = false;
            attackTimer      = 0f;
            hitApplied       = false;
            currentAttackType = AttackType.None;
            recoveryTimer    = 0f;

            AttackFinished?.Invoke();
        }

        private void TryStartAttack(AttackType type)
        {
            if (type == AttackType.None || isAttacking)
                return;

            if (movement != null && movement.IsDodging)
                return;

            if (healController != null && healController.IsHealing)
                return;

            float staminaCost;
            float duration;
            float hitTimeNorm;

            switch (type)
            {
                case AttackType.Light:
                    staminaCost = weapon.LightStaminaCost;
                    duration    = weapon.LightDuration;
                    hitTimeNorm = weapon.LightHitTimeNormalized;
                    break;

                case AttackType.Heavy:
                    staminaCost = weapon.HeavyStaminaCost;
                    duration    = weapon.HeavyDuration;
                    hitTimeNorm = weapon.HeavyHitTimeNormalized;
                    break;

                default:
                    return;
            }

            if (!stamina.HasEnough(staminaCost))
                return;

            if (!stamina.TrySpend(staminaCost))
                return;

            currentAttackType     = type;
            currentAttackDuration = duration;
            currentHitTime        = Mathf.Clamp01(hitTimeNorm) * duration;

            attackTimer = 0f;
            hitApplied  = false;
            isAttacking = true;

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
            var origin    = camera.transform.position;
            var direction = camera.transform.forward;

            var range = currentAttackType == AttackType.Heavy
                ? weapon.HeavyRange
                : weapon.LightRange;

            var damage = currentAttackType == AttackType.Heavy
                ? weapon.HeavyDamage
                : weapon.LightDamage;

            var mask = weapon.HitMask;

            var hits = Physics.SphereCastAll(
                origin,
                hitRadius,
                direction,
                range,
                mask,
                QueryTriggerInteraction.Collide
            );

            if (hits.Length == 0)
                return;

            var applied = 0;

            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (!hit.collider)
                    continue;

                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable == null)
                    continue;

                damageable.TakeDamage(damage);
                applied++;

                if (applied >= maxHitCount)
                    break;
            }
        }
    }
}
