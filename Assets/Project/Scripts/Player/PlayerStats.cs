// PlayerStats.cs
// Базовое здоровье игрока + события изменения/смерти.

using System;
using UnityEngine;

namespace Project.Scripts.Player
{
    [DisallowMultipleComponent]
    public class PlayerStats : MonoBehaviour
    {
        [Header("Здоровье")]
        [SerializeField, Min(1f)]
        private float maxHealth = 100f;

        [SerializeField]
        private float currentHealth = 100f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;

        /// <summary>
        /// Текущее здоровье изменилось (current, max).
        /// </summary>
        public event Action<float, float> HealthChanged;

        /// <summary>
        /// Игрок погиб.
        /// </summary>
        public event Action Died;

        private bool _isDead;

        private void Awake()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            _isDead       = currentHealth <= 0f;
            RaiseHealthChanged();
        }

        [ContextMenu("Debug: Full Heal")]
        public void FullHeal()
        {
            if (_isDead)
                return;

            currentHealth = maxHealth;
            RaiseHealthChanged();
        }

        public void Heal(float amount)
        {
            if (_isDead || amount <= 0f)
                return;

            var newHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            if (Mathf.Approximately(newHealth, currentHealth))
                return;

            currentHealth = newHealth;
            RaiseHealthChanged();
        }

        public void TakeDamage(float amount)
        {
            if (_isDead || amount <= 0f)
                return;

            var newHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
            if (Mathf.Approximately(newHealth, currentHealth))
                return;

            currentHealth = newHealth;
            RaiseHealthChanged();

            if (!(currentHealth <= 0f) || _isDead) return;
            _isDead = true;
            Died?.Invoke();
        }

        private void RaiseHealthChanged()
        {
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
