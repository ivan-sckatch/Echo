using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoGame
{
    // A conservative footprint for a standing character, clipped by the same optics and walls as acquisition.
    [RequireComponent(typeof(SecurityCameraController))]
    public sealed class CameraVisionFloor : MonoBehaviour
    {
        const float Cell = .35f, Lift = .025f;
        public Mesh Surface => mesh;
        public bool Focused { get; private set; }
        SecurityCameraController camera;
        TimeCycleManager cycles;
        Mesh mesh;
        MeshRenderer visual;
        Material fill, outline;
        float nextRefresh;
        CaptureTarget previousTarget;
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> faces = new List<int>(), edges = new List<int>();
        Vector3[] floor;
        bool[] visible, cells;
        readonly RaycastHit[] groundHits = new RaycastHit[24];
        void Awake()
        {
            camera = GetComponent<SecurityCameraController>(); cycles = FindAnyObjectByType<TimeCycleManager>();
            var root = new GameObject("Camera vision on floor"); root.transform.SetParent(transform, true);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            mesh = new Mesh { name = "Camera floor footprint" }; mesh.MarkDynamic();
            root.AddComponent<MeshFilter>().sharedMesh = mesh; visual = root.AddComponent<MeshRenderer>();
            fill = Material(.17f); outline = Material(.9f); visual.sharedMaterials = new[] { fill, outline };
            visual.shadowCastingMode = ShadowCastingMode.Off; visual.receiveShadows = false;
        }
        Material Material(float alpha)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3050;
            material.SetColor("_BaseColor", new Color(1,.6f,.12f,alpha)); return material;
        }
        void LateUpdate()
        {
            if (camera.Target || previousTarget != camera.Target || Time.unscaledTime >= nextRefresh)
            { Rebuild(); nextRefresh = Time.unscaledTime + .12f; previousTarget = camera.Target; }
        }
        bool Ground(Vector3 position, out Vector3 point)
        {
            position.y = camera.Head.position.y + .05f;
            int count = Physics.RaycastNonAlloc(position, Vector3.down, groundHits, camera.Range + 4, camera.OcclusionMask, QueryTriggerInteraction.Ignore);
            RaycastHit hit = default; float nearest = float.PositiveInfinity;
            for(int i=0;i<count;i++)
                if(groundHits[i].distance<nearest && !groundHits[i].transform.GetComponentInParent<CaptureTarget>() && !groundHits[i].transform.IsChildOf(camera.transform))
                { hit=groundHits[i]; nearest=hit.distance; }
            if (nearest < float.PositiveInfinity && hit.normal.y > .8f && (!cycles || hit.point.y > cycles.FallResetHeight))
            { point = hit.point + Vector3.up * Lift; return true; }
            point = default; return false;
        }
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, List<int> indices)
        {
            int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            indices.Add(n); indices.Add(n+1); indices.Add(n+2); indices.Add(n); indices.Add(n+2); indices.Add(n+3);
        }
        void Border(Vector3 a, Vector3 b)
        {
            var side = Vector3.Cross(Vector3.up, b-a).normalized * .023f;
            a += Vector3.up * .005f; b += Vector3.up * .005f; Quad(a-side,a+side,b+side,b-side,edges);
        }
        public void Rebuild()
        {
            vertices.Clear(); faces.Clear(); edges.Clear(); Focused = camera.Target;
            Color color = Focused ? new Color(1,.22f,.12f) : cycles && cycles.IsResearch ? new Color(.2f,.85f,1) : new Color(1,.65f,.12f);
            fill.SetColor("_BaseColor", new Color(color.r,color.g,color.b, Focused ? .45f : .17f));
            outline.SetColor("_BaseColor", new Color(color.r,color.g,color.b,.9f));
            if (Focused) Focus(); else Survey();
            mesh.Clear(); mesh.SetVertices(vertices); mesh.subMeshCount = 2;
            mesh.SetTriangles(faces,0); mesh.SetTriangles(edges,1); mesh.RecalculateBounds();
        }
        void Survey()
        {
            int n = Mathf.CeilToInt(camera.Range*2/Cell), stride = n+1, count = stride*stride;
            if (floor == null || floor.Length != count) { floor = new Vector3[count]; visible = new bool[count]; cells = new bool[n*n]; }
            Vector3 origin = camera.Head.position - new Vector3(n*Cell*.5f,0,n*Cell*.5f);
            for(int z=0;z<=n;z++) for(int x=0;x<=n;x++)
            {
                int i=z*stride+x;
                visible[i]=Ground(origin+new Vector3(x*Cell,0,z*Cell),out floor[i]) && camera.CanSeePosition(floor[i]+Vector3.up*(.95f-Lift),null,true);
            }
            for(int z=0;z<n;z++) for(int x=0;x<n;x++)
            {
                int i=z*stride+x; bool valid=visible[i]&&visible[i+1]&&visible[i+stride]&&visible[i+stride+1];
                if (valid) valid = Mathf.Max(Mathf.Max(floor[i].y,floor[i+1].y),Mathf.Max(floor[i+stride].y,floor[i+stride+1].y)) - Mathf.Min(Mathf.Min(floor[i].y,floor[i+1].y),Mathf.Min(floor[i+stride].y,floor[i+stride+1].y)) < .3f;
                cells[z*n+x]=valid;
                if(valid) Quad(floor[i],floor[i+1],floor[i+stride+1],floor[i+stride],faces);
            }
            for(int z=0;z<n;z++) for(int x=0;x<n;x++) if(cells[z*n+x])
            {
                int i=z*stride+x;
                if(z==0||!cells[(z-1)*n+x]) Border(floor[i],floor[i+1]);
                if(z==n-1||!cells[(z+1)*n+x]) Border(floor[i+stride],floor[i+stride+1]);
                if(x==0||!cells[z*n+x-1]) Border(floor[i],floor[i+stride]);
                if(x==n-1||!cells[z*n+x+1]) Border(floor[i+1],floor[i+stride+1]);
            }
        }
        void Focus()
        {
            Vector3 start=camera.Head.position, end=camera.Target.transform.position;
            Vector3 side=Vector3.Cross(Vector3.up,end-start).normalized;
            int steps=Mathf.Max(2,Mathf.CeilToInt(Vector2.Distance(new Vector2(start.x,start.z),new Vector2(end.x,end.z))/.2f));
            bool prior=false; Vector3 oldLeft=default,oldRight=default;
            for(int i=0;i<=steps;i++)
            {
                float t=(float)i/steps; Vector3 center=Vector3.Lerp(start,end,t); float width=Mathf.Lerp(.035f,.16f,t);
                bool validLeft=Ground(center-side*width,out var left),validRight=Ground(center+side*width,out var right);
                bool valid=validLeft&&validRight&&camera.CanSeePosition((left+right)*.5f+Vector3.up*(.95f-Lift),camera.Target.transform,true);
                if(prior&&valid&&Mathf.Abs(left.y-oldLeft.y)<.3f)
                { Quad(oldLeft,oldRight,right,left,faces); Border(oldLeft,left); Border(oldRight,right); }
                if(valid&&i==steps) Border(left,right);
                prior=valid;oldLeft=left;oldRight=right;
            }
        }
        void OnEnable() { if(visual) visual.enabled=true; nextRefresh=0; }
        void OnDisable() { if(visual) visual.enabled=false; }
        void OnDestroy() { if(mesh) Destroy(mesh); if(fill) Destroy(fill); if(outline) Destroy(outline); }
    }
}
