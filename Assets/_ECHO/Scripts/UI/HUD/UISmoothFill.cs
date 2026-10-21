using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class UISmoothFill : MonoBehaviour
    {
        public Image Fill;
        public RectTransform Marker;
        Coroutine motion;
        public void Set(float value)
        {
            value = Mathf.Clamp01(value);
            if (motion != null) StopCoroutine(motion);
            if (!isActiveAndEnabled || !UISettingsManager.Current.Animations || value < Fill.fillAmount)
            { Draw(value); return; }
            motion = StartCoroutine(Tween(value));
        }
        void Draw(float value)
        {
            Fill.fillAmount = value;
            if (Marker) { Marker.anchorMin = new Vector2(value, 0); Marker.anchorMax = new Vector2(value, 1); }
        }
        IEnumerator Tween(float target)
        {
            float start = Fill.fillAmount;
            for (float t = 0; t < .1f; t += Time.unscaledDeltaTime) { Draw(Mathf.Lerp(start, target, t / .1f)); yield return null; }
            Draw(target); motion = null;
        }
    }
}
