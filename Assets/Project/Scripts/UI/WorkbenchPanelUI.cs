// WorkbenchPanelUI.cs
// Панель верстака: плавное появление/исчезновение через CanvasGroup,
// блокировка управления игроком, переключение курсора.

using System.Collections;
using Project.Scripts.Player;
using UnityEngine;

namespace Project.Scripts.UI
{
    [DisallowMultipleComponent]
    public sealed class WorkbenchPanelUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private CanvasGroup                canvasGroup;
        [SerializeField] private FirstPersonController      playerController;
        [SerializeField] private PlayerInteractionController interactionController;

        [Header("Управление")]
        [SerializeField] private float  fadeDuration = 0.25f;
        [SerializeField] private KeyCode closeKey    = KeyCode.Escape;

        public bool IsOpen { get; private set; }
        private bool isAnimating;

        private void Awake()
        {
            canvasGroup.alpha          = 0f;
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            if (Input.GetKeyDown(closeKey))
                Close();
        }

        public void Open()
        {
            if (IsOpen || isAnimating)
                return;

            IsOpen = true;

            playerController.enabled      = false;
            interactionController.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            StartCoroutine(FadeRoutine(1f));
        }

        private void Close()
        {
            if (!IsOpen || isAnimating)
                return;

            IsOpen = false;

            StartCoroutine(FadeRoutine(0f));
        }

        private IEnumerator FadeRoutine(float targetAlpha)
        {
            isAnimating = true;

            var startAlpha = canvasGroup.alpha;
            var t          = 0f;

            if (targetAlpha > 0f)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable   = true;
            }

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;

            if (targetAlpha == 0f)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable   = false;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;

                playerController.enabled      = true;
                interactionController.enabled = true;
            }

            isAnimating = false;
        }
    }
}
