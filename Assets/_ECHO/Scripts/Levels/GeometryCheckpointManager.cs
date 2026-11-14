using UnityEngine;
namespace EchoGame
{
    // Session-local snapshots extend TimeCycleManager; no separate save-game subsystem.
    public sealed class GeometryCheckpointManager : MonoBehaviour
    {
        public TimeCycleManager Cycles;
        public GeometryMechanism[] Mechanisms;
        public Transform[] SectionSpawns;
        public Transform[] Checkpoints;
        public Renderer[] CheckpointVisuals;
        public Light[] CheckpointLights;
        public Material PendingMaterial, FixedMaterial;
        public int ActiveCheckpoint { get; private set; }
        GeometrySnapshot[] baseline;
        float glow;
        public void ResetState(bool full)
        {
            if (baseline == null || full)
            {
                ActiveCheckpoint = 0; baseline = new GeometrySnapshot[Mechanisms.Length];
                for (int i = 0; i < Mechanisms.Length; i++) { Mechanisms[i].ResetMechanism(); baseline[i] = Mechanisms[i].Capture(); }
            }
            else for (int i = 0; i < Mechanisms.Length; i++) Mechanisms[i].Restore(baseline[i]);
            Cycles.Spawn = SectionSpawns[ActiveCheckpoint];
            Cycles.FallResetHeight = SectionSpawns[ActiveCheckpoint].position.y - 3;
            Refresh();
        }
        public bool TryActivate(int checkpoint, bool lowerReady)
        {
            if (checkpoint != ActiveCheckpoint + 1 || checkpoint > Checkpoints.Length) return false;
            if (ActiveCheckpoint == 0)
            {
                if (!lowerReady) return false;
                foreach (var m in Mechanisms) if (m.Section == 0 && (m.IsMoving || !m.AtTarget))
                { Cycles.Notify("Механизм ещё не завершил движение", 2); return false; }
            }
            // Reaching the second pad completes the tier, even during retraction.
            foreach (var m in Mechanisms) if (m.Section == ActiveCheckpoint) m.Freeze();
            for (int i = 0; i < Mechanisms.Length; i++) baseline[i] = Mechanisms[i].Capture();
            ActiveCheckpoint = checkpoint;
            Cycles.ClearCheckpointEchoes();
            Cycles.Restart(false);
            glow = 1.5f; Refresh();
            Cycles.Notify("Контрольная точка активирована · Геометрия зафиксирована · Новая временная петля", 5);
            return true;
        }
        public bool ContainsNext(Vector3 feet)
        {
            if (ActiveCheckpoint >= Checkpoints.Length) return false;
            var p = feet - Checkpoints[ActiveCheckpoint].position;
            return Mathf.Abs(p.x) < 1.5f && Mathf.Abs(p.z) < 1.5f && Mathf.Abs(p.y) < 1;
        }
        public void Tick(float dt) { glow = Mathf.Max(0, glow - dt); Refresh(); }
        void Refresh()
        {
            for (int i = 0; i < CheckpointVisuals.Length; i++) CheckpointVisuals[i].sharedMaterial = i < ActiveCheckpoint ? FixedMaterial : PendingMaterial;
            for (int i = 0; i < CheckpointLights.Length; i++) CheckpointLights[i].intensity = i == ActiveCheckpoint - 1 ? .8f + glow * 3 : .3f;
        }
    }
}
