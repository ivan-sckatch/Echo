using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace EchoGame
{
    public sealed class SecurityCameraController : MonoBehaviour
    {
        public string CameraId;
        public Transform Head;
        [Min(.1f)] public float Range = 9;
        [Range(1, 170)] public float FieldOfView = 90;
        [Range(0, 60)] public float SweepAngle = 8;
        [Min(.1f)] public float SweepSeconds = 6;
        public LayerMask OcclusionMask = ~0;
        public TMP_Text Display;
        public LineRenderer Cone, LockBeam;
        public CaptureTarget Target { get; private set; }
        public int CaptureCount { get; private set; }
        Quaternion rest;
        EchoRecording heldFragment;
        bool initialized;
        readonly RaycastHit[] sightHits = new RaycastHit[64];
        void Initialize()
        {
            if (initialized) return; rest = Head.localRotation; initialized = true;
            if (!GetComponent<CameraVisionFloor>()) gameObject.AddComponent<CameraVisionFloor>();
            if (Cone) Cone.enabled = false;
        }
        public bool CanSee(CaptureTarget target)
        {
            if (!target || !target.Exists) return false;
            return CanSeePosition(target.AimPoint, target.transform);
        }
        public bool CanSeePosition(Vector3 point, Transform targetRoot = null, bool ignoreCharacters = false)
        {
            Vector3 delta = point - Head.position;
            if (delta.magnitude > Range || Vector3.Angle(Head.forward, delta) > FieldOfView * .5f) return false;
            int count = Physics.RaycastNonAlloc(Head.position, delta.normalized, sightHits, delta.magnitude, OcclusionMask, QueryTriggerInteraction.Ignore);
            var hits = sightHits;
            if (count == sightHits.Length) { hits = Physics.RaycastAll(Head.position, delta.normalized, delta.magnitude, OcclusionMask, QueryTriggerInteraction.Ignore); count = hits.Length; }
            for(int i=0;i<count;i++)
            {
                var hit = hits[i];
                if (hit.transform.IsChildOf(transform) || targetRoot && hit.transform.IsChildOf(targetRoot) ||
                    ignoreCharacters && hit.transform.GetComponentInParent<CaptureTarget>()) continue;
                return false;
            }
            return true;
        }
        public void Tick(float time, IReadOnlyList<CaptureTarget> targets, TimeCycleManager cycles)
        {
            Initialize();
            if (Target && (!Target.Exists || Target.Echo && Target.FragmentAt(time) != heldFragment)) Release();
            if (!Target)
            {
                Head.localRotation = rest * Quaternion.Euler(0, Mathf.Sin(time * Mathf.PI * 2 / SweepSeconds) * SweepAngle, 0);
                // Stable input order resolves simultaneous acquisition; no switching after acquisition.
                foreach (var candidate in targets) if (CanSee(candidate))
                {
                    Target = candidate; heldFragment = candidate.FragmentAt(time); Target.Capture(this); CaptureCount++;
                    if (Target.Echo) cycles.MarkEchoUsed(Target.Echo.Id);
                    else cycles.Notify(CameraId + " · ЗАХВАТ. R — сохранить запись · T — повторить цикл", 7);
                    break;
                }
            }
            if (Target) Head.rotation = Quaternion.LookRotation(Target.AimPoint - Head.position);
            if (Display) Display.text = CameraId + (Target ? Target.Player ? " · ИГРОК ЗАХВАЧЕН" : " · ЭХО ЗАХВАЧЕНО" : " · НАБЛЮДЕНИЕ");
            if (LockBeam) { LockBeam.enabled = Target; if (Target) { LockBeam.SetPosition(0, Head.position); LockBeam.SetPosition(1, Target.AimPoint); } }
            if (Cone) Cone.enabled = false;
        }
        void Release() { if (Target) Target.Release(this); Target = null; heldFragment = null; }
        public void ResetCamera() { Initialize(); Release(); CaptureCount = 0; Head.localRotation = rest; if (LockBeam) LockBeam.enabled = false; }
        void OnDisable() => Release();
        void OnDrawGizmosSelected()
        {
            if (!Head) return; Gizmos.color = Color.cyan;
            for (int i=0;i<16;i++) { float a=i*Mathf.PI/8; float r=Mathf.Tan(FieldOfView*.5f*Mathf.Deg2Rad)*Range; Gizmos.DrawLine(Head.position,Head.TransformPoint(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Range))); }
        }
    }
}
