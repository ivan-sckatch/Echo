using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class TimelineUI : MonoBehaviour
    {
        public UIPanelMotion Motion;
        public TMP_Text Clock, Duration;
        public RectTransform Content, Marker;
        public TimelineTrackUI TrackPrefab;
        readonly Dictionary<int, TimelineTrackUI> tracks = new Dictionary<int, TimelineTrackUI>();
        readonly List<float> interactions = new List<float>();
        public int InteractionCount => interactions.Count;
        public void AddInteraction(string label, float time) => interactions.Add(time);
        public void ClearInteractions() => interactions.Clear();
        TimelineTrackUI player;
        string signature;
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
            foreach (var e in data.Echoes) tracks[e.Id].SetState(e.State);
            player.State.text = data.Recording == RecordingState.Recording ? "● ЗАПИСЬ" : data.Echoes.Count == 0 ? "НЕТ ЗАПИСЕЙ" : "ОЖИДАНИЕ";
            player.SetPlayer(data.RecordStart, data.RecordEnd, Mathf.Max(1, data.Duration));
            var scroll = Content.GetComponentInParent<ScrollRect>();
            var panel = (RectTransform)scroll.transform.parent;
            var canvas = (RectTransform)Motion.transform;
            float rowHeight = TrackPrefab.GetComponent<LayoutElement>().preferredHeight;
            float height = Mathf.Min(canvas.rect.height * .4f, 118 + (data.Echoes.Count + 1) * (rowHeight + 8));
            panel.sizeDelta = new Vector2(Mathf.Min(1320, canvas.rect.width - 80), height);
            var scrollRect = (RectTransform)scroll.transform;
            scrollRect.anchorMin = Vector2.zero; scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(24, 44); scrollRect.offsetMax = new Vector2(-24, -68);
            player.SetInteractionTimes(interactions, Mathf.Max(1, data.Duration));
            Clock.text = UITheme.Clock(data.Time); Duration.text = UITheme.Clock(data.Duration);
            float f = Mathf.Clamp01(data.Time / Mathf.Max(1, data.Duration));
            Marker.anchorMin = new Vector2(f, 0); Marker.anchorMax = new Vector2(f, 1);
            float offset = 300 * (1 - f) - 24 * f;
            Marker.offsetMin = new Vector2(offset - 1, 0); Marker.offsetMax = new Vector2(offset + 1, 0);
        }
    }
}
