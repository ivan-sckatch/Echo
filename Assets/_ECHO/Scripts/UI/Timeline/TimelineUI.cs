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
                    row.Bind(e.Id, "ЭХО " + e.Id.ToString("00"), e.Intervals, Mathf.Max(1, data.Duration), e.Id % 2 == 0 ? UITheme.Secondary : UITheme.Cyan);
                    tracks.Add(e.Id, row);
                }
                player = Instantiate(TrackPrefab, Content);
                player.Bind(0, "ИГРОК", new[] { new ActionInterval(data.RecordStart, data.Time, "Запись", true) }, Mathf.Max(1, data.Duration), UITheme.Amber);
                signature = next; Marker.SetAsLastSibling();
            }
            foreach (var e in data.Echoes) tracks[e.Id].SetState(e.State);
            player.State.text = data.Recording == RecordingState.Recording ? "ЗАПИСЬ" : "ОЖИДАНИЕ";
            player.SetPlayer(data.RecordStart, data.RecordEnd, Mathf.Max(1, data.Duration));
            Clock.text = UITheme.Clock(data.Time); Duration.text = UITheme.Clock(data.Duration);
            float f = Mathf.Clamp01(data.Time / Mathf.Max(1, data.Duration));
            Marker.anchorMin = new Vector2(f, 0); Marker.anchorMax = new Vector2(f, 1);
            float offset = 300 * (1 - f) - 24 * f;
            Marker.offsetMin = new Vector2(offset - 1, 0); Marker.offsetMax = new Vector2(offset + 1, 0);
        }
    }
}
