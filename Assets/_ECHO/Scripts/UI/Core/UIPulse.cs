using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class UIPulse : MonoBehaviour
    {
        Coroutine pulse;
        public void Flash(Graphic graphic, Color color)
        {
            if (pulse != null) StopCoroutine(pulse);
            if (!UISettingsManager.Current.Animations) { graphic.color = color; return; }
            pulse = StartCoroutine(Animate(graphic, color));
        }
        IEnumerator Animate(Graphic graphic, Color color)
        {
            for (float t = 0; t < .3f; t += Time.unscaledDeltaTime)
            { if (!graphic) yield break; graphic.color = Color.Lerp(Color.white, color, t / .3f); yield return null; }
            if (graphic) graphic.color = color; pulse = null;
        }
    }
}
