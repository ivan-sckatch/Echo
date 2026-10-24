using TMPro;
using UnityEngine;
namespace EchoGame.UI
{
    public sealed class RecordingIndicatorUI : MonoBehaviour
    {
        public TMP_Text Status, Elapsed;
        public UISmoothFill Progress;
        public UIPanelMotion Motion;
        RecordingState previous;
        bool initialized, visible;
        int cycle;
        float savedUntil;
        public void Apply(UISnapshot data)
        {
            if (initialized && previous == RecordingState.Recording &&
                (data.Recording == RecordingState.Completed || data.Cycle != cycle))
                savedUntil = Time.unscaledTime + 1.8f;
            if (initialized && data.Cycle != cycle && data.Echoes.Count == 0) savedUntil = 0;
            bool recording = data.Recording == RecordingState.Recording;
            Status.text = recording ? "● ЗАПИСЬ" : "ЗАПИСЬ СОХРАНЕНА";
            Elapsed.text = recording ? $"{Mathf.Max(0, data.RecordEnd - data.RecordStart):0.0} с" : "";
            var rect = Progress.Fill.rectTransform;
            float duration = Mathf.Max(1, data.Duration);
            rect.anchorMin = new Vector2(Mathf.Clamp01(data.RecordStart / duration), 0);
            rect.anchorMax = new Vector2(Mathf.Clamp01(data.RecordEnd / duration), 1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Progress.Fill.fillAmount = 1;
            Progress.gameObject.SetActive(recording);
            bool show = data.Phase == SessionPhase.Playing && (recording || Time.unscaledTime < savedUntil);
            if (!initialized || visible != show) { Motion.Show(show); visible = show; }
            previous = data.Recording; cycle = data.Cycle; initialized = true;
        }
    }
}
