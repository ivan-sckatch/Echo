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
        SessionPhase previousPhase;
        bool initialized;
        public void Apply(UISnapshot data)
        {
            Status.text = data.Recording == RecordingState.Recording ? "● ИДЁТ ЗАПИСЬ" : data.Recording == RecordingState.Completed ? "ЗАПИСЬ ЗАВЕРШЕНА" : "R — НАЧАТЬ ЗАПИСЬ";
            Elapsed.text = $"{Mathf.Max(0, data.RecordEnd - data.RecordStart):0.0} с";
            Progress.Set((data.RecordEnd - data.RecordStart) / Mathf.Max(1, data.Duration));
            if (!initialized || previous != data.Recording || previousPhase != data.Phase)
            { Motion.Show(data.Phase == SessionPhase.Playing); previous = data.Recording; previousPhase = data.Phase; initialized = true; }
        }
    }
}
