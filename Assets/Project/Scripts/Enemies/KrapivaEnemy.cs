// KrapivaEnemy.cs
// Простой враг для VS: получает урон, умирает, даёт жгучесть при контакте.

using UnityEngine;
using Project.Scripts.Combat;
using Project.Scripts.Player;

namespace Project.Scripts.Enemies
{
    [DisallowMultipleComponent]
    public sealed class KrapivaEnemy : MonoBehaviour, IDamageable
    {
        [Header("Параметры")]
        [SerializeField, Min(1f)]
        private float _maxHealth = 20f;

        [SerializeField, Tooltip("Количество жгучести, которое навешивается в секунду при контакте.")]
        private float _burnPerSecond = 30f;

        [SerializeField, Tooltip("Пауза, чтобы не давать burn слишком часто.")]
        private float _burnInterval = 0.5f;

        [Header("Аудио/эффекты (опционально)")]
        [SerializeField] private ParticleSystem _hitVfx;
        [SerializeField] private ParticleSystem _deathVfx;

        private float _currentHealth;
        private float _burnTimer;

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        private void Update()
        {
            if (_burnTimer > 0f)
                _burnTimer -= Time.deltaTime;
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f)
                return;

            _currentHealth -= amount;

            if (_hitVfx != null)
                _hitVfx.Play();

            if (_currentHealth <= 0f)
                Die();
        }

        private void Die()
        {
            if (_deathVfx != null)
                Instantiate(_deathVfx, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            if (_burnPerSecond <= 0f)
                return;

            if (_burnTimer > 0f)
                return;

            var burn = other.GetComponentInParent<PlayerBurn>();
            if (burn != null)
            {
                burn.AddBurn(_burnPerSecond);
                _burnTimer = _burnInterval;
            }
        }
    }
}
