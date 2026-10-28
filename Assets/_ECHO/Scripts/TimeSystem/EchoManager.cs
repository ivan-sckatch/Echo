using System.Collections.Generic;
using UnityEngine;
namespace EchoGame
{
    public sealed class EchoManager : MonoBehaviour
    {
        [Range(1, 16)] public int MaximumEchoes = 3;
        public Transform Container;
        public bool ConcurrentIntervals;
        public bool ShareSingleSlot;
        public EchoPlayback Prefab;
        readonly List<EchoPlayback> echoes = new List<EchoPlayback>();
        int nextId = 1;
        public event System.Action Changed;
        public IReadOnlyList<EchoPlayback> Echoes => echoes;
        public void Add(EchoRecording recording)
        {
            if (recording == null) return;
            if (ConcurrentIntervals && FindConflict(recording) != null) return;
            if (ShareSingleSlot && echoes.Count > 0)
            {
                echoes[0].Initialize(EchoRecording.Combine(new[] { echoes[0].Recording, recording }));
                echoes[0].ResetEvents(); Changed?.Invoke(); return;
            }
            while (!ConcurrentIntervals && echoes.Count >= MaximumEchoes) RemoveFirst();
            var echo = Instantiate(Prefab, Container);
            echo.Id = nextId++;
            echo.name = "Echo " + echo.Id.ToString("00");
            echo.Initialize(recording); echoes.Add(echo);
            Changed?.Invoke();
        }
        public EchoPlayback FindConflict(EchoRecording recording)
        {
            foreach (var e in echoes) foreach (var saved in e.Recording.Fragments) foreach (var candidate in recording.Fragments)
                if (candidate.StartTime < saved.EndTime && saved.StartTime < candidate.EndTime) return e;
            return null;
        }
        public void RemoveLastFragment()
        {
            if (echoes.Count == 0) return;
            var e = echoes[echoes.Count - 1];
            if (!ShareSingleSlot || e.Recording.Fragments.Count == 1) { Remove(e.Id); return; }
            var parts = new List<EchoRecording>(e.Recording.Fragments); parts.RemoveAt(parts.Count - 1);
            e.Initialize(EchoRecording.Combine(parts)); e.ResetEvents(); Changed?.Invoke();
        }
        public void Remove(int id)
        {
            var echo = echoes.Find(e => e.Id == id); if (!echo) return;
            echoes.Remove(echo); echo.gameObject.SetActive(false); Destroy(echo.gameObject); Changed?.Invoke();
        }
        void RemoveFirst()
        {
            var echo = echoes[0]; echoes.RemoveAt(0);
            echo.gameObject.SetActive(false); Destroy(echo.gameObject);
        }
        public void Clear() { while (echoes.Count > 0) RemoveFirst(); nextId = 1; Changed?.Invoke(); }
        public void Evaluate(float time)
        {
            bool changed = false;
            foreach (var echo in echoes) { var previous = echo.State; echo.Evaluate(time); changed |= previous != echo.State; }
            if (changed) Changed?.Invoke();
        }
    }
}
