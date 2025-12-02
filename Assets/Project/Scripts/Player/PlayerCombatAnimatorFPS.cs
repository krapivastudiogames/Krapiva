// PlayerCombatAnimatorFPS.cs
// Связывает события PlayerCombatController с Animator рук (FPS-рег).
//
// Папка: Project/Scripts/Player
// Namespace: Project.Scripts.Player

using UnityEngine;

namespace Project.Scripts.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCombatController))]
    public sealed class PlayerCombatAnimatorFPS : MonoBehaviour
    {
        [Header("Аниматор рук")]
        [SerializeField, Tooltip("Animator, который управляет анимациями рук/оружия.")]
        private Animator _animator;

        [Header("Параметры аниматора")]
        [SerializeField, Tooltip("Trigger для быстрой атаки.")]
        private string _lightAttackTriggerName = "Attack_Light";

        [SerializeField, Tooltip("Trigger для тяжёлой атаки.")]
        private string _heavyAttackTriggerName = "Attack_Heavy";

        private PlayerCombatController _combat;

        private int _lightAttackTriggerHash;
        private int _heavyAttackTriggerHash;

        private void Awake()
        {
            _combat = GetComponent<PlayerCombatController>();

            if (_animator == null)
            {
                Debug.LogError("PlayerCombatAnimatorFPS: Animator не назначен.");
                enabled = false;
                return;
            }

            _lightAttackTriggerHash = Animator.StringToHash(_lightAttackTriggerName);
            _heavyAttackTriggerHash = Animator.StringToHash(_heavyAttackTriggerName);
        }

        private void OnEnable()
        {
            if (_combat == null)
                return;

            _combat.LightAttackStarted  += OnLightAttackStarted;
            _combat.HeavyAttackStarted  += OnHeavyAttackStarted;
        }

        private void OnDisable()
        {
            if (_combat == null)
                return;

            _combat.LightAttackStarted  -= OnLightAttackStarted;
            _combat.HeavyAttackStarted  -= OnHeavyAttackStarted;
        }

        private void OnLightAttackStarted()
        {
            _animator.ResetTrigger(_heavyAttackTriggerHash);
            _animator.SetTrigger(_lightAttackTriggerHash);
        }

        private void OnHeavyAttackStarted()
        {
            _animator.ResetTrigger(_lightAttackTriggerHash);
            _animator.SetTrigger(_heavyAttackTriggerHash);
        }
    }
}
