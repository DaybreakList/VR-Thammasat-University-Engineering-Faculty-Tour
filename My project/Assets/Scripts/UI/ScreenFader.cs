using System;
using System.Collections;
using UnityEngine;

namespace VRTSE.UI
{
    public sealed class ScreenFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.4f;
        [SerializeField, Min(0f)] private float blackHoldDuration = 0.12f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.4f;

        private Coroutine fadeRoutine;

        public bool IsFading => fadeRoutine != null;

        private void Awake()
        {
            SetAlpha(0f);
        }

        public void ConfigurePrototype(CanvasGroup canvasGroup)
        {
            fadeCanvasGroup = canvasGroup;
        }

        public bool TryFadeThrough(Action whileBlack, Action whenComplete = null)
        {
            if (fadeCanvasGroup == null)
            {
                Debug.LogError("[VR TSE Fade] Fade CanvasGroup is not configured.", this);
                return false;
            }

            if (fadeRoutine != null)
                return false;

            fadeRoutine = StartCoroutine(FadeRoutine(whileBlack, whenComplete));
            return true;
        }

        private IEnumerator FadeRoutine(Action whileBlack, Action whenComplete)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            yield return FadeAlpha(0f, 1f, fadeOutDuration);

            try
            {
                whileBlack?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (blackHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(blackHoldDuration);

            yield return FadeAlpha(1f, 0f, fadeInDuration);
            fadeCanvasGroup.blocksRaycasts = false;
            fadeRoutine = null;
            whenComplete?.Invoke();
        }

        private IEnumerator FadeAlpha(float from, float to, float duration)
        {
            SetAlpha(from);

            if (duration <= 0f)
            {
                SetAlpha(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            if (fadeCanvasGroup == null)
                return;

            fadeCanvasGroup.alpha = alpha;
            fadeCanvasGroup.interactable = false;
            if (alpha <= 0f)
                fadeCanvasGroup.blocksRaycasts = false;
        }
    }
}
