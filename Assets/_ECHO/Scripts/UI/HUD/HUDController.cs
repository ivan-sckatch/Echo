using System.Collections;
using TMPro;
using UnityEngine;
namespace EchoGame.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        public UIPanelMotion Motion, NoticePanel;
        public TMP_Text NoticeText, CompactTime;
        public CycleTimerUI Timer;
        public EchoStatusUI Echoes;
        public RecordingIndicatorUI Recording;
        public UISmoothFill CompactTimeline;
        public CrosshairUI Crosshair;
        public InteractionPromptUI Prompt;
        Coroutine noticeRoutine;
        public void Apply(UISnapshot data)
        {
            Timer.Apply(data); Echoes.Apply(data); Recording.Apply(data);
            CompactTime.text = UITheme.Clock(data.Time) + " / " + UITheme.Clock(data.Duration);
            CompactTimeline.Fill.color = data.Recording == RecordingState.Recording ? UITheme.Amber : UITheme.Cyan;
            CompactTimeline.Set(data.Time / Mathf.Max(1, data.Duration));
        }
        public void Show(bool visible)
        { Motion.Show(visible); Crosshair.SetVisible(visible); Prompt.SetAllowed(visible); if (!visible) NoticePanel.Show(false); }
        public void SetInteraction(bool available, string text) { Crosshair.SetInteractable(available); Prompt.SetPrompt(available, text); }
        public void Notice(string text, bool error)
        {
            NoticeText.text = text; NoticeText.color = error ? UITheme.Error : UITheme.Text;
            if (noticeRoutine != null) StopCoroutine(noticeRoutine);
            noticeRoutine = StartCoroutine(ShowNotice());
        }
        IEnumerator ShowNotice()
        { NoticePanel.Show(true); yield return new WaitForSecondsRealtime(8); NoticePanel.Show(false); noticeRoutine = null; }
    }
}
