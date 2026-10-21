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
        public ScannerHUD Scanner;
        bool interaction;
        Coroutine noticeRoutine;
        public void Apply(UISnapshot data)
        {
            Timer.Apply(data); Echoes.Apply(data); Recording.Apply(data);
            CompactTime.text = UITheme.Clock(data.Time) + " / " + UITheme.Clock(data.Duration);
            CompactTimeline.Fill.color = data.Recording == RecordingState.Recording ? UITheme.Amber : UITheme.Cyan;
            CompactTimeline.Set(data.Time / Mathf.Max(1, data.Duration));
            if (Scanner) Scanner.Apply(data.Scanner);
            Crosshair.SetInteractable(interaction || data.Scanner != null && data.Scanner.Enabled && data.Scanner.HasTarget);
            var noticeRect = (RectTransform)NoticePanel.transform;
            if (data.Scanner != null)
            {
                noticeRect.anchorMin = noticeRect.anchorMax = new Vector2(.5f, 1); noticeRect.pivot = new Vector2(.5f, 1);
                noticeRect.anchoredPosition = new Vector2(0, -38);
                noticeRect.sizeDelta = new Vector2(Mathf.Clamp(((RectTransform)Motion.transform).rect.width - 920, 460, 800), 132);
                NoticeText.fontSize = 24;
            }
            else
            {
                noticeRect.anchorMin = noticeRect.anchorMax = new Vector2(.5f, 0); noticeRect.pivot = new Vector2(.5f, 0);
                noticeRect.anchoredPosition = new Vector2(0, 234); noticeRect.sizeDelta = new Vector2(960, 94); NoticeText.fontSize = 28;
            }
        }
        public void Show(bool visible)
        { Motion.Show(visible); Crosshair.SetVisible(visible); Prompt.SetAllowed(visible); if (!visible) NoticePanel.Show(false); }
        public void SetInteraction(bool available, string text) { interaction = available; Crosshair.SetInteractable(available); Prompt.SetPrompt(available, text); }
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
