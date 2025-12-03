// PlayerInteractionController.cs
// Луч из камеры, поиск IInteractable и взаимодействие по клавише E.

using System;
using Project.Scripts.Core;
using UnityEngine;

namespace Project.Scripts.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private new Camera             camera;
        [SerializeField] private FirstPersonController  movement;
        [SerializeField] private PlayerHealController   healController;

        [Header("Параметры взаимодействия")]
        [SerializeField] private KeyCode   interactKey     = KeyCode.E;
        [SerializeField] private float     maxDistance     = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;

        public event Action<IInteractable> InteractionTargetChanged;
        public event Action<IInteractable> Interacted;

        private IInteractable currentTarget;

        private void Update()
        {
            UpdateCurrentTarget();
            HandleInput();
        }

        private void UpdateCurrentTarget()
        {
            IInteractable newTarget = null;

            var ray = new Ray(camera.transform.position, camera.transform.forward);
            if (Physics.Raycast(
                    ray,
                    out var hit,
                    maxDistance,
                    interactionMask,
                    QueryTriggerInteraction.Collide))
            {
                newTarget = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (ReferenceEquals(newTarget, currentTarget))
                return;

            currentTarget = newTarget;
            InteractionTargetChanged?.Invoke(currentTarget);
        }

        private void HandleInput()
        {
            if (!Input.GetKeyDown(interactKey))
                return;

            if (currentTarget == null)
                return;

            if (movement != null && movement.IsDodging)
                return;

            if (healController != null && healController.IsHealing)
                return;

            currentTarget.Interact(gameObject);
            Interacted?.Invoke(currentTarget);
        }
    }
}
