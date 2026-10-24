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
        public CompactEchoTimelineUI Schedule;
        int previousSaved = -1, previousCapacity = -1;
        RecordingRejection rejection;
        public CrosshairUI Crosshair;
        public InteractionPromptUI Prompt;
        public ScannerHUD Scanner;
        static readonly System.Collections.Generic.HashSet<string> learnedHints = new System.Collections.Generic.HashSet<string>();
        bool interaction;
        Coroutine noticeRoutine;
        public void Apply(UISnapshot data)
        {
            Timer.Apply(data); Echoes.Apply(data); Recording.Apply(data);
            if (data.LimitModel == EchoLimitModel.SavedRecordingsLimit && data.Capacity > 0 && data.Echoes.Count >= data.Capacity &&
                (previousSaved < data.Capacity || previousCapacity != data.Capacity))
                Notice("Лимит записей достигнут. Следующая запись заменит старейшую.", false);
            previousSaved = data.Echoes.Count; previousCapacity = data.Capacity;
            if (data.Rejection != null && data.Rejection != rejection)
            {
                rejection = data.Rejection;
                string interval = rejection.Start.HasValue && rejection.End.HasValue ? $" {rejection.Start:0.#}–{rejection.End:0.#} с" : "";
                Notice(rejection.Reason + interval, true);
            }
            if (CompactTime) CompactTime.gameObject.SetActive(false);
            CompactTimeline.Fill.color = UITheme.Muted;
            CompactTimeline.Set(data.IsResearch ? 0 : data.Time / Mathf.Max(.001f, data.Duration));
            if (Schedule) Schedule.Apply(data);
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
                noticeRect.anchoredPosition = new Vector2(0, Schedule ? Mathf.Max(234, ((RectTransform)Schedule.transform).rect.height + 100) : 234); noticeRect.sizeDelta = new Vector2(960, 94); NoticeText.fontSize = 28;
            }
        }
        public void Show(bool visible)
        { Motion.Show(visible); Crosshair.SetVisible(visible); Prompt.SetAllowed(visible); if (!visible) NoticePanel.Show(false); }
        public void SetInteraction(bool available, string text) { interaction = available; Crosshair.SetInteractable(available); Prompt.SetPrompt(available, text); }
        public void Notice(string text, bool error)
        {
            if (!error && (text.Contains("R —") || text.Contains("Tab —") || text.Contains("ПКМ")) && !learnedHints.Add(text)) return;
            NoticeText.text = text; NoticeText.color = error ? UITheme.Error : UITheme.Text;
            if (noticeRoutine != null) StopCoroutine(noticeRoutine);
            noticeRoutine = StartCoroutine(ShowNotice());
        }
        IEnumerator ShowNotice()
        { NoticePanel.Show(true); yield return new WaitForSecondsRealtime(4); NoticePanel.Show(false); noticeRoutine = null; }
    }
}
