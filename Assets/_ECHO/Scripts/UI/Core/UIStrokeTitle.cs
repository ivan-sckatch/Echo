using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EchoGame.UI
{
    // Original monoline title lettering; independent of installed desktop fonts.
    // Body text remains TMP for legibility and localization.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIStrokeTitle : MaskableGraphic
    {
        public string Title = "ECHO";
        public float Stroke = 1.65f;
        public float Tracking = .18f;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (string.IsNullOrEmpty(Title)) return;
            var r = rectTransform.rect;
            float height = Mathf.Min(r.height - Stroke * 2, r.width / (Title.Length * (.7f + Tracking) - Tracking));
            float width = height * (Title.Length * (.7f + Tracking) - Tracking);
            Vector2 origin = new Vector2(r.center.x - width / 2, r.center.y - height / 2);
            foreach (char c in Title)
            {
                foreach (var path in Paths(c))
                    for (int i = 1; i < path.Length; i++)
                        Line(mesh, origin + path[i - 1] * height, origin + path[i] * height);
                origin.x += height * (.7f + Tracking);
            }
        }
        void Line(VertexHelper mesh, Vector2 a, Vector2 b)
        {
            var n = new Vector2(-(b - a).y, (b - a).x).normalized * Stroke / 2;
            int i = mesh.currentVertCount;
            foreach (Vector2 p in new[] { a - n, a + n, b + n, b - n }) mesh.AddVert(p, color, Vector2.zero);
            mesh.AddTriangle(i, i + 1, i + 2); mesh.AddTriangle(i, i + 2, i + 3);
        }
        static Vector2[] P(params float[] xy)
        {
            var p = new Vector2[xy.Length / 2];
            for (int i = 0; i < p.Length; i++) p[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return p;
        }
        static Vector2[] Arc(float start, float end, float cx = .35f, float cy = .5f, float rx = .35f, float ry = .5f)
        {
            int count = Mathf.CeilToInt(Mathf.Abs(end - start) / 5);
            var p = new Vector2[count + 1];
            for (int i = 0; i <= count; i++)
            {
                float a = Mathf.Lerp(start, end, (float)i / count) * Mathf.Deg2Rad;
                p[i] = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry);
            }
            return p;
        }
        static IEnumerable<Vector2[]> Paths(char c)
        {
            switch (c)
            {
                case 'E': yield return P(.7f, 1, 0, 1, 0, 0, .7f, 0); yield return P(0, .5f, .56f, .5f); break;
                case 'C': case 'С': yield return Arc(42, 318); break;
                case 'H': case 'Н': yield return P(0, 0, 0, 1); yield return P(.7f, 0, .7f, 1); yield return P(0, .5f, .7f, .5f); break;
                case 'O': case 'О': yield return Arc(0, 360); break;
                case 'П': yield return P(0, 0, 0, 1, .7f, 1, .7f, 0); break;
                case 'А': yield return P(0, 0, .35f, 1, .7f, 0); yield return P(.14f, .4f, .56f, .4f); break;
                case 'У': yield return P(0, 1, .35f, .45f); yield return P(.7f, 1, .13f, 0, 0, 0); break;
                case 'З': yield return Arc(90, -90, .3f, .76f, .34f, .24f); yield return Arc(90, -100, .3f, .26f, .38f, .26f); break;
                case 'Т': yield return P(0, 1, .7f, 1); yield return P(.35f, 1, .35f, 0); break;
                case 'Р': yield return P(0, 0, 0, 1, .35f, 1); yield return Arc(90, -90, .35f, .75f, .35f, .25f); yield return P(.35f, .5f, 0, .5f); break;
                case 'И': case 'Й':
                    yield return P(0, 1, 0, 0, .7f, 1, .7f, 0);
                    if (c == 'Й') yield return Arc(200, 340, .35f, 1.16f, .17f, .08f);
                    break;
                case 'К': yield return P(0, 0, 0, 1); yield return P(.7f, 1, 0, .45f, .7f, 0); break;
            }
        }
    }
}
