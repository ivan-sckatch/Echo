using System.Collections.Generic;
using UnityEngine;
namespace EchoGame
{
    // Called only by TimeCycleManager. Neither emitters nor nodes advance an independent clock.
    public sealed class ScanSimulation : MonoBehaviour
    {
        public GameManager Game;
        public TimeCycleManager Cycles;
        public TemporalScanner PlayerScanner;
        public PhaseNode[] Nodes;
        public LayerMask DetectionMask = ~0;
        public Material PlayerBeamMaterial, EchoBeamMaterial;
        [Min(.001f)] public float BeamWidth = .02f;
        public float SynchronizationRoomStart = 11.5f;
        public bool SynchronizationHints = true;
        public ScanRegistry Registry { get; } = new ScanRegistry();
        public PhaseNode PlayerTarget { get; private set; }
        public ScanVisualController PlayerVisual { get; private set; }
        public IReadOnlyCollection<int> ContributingEchoes => contributing;
        readonly HashSet<int> contributing = new HashSet<int>();
        readonly Dictionary<int, ScanVisualController> echoes = new Dictionary<int, ScanVisualController>();
        readonly List<int> removed = new List<int>();
        bool initialized, introShown, discoveryHintShown, overlapHintShown;
        void OnEnable() { if (Game) Game.StateChanged += OnState; }
        void OnDisable() { if (Game) Game.StateChanged -= OnState; HideBeams(); }
        void EnsureInitialized()
        {
            if (initialized) return;
            Registry.Rebuild(Nodes);
            PlayerVisual = ScanVisualController.Create(transform, "Player scan beam", PlayerBeamMaterial, BeamWidth);
            initialized = true;
        }
        void OnState(GameState state) { if (state != GameState.Playing) HideBeams(); }
        public void HideBeams()
        {
            if (PlayerVisual) PlayerVisual.Hide();
            foreach (var visual in echoes.Values) if (visual) visual.Hide();
        }
        public void ResetSimulation()
        {
            EnsureInitialized(); Registry.Rebuild(Nodes); PlayerScanner.ResetScanner(); PlayerTarget = null;
            contributing.Clear(); foreach (var node in Nodes) node.ResetNode(); HideBeams(); PruneEchoVisuals();
            if (!introShown) { introShown = true; Cycles.Notify("Удерживайте ПКМ, чтобы активировать темпоральный сканер.", 9); }
        }
        void PruneEchoVisuals()
        {
            removed.Clear();
            foreach (var pair in echoes)
            {
                bool exists = false; foreach (var echo in Cycles.Echoes.Echoes) if (echo.Id == pair.Key) { exists = true; break; }
                if (!exists || !pair.Value) { if (pair.Value) { pair.Value.Hide(); Destroy(pair.Value.gameObject); } removed.Add(pair.Key); }
            }
            foreach (int id in removed) echoes.Remove(id);
        }
        public void Tick(float dt)
        {
            EnsureInitialized(); contributing.Clear();
            foreach (var node in Nodes) node.BeginStep();
            Physics.SyncTransforms();
            ScanFrame player = PlayerScanner.ReadInput();
            PlayerTarget = ScanDetector.Trace(player, PlayerScanner.Range, DetectionMask, Registry, false, out var playerEnd);
            PlayerScanner.SetTarget(PlayerTarget ? PlayerTarget.StableId : null);
            if (PlayerTarget) PlayerTarget.Submit(0);
            PruneEchoVisuals();
            foreach (var echo in Cycles.Echoes.Echoes)
            {
                if (!echoes.TryGetValue(echo.Id, out var visual))
                { visual = ScanVisualController.Create(echo.transform, "Echo scan beam", EchoBeamMaterial, BeamWidth); echoes.Add(echo.Id, visual); }
                var frame = echo.IsActive ? echo.CurrentFrame.Scan : default;
                var target = ScanDetector.Trace(frame, PlayerScanner.Range, DetectionMask, Registry, true, out var end);
                if (target) target.Submit(echo.Id);
                visual.Render(frame, end, target);
            }
            foreach (var node in Nodes)
            {
                node.EndStep(dt);
                foreach (int id in node.SignalingSources) if (id > 0) contributing.Add(id);
                if (node.IsDiscovered && !discoveryHintShown)
                { discoveryHintShown = true; Cycles.Notify("Удерживайте сканирующий луч на фазовом узле.", 7); }
            }
            PlayerVisual.Render(PlayerScanner.Current, playerEnd, PlayerTarget);
            if (SynchronizationHints && !overlapHintShown && Cycles.Player.Feet.z >= SynchronizationRoomStart)
            { overlapHintShown = true; Cycles.Notify("Некоторые механизмы требуют одновременного воздействия.", 8); }
        }
        public void Analyze()
        {
            EnsureInitialized();
            var frame = PlayerScanner.ReadInput();
            PlayerScanner.Inspect(Cycles);
            PlayerTarget = ScanDetector.Trace(frame, PlayerScanner.Range, DetectionMask, Registry, false, out var end);
            PlayerScanner.SetTarget(PlayerTarget ? PlayerTarget.StableId : null);
            PlayerVisual.Render(frame, end, PlayerTarget);
        }
    }
}
