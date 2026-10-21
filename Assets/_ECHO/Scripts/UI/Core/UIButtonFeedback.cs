using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Coroutine motion;
        public void OnPointerDown(PointerEventData e) { if (GetComponent<Selectable>().IsInteractable()) Animate(.97f); }
        public void OnPointerUp(PointerEventData e) => Animate(1);
        public void OnPointerExit(PointerEventData e) => Animate(1);
        void Animate(float scale)
        {
            if (motion != null) StopCoroutine(motion);
            if (!UISettingsManager.Current.Animations) { transform.localScale = Vector3.one; return; }
            motion = StartCoroutine(Transition(scale));
        }
        IEnumerator Transition(float scale)
        {
            Vector3 initial = transform.localScale;
            for (float t = 0; t < .08f; t += Time.unscaledDeltaTime)
            { transform.localScale = Vector3.Lerp(initial, Vector3.one * scale, t / .08f); yield return null; }
            transform.localScale = Vector3.one * scale;
        }
        void OnDisable() { transform.localScale = Vector3.one; }
    }
}
