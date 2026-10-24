using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class CompactEchoTimelineUI : MonoBehaviour
    {
        public UISmoothFill Player;
        public RecordingIndicatorUI Recording;
        public Image SegmentPrefab, Conflict;
        public TMP_Text LabelPrefab;
        public int LaneCount { get; private set; }
        public int SegmentCount => bars.Count;
        readonly List<Image> lanes = new List<Image>();
        readonly Dictionary<int, Image> bars = new Dictionary<int, Image>();
        List<EchoLaneLayout.Placement> layout = new List<EchoLaneLayout.Placement>();
        string signature;
        RecordingRejection rejection;
        float conflictUntil;
        static void Position(RectTransform rect, float start, float end, float y, float height)
        {
            rect.anchorMin = new Vector2(start, 1); rect.anchorMax = new Vector2(end, 1);
            rect.pivot = new Vector2(.5f, 1);
            rect.offsetMin = new Vector2(0, -y - height); rect.offsetMax = new Vector2(0, -y);
        }
        public void Apply(UISnapshot data)
        {
            var key = new StringBuilder();
            foreach (var e in data.Echoes) key.Append(e.Id).Append('/').Append(e.Start.ToString("R")).Append('/').Append(e.End.ToString("R")).Append(';');
            string next = key.ToString();
            if (signature != next)
            {
                layout = EchoLaneLayout.Arrange(data.Echoes); signature = next;
                LaneCount = 0;
                var live = new HashSet<int>();
                foreach (var p in layout) { LaneCount = Mathf.Max(LaneCount, p.Lane + 1); live.Add(p.Echo.Id); }
                var removed = new List<int>();
                foreach (var id in bars.Keys) if (!live.Contains(id)) removed.Add(id);
                foreach (var id in removed) { Destroy(bars[id].gameObject); bars.Remove(id); }
                while (lanes.Count < LaneCount) lanes.Add(Instantiate(SegmentPrefab, transform));
                for (int i = 0; i < lanes.Count; i++)
                {
                    lanes[i].gameObject.SetActive(i < LaneCount);
                    lanes[i].color = new Color(UITheme.Muted.r, UITheme.Muted.g, UITheme.Muted.b, .22f);
                    lanes[i].name = "Echo lane " + i;
                    Position(lanes[i].rectTransform, 0, 1, 14 + i * 16, 10);
                }
                foreach (var p in layout)
                    if (!bars.ContainsKey(p.Echo.Id))
                    {
                        var bar = Instantiate(SegmentPrefab, transform); bar.name = "Echo " + p.Echo.Id;
                        var label = Instantiate(LabelPrefab, bar.transform); label.gameObject.SetActive(true);
                        label.text = p.Echo.Id.ToString("00");
                        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                        label.rectTransform.offsetMin = new Vector2(0, -3); label.rectTransform.offsetMax = new Vector2(0, 3);
                        bars.Add(p.Echo.Id, bar);
                    }
            }
            ((RectTransform)transform).sizeDelta = new Vector2(680, 8 + LaneCount * 16);
            float duration = Mathf.Max(.001f, data.Duration);
            var current = new Dictionary<int, EchoUIData>();
            foreach (var echo in data.Echoes) current[echo.Id] = echo;
            Position((RectTransform)Player.transform, 0, 1, 0, 8);
            Position((RectTransform)Recording.Progress.transform, 0, 1, 0, 8);
            foreach (var p in layout)
            {
                var echo = current[p.Echo.Id];
                var bar = bars[p.Echo.Id];
                Position(bar.rectTransform, Mathf.Clamp01(p.Echo.Start / duration), Mathf.Clamp01(p.Echo.End / duration), 14 + p.Lane * 16, 10);
                Color color = echo.State == EchoState.SyncError ? UITheme.Error : echo.Id % 2 == 0 ? UITheme.Secondary : UITheme.Cyan;
                color.a = echo.State == EchoState.Completed ? .4f : echo.State == EchoState.Waiting ? .65f : 1;
                bar.color = color;
            }
            if (data.Rejection != null && data.Rejection != rejection)
            { rejection = data.Rejection; conflictUntil = Time.unscaledTime + 3; }
            bool conflict = rejection != null && rejection.Start.HasValue && rejection.End.HasValue && rejection.End > rejection.Start && Time.unscaledTime < conflictUntil;
            Conflict.gameObject.SetActive(conflict);
            if (conflict)
            {
                Conflict.rectTransform.anchorMin = new Vector2(Mathf.Clamp01(rejection.Start.Value / duration), 0);
                Conflict.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(rejection.End.Value / duration), 1);
                Conflict.rectTransform.offsetMin = Conflict.rectTransform.offsetMax = Vector2.zero;
                Conflict.transform.SetAsLastSibling();
            }
            Player.Marker.SetParent(transform, false);
            Player.Marker.sizeDelta = new Vector2(2, 0); Player.Marker.anchoredPosition = Vector2.zero;
            Player.Marker.gameObject.SetActive(!data.IsResearch);
            Player.Marker.SetAsLastSibling();
        }
        void Update() { if (Conflict && Conflict.gameObject.activeSelf && Time.unscaledTime >= conflictUntil) Conflict.gameObject.SetActive(false); }
    }
}
