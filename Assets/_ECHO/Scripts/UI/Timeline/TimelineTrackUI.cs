using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class TimelineTrackUI : MonoBehaviour
    {
        public TMP_Text Label, State;
        public Toggle Visibility;
        public RectTransform Lane;
        public Image SegmentPrefab;
        readonly List<Image> segments = new List<Image>();
        public int Id { get; private set; }
        public void Bind(int id, string title, IReadOnlyList<ActionInterval> actions, float duration, Color color)
        {
            Id = id; Label.text = title;
            foreach (var segment in segments) Destroy(segment.gameObject); segments.Clear();
            foreach (var action in actions)
            {
                var segment = Instantiate(SegmentPrefab, Lane); segment.color = action.Moving ? color : new Color(color.r, color.g, color.b, .35f);
                var rect = segment.rectTransform;
                rect.anchorMin = new Vector2(Mathf.Clamp01(action.Start / duration), .15f);
                rect.anchorMax = new Vector2(Mathf.Clamp01(action.End / duration), .85f);
                rect.offsetMin = new Vector2(1, 0); rect.offsetMax = new Vector2(-1, 0);
                segments.Add(segment);
            }
            Visibility.onValueChanged.RemoveAllListeners();
            Visibility.onValueChanged.AddListener(visible => Lane.gameObject.SetActive(visible));
            Lane.gameObject.SetActive(Visibility.isOn);
        }
        public void SetState(EchoState state) { State.text = UITheme.StateName(state); State.color = state == EchoState.SyncError ? UITheme.Error : UITheme.Muted; }
        public void SetPlayer(float start, float time, float duration)
        {
            if (segments.Count == 0) return;
            segments[0].rectTransform.anchorMin = new Vector2(Mathf.Clamp01(start / duration), .15f);
            segments[0].rectTransform.anchorMax = new Vector2(Mathf.Clamp01(time / duration), .85f);
        }
    }
}
