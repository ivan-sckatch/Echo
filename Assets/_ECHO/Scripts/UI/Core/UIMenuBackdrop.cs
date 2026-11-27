using UnityEngine;
using UnityEngine.UI;

namespace EchoGame.UI
{
    [RequireComponent(typeof(RawImage))]
    public sealed class UIMenuBackdrop : MonoBehaviour
    {
        public Material Blur;
        RawImage image;
        RenderTexture snapshot;
        public void Show(bool visible)
        {
            if (!image) image = GetComponent<RawImage>();
            if (!visible) { image.enabled = false; Release(); return; }
            if (!snapshot) Capture();
            image.enabled = snapshot; image.texture = snapshot;
        }
        void Capture()
        {
            var source = Camera.main ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (!source) return;
            int width = Mathf.Max(640, Screen.width / 2), height = Mathf.Max(360, Screen.height / 2);
            var temporary = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var go = new GameObject("Paused scene snapshot") { hideFlags = HideFlags.HideAndDontSave };
            var camera = go.AddComponent<Camera>(); camera.enabled = false;
            camera.CopyFrom(source); camera.enabled = false;
            camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            camera.cullingMask = source.cullingMask & ~(1 << 5); // Exclude screen UI from the snapshot.
            camera.targetTexture = temporary;
            try
            {
                camera.Render();
                snapshot = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32) { name = "Menu blurred world", filterMode = FilterMode.Bilinear };
                snapshot.Create();
                if (Blur)
                {
                    var intermediate = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                    try { Graphics.Blit(temporary, intermediate, Blur); Graphics.Blit(intermediate, snapshot, Blur); }
                    finally { RenderTexture.ReleaseTemporary(intermediate); }
                }
                else Graphics.Blit(temporary, snapshot);
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.ReleaseTemporary(temporary); Destroy(go);
            }
        }
        void Release()
        {
            if (image) image.texture = null;
            if (!snapshot) return;
            snapshot.Release(); Destroy(snapshot); snapshot = null;
        }
        void OnDestroy() => Release();
    }
}
