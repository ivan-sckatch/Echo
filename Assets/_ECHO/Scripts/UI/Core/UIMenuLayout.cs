using UnityEngine;

namespace EchoGame.UI
{
    // Fit the composed menu as a unit, including on 16:10 and at 125% UI scale.
    [ExecuteAlways]
    public sealed class UIMenuLayout : MonoBehaviour
    {
        public Vector2 DesignSize = new Vector2(1520, 960);
        void OnEnable() { Canvas.willRenderCanvases += Fit; Fit(); }
        void OnDisable() => Canvas.willRenderCanvases -= Fit;
        void Fit()
        {
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (!parent || parent.rect.width <= 0 || parent.rect.height <= 0) return;
            float scale = Mathf.Min(1, (parent.rect.width - 48) / DesignSize.x, (parent.rect.height - 48) / DesignSize.y);
            rect.localScale = Vector3.one * Mathf.Max(.1f, scale);
        }
    }
}
