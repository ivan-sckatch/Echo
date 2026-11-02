using UnityEngine;

namespace EchoGame.Visuals
{
    public sealed class EchoProjectionArt : MonoBehaviour
    {
        public EchoPlayback Playback;
        public Renderer[] Silhouette;
        public TrailRenderer Trail;
        public Transform ArrivalRing;
        Transform[] scanBands;
        MaterialPropertyBlock properties;
        bool wasActive;
        float age;
        void Awake()
        {
            properties = new MaterialPropertyBlock();
            var bands = new System.Collections.Generic.List<Transform>();
            foreach (var r in Silhouette) if (r && r.name == "Scan band") bands.Add(r.transform);
            scanBands = bands.ToArray();
        }
        void LateUpdate()
        {
            bool active = Playback && Playback.IsActive;
            if (active && !wasActive) { age = 0; if (Trail) Trail.Clear(); }
            if (active) age += Time.deltaTime;
            float reveal = Mathf.Clamp01(age / .5f);
            float pulse = .9f + .1f * Mathf.Sin(Time.time * 3);
            properties.SetColor("_BaseColor", new Color(.27f, .89f, .88f, (.25f + .14f * reveal) * pulse));
            properties.SetColor("_EmissionColor", new Color(.27f, .89f, .88f) * (1.1f + .4f * (1 - reveal)));
            foreach (var r in Silhouette) if (r) r.SetPropertyBlock(properties);
            for (int i = 0; i < scanBands.Length; i++)
            {
                var p = scanBands[i].localPosition; p.x = .008f * Mathf.Sin(Time.time * 6 + i * 1.8f);
                scanBands[i].localPosition = p;
            }
            if (Trail) { Trail.emitting = active; if (!active && wasActive) Trail.Clear(); }
            if (ArrivalRing)
            {
                ArrivalRing.gameObject.SetActive(active && age < .6f);
                ArrivalRing.localScale = Vector3.one * (.3f + age * 1.8f);
            }
            wasActive = active;
        }
    }
}
