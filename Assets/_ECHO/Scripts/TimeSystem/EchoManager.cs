using System.Collections.Generic;
using UnityEngine;
namespace EchoGame
{
    public sealed class EchoManager : MonoBehaviour
    {
        [Range(1, 16)] public int MaximumEchoes = 3;
        public Transform Container;
        public bool ConcurrentIntervals;
        public EchoPlayback Prefab;
        readonly List<EchoPlayback> echoes = new List<EchoPlayback>();
        int nextId = 1;
        public event System.Action Changed;
        public IReadOnlyList<EchoPlayback> Echoes => echoes;
        public void Add(EchoRecording recording)
        {
            if (recording == null) return;
            if (ConcurrentIntervals && FindConflict(recording) != null) return;
            while (!ConcurrentIntervals && echoes.Count >= MaximumEchoes) RemoveFirst();
            var echo = Instantiate(Prefab, Container);
            echo.Id = nextId++;
            echo.name = "Echo " + echo.Id.ToString("00");
            echo.Initialize(recording); echoes.Add(echo);
            Changed?.Invoke();
        }
        public EchoPlayback FindConflict(EchoRecording recording)
        {
            foreach (var e in echoes)
                if (recording.StartTime < e.Recording.EndTime && e.Recording.StartTime < recording.EndTime) return e;
            return null;
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
