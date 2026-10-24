using TMPro;
using UnityEngine;
namespace EchoGame.UI
{
    public sealed class RecordingIndicatorUI : MonoBehaviour
    {
        public TMP_Text Status, Elapsed;
        public UISmoothFill Progress;
        public UIPanelMotion Motion;
        bool initialized, visible;
        public void Apply(UISnapshot data)
        {
            bool recording = data.Recording == RecordingState.Recording && !data.IsResearch;
            Status.text = recording ? "● ЗАПИСЬ" : "";
            Elapsed.text = recording ? $"{Mathf.Max(0, data.RecordEnd - data.RecordStart):0.0} с" : "";
            var rect = Progress.Fill.rectTransform;
            float duration = Mathf.Max(.001f, data.Duration);
            rect.anchorMin = new Vector2(Mathf.Clamp01(data.RecordStart / duration), 0);
            rect.anchorMax = new Vector2(Mathf.Clamp01(data.RecordEnd / duration), 1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Progress.Fill.fillAmount = 1;
            Progress.gameObject.SetActive(recording);
            bool show = data.Phase == SessionPhase.Playing && recording;
            if (!initialized || visible != show) { Motion.Show(show); visible = show; initialized = true; }
        }
    }
}
