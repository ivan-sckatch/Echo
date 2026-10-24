using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class TimelineUI : MonoBehaviour
    {
        public UIPanelMotion Motion;
        public TMP_Text Clock, Duration, Summary;
        public Image Conflict;
        RecordingRejection rejection;
        float conflictUntil;
        public RectTransform Content, Marker;
        public TimelineTrackUI TrackPrefab;
        readonly Dictionary<int, TimelineTrackUI> tracks = new Dictionary<int, TimelineTrackUI>();
        readonly List<float> interactions = new List<float>();
        public int InteractionCount => interactions.Count;
        public void AddInteraction(string label, float time) => interactions.Add(time);
        public void ClearInteractions() => interactions.Clear();
        TimelineTrackUI player;
        string signature;
        void Update() { if (Conflict && Conflict.gameObject.activeSelf && Time.unscaledTime >= conflictUntil) Conflict.gameObject.SetActive(false); }
        public void Apply(UISnapshot data)
        {
            string next = data.Duration.ToString("R"); foreach (var e in data.Echoes) next += "/" + e.Id;
            if (next != signature)
            {
                foreach (var track in tracks.Values) Destroy(track.gameObject); tracks.Clear();
                if (player) Destroy(player.gameObject);
                foreach (var e in data.Echoes)
                {
                    var row = Instantiate(TrackPrefab, Content);
                    row.Bind(e.Id, "ЭХО " + e.Id.ToString("00"), e.Intervals, Mathf.Max(1, data.Duration), UITheme.Cyan);
                    tracks.Add(e.Id, row);
                }
                player = Instantiate(TrackPrefab, Content);
                player.Bind(0, "ИГРОК", new[] { new ActionInterval(data.RecordStart, data.Time, "Запись", true) }, Mathf.Max(1, data.Duration), UITheme.Amber);
                signature = next; Marker.SetAsLastSibling();
            }
            foreach (var e in data.Echoes) { tracks[e.Id].SetState(e.State); tracks[e.Id].Label.text = $"ЭХО {e.Id:00}  {e.Start:0.#}–{e.End:0.#}с"; }
            player.State.text = data.Recording == RecordingState.Recording ? "● ЗАПИСЬ" : data.Echoes.Count == 0 ? "НЕТ ЗАПИСЕЙ" : "ОЖИДАНИЕ";
            player.SetPlayer(data.RecordStart, data.RecordEnd, Mathf.Max(1, data.Duration));
            var scroll = Content.GetComponentInParent<ScrollRect>();
            var panel = (RectTransform)scroll.transform.parent;
            var canvas = (RectTransform)Motion.transform;
            float rowHeight = TrackPrefab.GetComponent<LayoutElement>().preferredHeight;
            float height = Mathf.Min(canvas.rect.height * .4f, 142 + (data.Echoes.Count + 1) * (rowHeight + 8));
            panel.sizeDelta = new Vector2(Mathf.Min(1320, canvas.rect.width - 80), height);
            var scrollRect = (RectTransform)scroll.transform;
            scrollRect.anchorMin = Vector2.zero; scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(24, 44); scrollRect.offsetMax = new Vector2(-24, -92);
            player.SetInteractionTimes(interactions, Mathf.Max(1, data.Duration));
            bool research = data.IsResearch;
            Clock.text = research ? "—" : UITheme.Clock(data.Time); Duration.text = research ? "—" : UITheme.Clock(data.Duration);
            if (Summary) Summary.text = $"Сохранено: {data.Echoes.Count} · {data.LimitLabel}" + (research ? " · ИССЛЕДОВАНИЕ" : "");
            if (data.Rejection != null && rejection != data.Rejection) { rejection = data.Rejection; conflictUntil = Time.unscaledTime + 3; }
            bool conflict = rejection != null && rejection.Start.HasValue && rejection.End.HasValue && rejection.End > rejection.Start && Time.unscaledTime < conflictUntil;
            if (Conflict)
            {
                Conflict.gameObject.SetActive(conflict);
                if (conflict)
                {
                    float start = Mathf.Clamp01(rejection.Start.Value / Mathf.Max(.001f, data.Duration));
                    float end = Mathf.Clamp01(rejection.End.Value / Mathf.Max(.001f, data.Duration));
                    Conflict.rectTransform.anchorMin = new Vector2(start, 0); Conflict.rectTransform.anchorMax = new Vector2(end, 1);
                    Conflict.rectTransform.offsetMin = new Vector2(300 * (1-start) - 24 * start, 0);
                    Conflict.rectTransform.offsetMax = new Vector2(300 * (1-end) - 24 * end, 0);
                    Conflict.transform.SetAsLastSibling();
                    if (Summary) Summary.text += " · " + rejection.Reason;
                }
            }
            float f = research ? 0 : Mathf.Clamp01(data.Time / Mathf.Max(.001f, data.Duration));
            Marker.gameObject.SetActive(!research);
            Marker.anchorMin = new Vector2(f, 0); Marker.anchorMax = new Vector2(f, 1);
            float offset = 300 * (1 - f) - 24 * f;
            Marker.offsetMin = new Vector2(offset - 1, 0); Marker.offsetMax = new Vector2(offset + 1, 0);
        }
    }
}
