// PlayerStaminaSliderUI.cs
// Отображение стамины игрока на UI-слайдере.

using Project.Scripts.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scripts.UI
{
    [DisallowMultipleComponent]
    public sealed class PlayerStaminaSliderUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private PlayerStamina stamina;
        [SerializeField] private Slider        slider;

        private void Awake()
        {
            slider.minValue = 0f;
            slider.maxValue = stamina.MaxStamina;
            slider.value    = stamina.CurrentStamina;
        }

        private void OnEnable()
        {
            stamina.StaminaChanged += OnStaminaChanged;
        }

        private void OnDisable()
        {
            stamina.StaminaChanged -= OnStaminaChanged;
        }

        private void OnStaminaChanged(float current, float max)
        {
            slider.maxValue = max;
            slider.value    = current;
        }
    }
}