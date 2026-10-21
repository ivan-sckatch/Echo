using UnityEngine;
using UnityEngine.Rendering;
namespace EchoGame
{
    public sealed class ScanVisualController : MonoBehaviour
    {
        public LineRenderer Beam;
        public Transform HitMarker;
        public static ScanVisualController Create(Transform parent, string name, Material material, float width)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            var visual = root.AddComponent<ScanVisualController>();
            visual.Beam = root.AddComponent<LineRenderer>(); visual.Beam.sharedMaterial = material;
            visual.Beam.useWorldSpace = true; visual.Beam.positionCount = 2; visual.Beam.startWidth = width * .2f; visual.Beam.endWidth = width * .55f;
            visual.Beam.shadowCastingMode = ShadowCastingMode.Off; visual.Beam.receiveShadows = false;
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere); marker.name = "Scan contact"; marker.transform.SetParent(root.transform, false);
            var collider = marker.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            marker.GetComponent<Renderer>().sharedMaterial = material; marker.transform.localScale = Vector3.one * .09f;
            visual.HitMarker = marker.transform; visual.Hide(); return visual;
        }
        public void Render(ScanFrame frame, Vector3 end, bool target)
        {
            Beam.enabled = frame.Enabled;
            HitMarker.gameObject.SetActive(frame.Enabled && target);
            if (!frame.Enabled) return;
            Vector3 right = Vector3.Cross(Vector3.up, frame.Direction).normalized;
            Beam.SetPosition(0, frame.Origin + frame.Direction * .35f + right * .12f - Vector3.up * .1f);
            Beam.SetPosition(1, end); HitMarker.position = end;
        }
        public void Hide() { Beam.enabled = false; HitMarker.gameObject.SetActive(false); }
    }
}
