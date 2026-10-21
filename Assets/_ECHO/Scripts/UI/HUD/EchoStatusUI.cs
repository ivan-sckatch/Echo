using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace EchoGame.UI
{
    public sealed class EchoStatusUI : MonoBehaviour
    {
        public TMP_Text Count, Empty;
        public Transform Content;
        public TMP_Text RowPrefab;
        readonly Dictionary<int, TMP_Text> rows = new Dictionary<int, TMP_Text>();
        readonly Dictionary<int, EchoState> states = new Dictionary<int, EchoState>();
        public void Apply(UISnapshot data)
        {
            int active = 0;
            foreach (var echo in data.Echoes) if (echo.State == EchoState.Playback) active++;
            Count.text = $"АКТИВНЫЕ КОПИИ: {active} / {data.Capacity}";
            Empty.gameObject.SetActive(data.Echoes.Count == 0);
            var removed = new List<int>();
            foreach (int id in rows.Keys)
            {
                bool found = false; foreach (var e in data.Echoes) if (e.Id == id) { found = true; break; }
                if (!found) removed.Add(id);
            }
            foreach (int id in removed) { Destroy(rows[id].gameObject); rows.Remove(id); states.Remove(id); }
            foreach (var echo in data.Echoes)
            {
                if (!rows.TryGetValue(echo.Id, out var row)) { row = Instantiate(RowPrefab, Content); rows.Add(echo.Id, row); }
                row.text = $"ЭХО {echo.Id:00}  ·  {UITheme.StateName(echo.State)}";
                Color color = echo.State == EchoState.SyncError ? UITheme.Error : echo.State == EchoState.Completed ? UITheme.Muted : UITheme.Cyan;
                if (!states.TryGetValue(echo.Id, out var previous) || previous != echo.State)
                {
                    var pulse = row.GetComponent<UIPulse>(); if (!pulse) pulse = row.gameObject.AddComponent<UIPulse>();
                    pulse.Flash(row, color); states[echo.Id] = echo.State;
                }
            }
        }
    }
}
