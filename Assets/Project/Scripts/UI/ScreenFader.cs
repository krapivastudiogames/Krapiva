// ScreenFader.cs
// Плавное затемнение/осветление экрана через CanvasGroup.

using System.Collections;
using UnityEngine;

namespace Project.Scripts.UI
{
    [DisallowMultipleComponent]
    public sealed class ScreenFader : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Настройки по умолчанию")]
        [SerializeField, Min(0.01f)]
        private float defaultFadeDuration = 0.4f;

        public bool IsVisible => canvasGroup.alpha > 0.001f;

        private void Awake()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;
        }

        public IEnumerator FadeOut(float duration = -1f)
        {
            var time     = 0f;
            var fadeTime = duration > 0f ? duration : defaultFadeDuration;

            while (time < fadeTime)
            {
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / fadeTime);
                canvasGroup.alpha = t;
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        public IEnumerator FadeIn(float duration = -1f)
        {
            var time     = 0f;
            var fadeTime = duration > 0f ? duration : defaultFadeDuration;

            while (time < fadeTime)
            {
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / fadeTime);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }
    }
}