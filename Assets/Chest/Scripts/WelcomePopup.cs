using System.Collections;
using UnityEngine;

namespace Unisza
{
    /// <summary>
    /// Fades a panel in, holds it, then fades it out. Used for the chest welcome message.
    /// </summary>
    public class WelcomePopup : MonoBehaviour
    {
        [Header("Refs")]
        public CanvasGroup group;
        public RectTransform panel;

        [Header("Content")]
        [TextArea] public string message = "Welcome to Unisza";

        [Header("Timing")]
        public float fadeInDuration = 0.3f;
        public float holdDuration = 2.5f;
        public float fadeOutDuration = 0.45f;
        [Tooltip("Panel scales up slightly while fading in.")]
        public bool popScale = true;
        public float scaleFrom = 0.85f;

        Coroutine routine;

        void Awake()
        {
            if (!group) group = GetComponent<CanvasGroup>();
            SetAlpha(0f);
        }

        public void Show()
        {
            Show(message);
        }

        public void Show(string text)
        {
            if (!group) return;

            foreach (var label in GetComponentsInChildren<UnityEngine.UI.Text>(true))
                label.text = text;

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Play(text));
        }

        IEnumerator Play(string text)
        {
            yield return FadeTo(1f, fadeInDuration, true);
            yield return new WaitForSeconds(holdDuration);
            yield return FadeTo(0f, fadeOutDuration, false);

            routine = null;
        }

        IEnumerator FadeTo(float target, float duration, bool scaleIn)
        {
            float startAlpha = group.alpha;
            float startScale = panel != null ? panel.localScale.x : 1f;
            float endScale = scaleIn ? 1f : (popScale ? scaleFrom : 1f);

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

                group.alpha = Mathf.Lerp(startAlpha, target, k);

                if (panel != null)
                {
                    float s = Mathf.Lerp(startScale, endScale, k);
                    panel.localScale = new Vector3(s, s, 1f);
                }

                yield return null;
            }

            group.alpha = target;
            if (panel != null) panel.localScale = new Vector3(endScale, endScale, 1f);
        }

        void SetAlpha(float a)
        {
            if (!group) return;

            group.alpha = a;
            group.blocksRaycasts = a > 0.01f;
            group.interactable = a > 0.01f;

            if (panel != null && popScale)
            {
                float s = a > 0.01f ? 1f : scaleFrom;
                panel.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}
