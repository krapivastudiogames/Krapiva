// PlayerHealthSliderUI.cs
// Отображение здоровья игрока на UI-слайдере.

using Project.Scripts.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.UI
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealthSliderUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private PlayerStats stats;
        [SerializeField] private Slider      slider;

        private void Awake()
        {
            slider.minValue = 0f;
            slider.maxValue = stats.MaxHealth;
            slider.value    = stats.CurrentHealth;
        }

        private void OnEnable()
        {
            stats.HealthChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            stats.HealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float current, float max)
        {
            slider.maxValue = max;
            slider.value    = current;
        }
    }
}