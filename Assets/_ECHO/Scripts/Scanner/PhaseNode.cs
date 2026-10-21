using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace EchoGame
{
    public enum PhaseNodeMode { Impulse, Continuous }
    public sealed class PhaseNode : MonoBehaviour
    {
        public string StableId;
        public string DisplayName = "ФАЗОВЫЙ УЗЕЛ";
        public PhaseNodeMode Mode;
        public bool InitiallyHidden = true;
        [Min(.01f)] public float DiscoverySeconds = .5f, ActivationSeconds = 2, HighlightSeconds = 5;
        public Renderer Visual;
        public Material DiscoveredMaterial, InactiveMaterial, ActiveMaterial;
        public UnityEvent OnActivated = new UnityEvent();
        public event Action<PhaseNode, int> Activated;
        sealed class Exposure { public bool Touched, Fired; public float Discovery, Activation; }
        readonly Dictionary<int, Exposure> sources = new Dictionary<int, Exposure>();
        readonly List<int> lost = new List<int>();
        readonly List<int> signaling = new List<int>();
        float highlight;
        public bool IsDiscovered { get; private set; }
        public bool IsSignaling { get; private set; }
        public bool ActivatedThisStep { get; private set; }
        public bool HasActivated { get; private set; }
        public IReadOnlyList<int> SignalingSources => signaling;
        public bool HasPlayerSignal => signaling.Contains(0);
        public bool HasEchoSignal { get { foreach (int id in signaling) if (id > 0) return true; return false; } }
        public string SignalOwner
        {
            get { string value = ""; foreach (int id in signaling) value += (value.Length == 0 ? "" : " + ") + (id == 0 ? "ИГРОК" : "ЭХО " + id); return value.Length > 0 ? value : "НЕАКТИВЕН"; }
        }
        public float DiscoveryProgress(int source) => IsDiscovered ? 1 : sources.TryGetValue(source, out var e) ? Mathf.Clamp01(e.Discovery / DiscoverySeconds) : 0;
        public float ActivationProgress(int source) => sources.TryGetValue(source, out var e) ? Mathf.Clamp01(e.Activation / ActivationSeconds) : 0;
        public void BeginStep()
        {
            ActivatedThisStep = false; signaling.Clear();
            foreach (var exposure in sources.Values) exposure.Touched = false;
        }
        public void Submit(int source)
        {
            if (!sources.TryGetValue(source, out var e)) { e = new Exposure(); sources.Add(source, e); }
            e.Touched = true;
        }
        public void EndStep(float dt)
        {
            lost.Clear(); bool illuminated = false;
            foreach (var pair in sources)
            {
                var e = pair.Value;
                if (!e.Touched) { lost.Add(pair.Key); continue; }
                illuminated = true;
                float activationStep = dt;
                if (!IsDiscovered)
                {
                    float needed = Mathf.Max(0, DiscoverySeconds - e.Discovery);
                    e.Discovery += dt; activationStep = Mathf.Max(0, dt - needed);
                    if (e.Discovery + .00001f >= DiscoverySeconds) IsDiscovered = true;
                    else continue;
                }
                e.Activation += activationStep;
                if (e.Activation + .00001f >= ActivationSeconds)
                {
                    if (!e.Fired)
                    { e.Fired = true; HasActivated = true; ActivatedThisStep = true; Activated?.Invoke(this, pair.Key); OnActivated.Invoke(); }
                    if (Mode == PhaseNodeMode.Continuous || ActivatedThisStep) signaling.Add(pair.Key);
                }
            }
            foreach (int id in lost) sources.Remove(id);
            if (IsDiscovered && illuminated) highlight = HighlightSeconds;
            else if (InitiallyHidden && IsDiscovered) { highlight -= dt; if (highlight <= 0) IsDiscovered = false; }
            IsSignaling = signaling.Count > 0;
            UpdateVisual();
        }
        public void ResetNode()
        {
            sources.Clear(); signaling.Clear(); lost.Clear(); highlight = 0;
            IsDiscovered = !InitiallyHidden; IsSignaling = ActivatedThisStep = HasActivated = false; UpdateVisual();
        }
        void UpdateVisual()
        {
            if (!Visual) return;
            Visual.enabled = IsDiscovered;
            Visual.sharedMaterial = IsSignaling ? ActiveMaterial : InitiallyHidden ? DiscoveredMaterial : InactiveMaterial;
        }
    }
}
