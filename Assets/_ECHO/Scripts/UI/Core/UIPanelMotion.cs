using System.Collections;
using UnityEngine;
namespace EchoGame.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIPanelMotion : MonoBehaviour
    {
        CanvasGroup group;
        Coroutine motion;
        bool targetVisible;
        void OnEnable() => UISettingsManager.Changed += OnSettings;
        void OnDisable() { UISettingsManager.Changed -= OnSettings; if (motion != null) StopCoroutine(motion); motion = null; }
        void OnSettings() { if (!UISettingsManager.Current.Animations) Show(targetVisible); }
        public void Show(bool visible)
        {
            targetVisible = visible;
            if (!group) group = GetComponent<CanvasGroup>();
            if (motion != null) { StopCoroutine(motion); motion = null; }
            group.interactable = visible; group.blocksRaycasts = visible;
            if (!UISettingsManager.Current.Animations || !isActiveAndEnabled)
            { group.alpha = visible ? 1 : 0; return; }
            motion = StartCoroutine(Fade(visible ? 1 : 0));
        }
        IEnumerator Fade(float target)
        {
            float start = group.alpha;
            for (float t = 0; t < .16f; t += Time.unscaledDeltaTime)
            { group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, t / .16f)); yield return null; }
            group.alpha = target; motion = null;
        }
    }
}
