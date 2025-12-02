// PlayerStamina.cs
// Стамина: расход на действия + разные режимы регенерации.
// Режим регена будет задавать контроллер движения.

using System;
using UnityEngine;

namespace Project.Scripts.Player
{
    [DisallowMultipleComponent]
    public class PlayerStamina : MonoBehaviour
    {
        public enum RegenMode
        {
            None,   // нет восстановления (бег, активное действие)
            Walk,   // медленное восстановление (ходьба)
            Idle    // быстрое восстановление (стоя, очень медленное движение)
        }

        [Header("Параметры стамины")]
        [SerializeField, Min(1f)]
        private float maxStamina = 100f;

        [SerializeField]
        private float currentStamina = 100f;

        [Header("Скорость восстановления (в секунду)")]
        [SerializeField, Min(0f)]
        private float idleRegenPerSecond = 25f;

        [SerializeField, Min(0f)]
        private float walkRegenPerSecond = 15f;

        [Tooltip("Задержка перед началом регена после траты стамины, сек.")]
        [SerializeField, Min(0f)]
        private float regenDelay = 0.3f;

        public float MaxStamina => maxStamina;
        public float CurrentStamina => currentStamina;

        public bool IsDepleted => currentStamina <= 0.01f;

        public event Action<float, float> StaminaChanged;

        private RegenMode _regenMode = RegenMode.Idle;
        private float _timeSinceLastSpend;

        private void Awake()
        {
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            _timeSinceLastSpend = regenDelay;
            RaiseStaminaChanged();
        }

        private void Update()
        {
            _timeSinceLastSpend += Time.deltaTime;
            TryRegen(Time.deltaTime);
        }

        public void SetRegenMode(RegenMode mode)
        {
            _regenMode = mode;
        }

        /// <summary>
        /// Проверка: хватает ли стамины на действие.
        /// </summary>
        public bool HasEnough(float cost)
        {
            if (cost <= 0f)
                return true;

            return currentStamina >= cost;
        }

        /// <summary>
        /// Потратить стамину. Возвращает true, если удачно.
        /// </summary>
        public bool TrySpend(float cost)
        {
            if (cost <= 0f)
                return true;

            if (currentStamina < cost)
                return false;

            currentStamina = Mathf.Clamp(currentStamina - cost, 0f, maxStamina);
            _timeSinceLastSpend = 0f;
            RaiseStaminaChanged();
            return true;
        }

        private void TryRegen(float deltaTime)
        {
            if (_timeSinceLastSpend < regenDelay)
                return;

            float regenRate = 0f;

            switch (_regenMode)
            {
                case RegenMode.Idle:
                    regenRate = idleRegenPerSecond;
                    break;
                case RegenMode.Walk:
                    regenRate = walkRegenPerSecond;
                    break;
                case RegenMode.None:
                    regenRate = 0f;
                    break;
            }

            if (regenRate <= 0f || Mathf.Approximately(currentStamina, maxStamina))
                return;

            float newStamina = Mathf.Clamp(currentStamina + regenRate * deltaTime, 0f, maxStamina);
            if (Mathf.Approximately(newStamina, currentStamina))
                return;

            currentStamina = newStamina;
            RaiseStaminaChanged();
        }

        private void RaiseStaminaChanged()
        {
            StaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }
}
