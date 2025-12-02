// PlayerBurn.cs
// Шкала "жгучести": накапливается от крапивы, рассеивается на дистанции,
// влияет на скорость игрока.

using System;
using UnityEngine;

namespace Project.Scripts.Player
{
    [DisallowMultipleComponent]
    public class PlayerBurn : MonoBehaviour
    {
        [Header("Параметры жгучести")]
        [SerializeField, Min(1f)]
        private float maxBurn = 100f;

        [Tooltip("Скорость естественного уменьшения жгучести в секунду, когда игрок вне опасной зоны.")]
        [SerializeField, Min(0f)]
        private float burnDecayPerSecond = 20f;

        [Tooltip("Минимальный множитель скорости при максимальной жгучести.")]
        [SerializeField, Range(0.1f, 1f)]
        private float minSpeedMultiplierAtMaxBurn = 0.3f;

        [Tooltip("Сколько секунд после последнего контакта с крапивой жгучесть начинает спадать.")]
        [SerializeField, Min(0f)]
        private float decayDelay = 0.5f;

        public float MaxBurn => maxBurn;
        public float CurrentBurn => _currentBurn;

        /// <summary>
        /// Нормализованное значение жгучести 0..1.
        /// </summary>
        private float Burn01 => Mathf.Approximately(maxBurn, 0f) ? 0f : Mathf.Clamp01(_currentBurn / maxBurn);

        /// <summary>
        /// Множитель скорости движения в зависимости от жгучести.
        /// </summary>
        public float SpeedMultiplier
        {
            get
            {
                var t = Burn01; // 0 — нет замедления, 1 — максимальное замедление
                var min = minSpeedMultiplierAtMaxBurn;
                return Mathf.Lerp(1f, min, t);
            }
        }

        public event Action<float, float> BurnChanged;

        private float _currentBurn;
        private float _timeSinceLastHit;

        private void Awake()
        {
            _currentBurn = Mathf.Clamp(_currentBurn, 0f, maxBurn);
            _timeSinceLastHit = decayDelay;
            RaiseBurnChanged();
        }

        private void Update()
        {
            _timeSinceLastHit += Time.deltaTime;
            TryDecay(Time.deltaTime);
        }

        /// <summary>
        /// Накопить жгучесть (от крапивы).
        /// </summary>
        public void AddBurn(float amount)
        {
            if (amount <= 0f)
                return;

            var newBurn = Mathf.Clamp(_currentBurn + amount, 0f, maxBurn);
            if (Mathf.Approximately(newBurn, _currentBurn))
                return;

            _currentBurn = newBurn;
            _timeSinceLastHit = 0f;
            RaiseBurnChanged();
        }

        private void TryDecay(float deltaTime)
        {
            if (_timeSinceLastHit < decayDelay)
                return;

            if (_currentBurn <= 0f || burnDecayPerSecond <= 0f)
                return;

            var newBurn = Mathf.Clamp(_currentBurn - burnDecayPerSecond * deltaTime, 0f, maxBurn);
            if (Mathf.Approximately(newBurn, _currentBurn))
                return;

            _currentBurn = newBurn;
            RaiseBurnChanged();
        }

        private void RaiseBurnChanged()
        {
            BurnChanged?.Invoke(_currentBurn, maxBurn);
        }
    }
}
