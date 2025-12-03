// PlayerHealController.cs
// Лечение игрока по нажатию F: анимация + восстановление здоровья.

using System;
using UnityEngine;

namespace Project.Scripts.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealController : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private PlayerStats           stats;
        [SerializeField] private FirstPersonController movement;
        [SerializeField] private PlayerCombatController combat;
        [SerializeField] private Animator              animator;

        [Header("Настройки лечения")]
        [SerializeField] private KeyCode healKey         = KeyCode.F;
        [SerializeField] private float   healAmount      = 25f;
        [SerializeField] private float   healApplyDelay  = 0.4f;
        [SerializeField] private float   healDuration    = 1.0f;
        [SerializeField] private float   healCooldown    = 3.0f;

        [Header("Анимация")]
        [SerializeField] private string healTriggerName = "Heal";

        private bool  isHealing;
        private bool  healApplied;
        private float healTimer;
        private float cooldownTimer;
        private int   healTriggerHash;

        public bool IsHealing => isHealing;

        public event Action HealStarted;
        public event Action HealApplied;
        public event Action HealFinished;

        private void Awake()
        {
            if (animator && !string.IsNullOrWhiteSpace(healTriggerName))
                healTriggerHash = Animator.StringToHash(healTriggerName);
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
                cooldownTimer -= Time.deltaTime;

            UpdateHealState();
            HandleInput();
        }

        private void HandleInput()
        {
            if (!Input.GetKeyDown(healKey))
                return;

            if (isHealing)
                return;

            if (cooldownTimer > 0f)
                return;

            if (movement != null && movement.IsDodging)
                return;

            if (combat != null && combat.IsAttacking)
                return;

            StartHeal();
        }

        private void StartHeal()
        {
            isHealing   = true;
            healApplied = false;
            healTimer   = 0f;

            cooldownTimer = healCooldown;

            if (animator && healTriggerHash != 0)
                animator.SetTrigger(healTriggerHash);

            HealStarted?.Invoke();
        }

        private void UpdateHealState()
        {
            if (!isHealing)
                return;

            healTimer += Time.deltaTime;

            if (!healApplied && healTimer >= healApplyDelay)
                ApplyHeal();

            if (healTimer >= healDuration)
            {
                isHealing = false;
                HealFinished?.Invoke();
            }
        }

        private void ApplyHeal()
        {
            healApplied = true;

            if (healAmount > 0f)
                stats.Heal(healAmount);

            HealApplied?.Invoke();
        }
    }
}
