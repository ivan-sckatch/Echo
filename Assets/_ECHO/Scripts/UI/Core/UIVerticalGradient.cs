using UnityEngine;
using UnityEngine.UI;
namespace EchoGame.UI
{
    // Vertex tint only: no extra texture, material, Update or postprocessing pass.
    public sealed class UIVerticalGradient : BaseMeshEffect
    {
        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive()) return;
            var rect = graphic.rectTransform.rect;
            var vertex = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                float factor = Mathf.Lerp(.82f, 1.28f, Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y));
                Color color = vertex.color; color.r *= factor; color.g *= factor; color.b *= factor;
                vertex.color = color; mesh.SetUIVertex(vertex, i);
            }
        }
    }
}
