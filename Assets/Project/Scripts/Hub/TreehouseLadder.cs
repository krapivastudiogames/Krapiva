// TreehouseLadder.cs
// Лестница в домик на дереве:
// игрок входит в триггер, нажимает E (через IInteractable),
// экран затемняется, игрок телепортируется либо ВВЕРХ, либо ВНИЗ,
// затем экран плавно появляется.
// Направление выбирается по тому, к какой точке игрок ближе
// (bottomPoint / topPoint). После телепорта есть кулдаун,
// во время которого повторный телепорт невозможен.

using System.Collections;
using Project.Scripts.Core;
using Project.Scripts.Player;
using Project.Scripts.UI;
using UnityEngine;

namespace Project.Scripts.Hub
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class TreehouseLadder : MonoBehaviour, IInteractable
    {
        [Header("Ссылки")]
        [SerializeField, Tooltip("Точка у основания дерева (внизу).")]
        private Transform bottomPoint;

        [SerializeField, Tooltip("Точка в домике (наверху).")]
        private Transform topPoint;

        [SerializeField] private ScreenFader screenFader;

        [Header("Настройки телепорта")]
        [SerializeField, Min(0.01f), Tooltip("Длительность затемнения/проявления экрана.")]
        private float fadeDuration = 0.4f;

        [SerializeField, Min(0f), Tooltip("Кулдаун между телепортами, сек.")]
        private float teleportCooldown = 1.8f;

        [SerializeField, Tooltip("Выравнивать ли поворот игрока под точку телепорта.")]
        private bool matchRotation = true;

        private FirstPersonController currentPlayer;
        private CharacterController   currentController;

        private bool  teleportInProgress;
        private float cooldownTimer;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Update()
        {
            if (cooldownTimer > 0f)
                cooldownTimer -= Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (currentPlayer != null)
                return;

            var player = other.GetComponentInParent<FirstPersonController>();
            if (player == null)
                return;

            currentPlayer     = player;
            currentController = player.GetComponent<CharacterController>();
        }

        private void OnTriggerExit(Collider other)
        {
            if (currentPlayer == null)
                return;

            if (!other.GetComponentInParent<FirstPersonController>())
                return;

            currentPlayer     = null;
            currentController = null;
        }

        public void Interact(GameObject interactor)
        {
            if (teleportInProgress)
                return;

            if (cooldownTimer > 0f)
                return;

            if (currentPlayer == null)
                return;

            if (interactor != currentPlayer.gameObject)
                return;

            // Определяем, к какой точке игрок ближе сейчас
            var playerPos     = currentPlayer.transform.position;
            var distToBottom  = (playerPos - bottomPoint.position).sqrMagnitude;
            var distToTop     = (playerPos - topPoint.position).sqrMagnitude;

            var targetPoint = distToBottom <= distToTop ? topPoint : bottomPoint;

            var mono = (MonoBehaviour)currentPlayer;
            mono.StartCoroutine(TeleportRoutine(targetPoint));
        }

        private IEnumerator TeleportRoutine(Transform targetPoint)
        {
            teleportInProgress = true;

            var controllerWasEnabled = currentController.enabled;
            var movementWasEnabled   = currentPlayer.enabled;

            // Блокируем движение на время телепорта
            currentController.enabled = false;
            currentPlayer.enabled     = false;

            // Плавное затемнение
            yield return screenFader.FadeOut(fadeDuration);

            // Телепорт
            var playerTransform = currentPlayer.transform;
            playerTransform.position = targetPoint.position;

            if (matchRotation)
                playerTransform.rotation = targetPoint.rotation;

            // Кадр на обновление
            yield return null;

            // Плавное появление
            yield return screenFader.FadeIn(fadeDuration);

            currentController.enabled = controllerWasEnabled;
            currentPlayer.enabled     = movementWasEnabled;

            cooldownTimer      = teleportCooldown; // защита от моментального «обратного» телепорта
            teleportInProgress = false;
        }
    }
}
