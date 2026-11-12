using UnityEngine;
namespace EchoGame
{
    // Diegetic wiring shows the same live signal that drives the mechanisms.
    public sealed class GeometryCircuitLink : MonoBehaviour
    {
        public GeometryTerminal Source;
        public GeometryTerminal SecondSource;
        public PhaseNode NodeSource;
        public LineRenderer Wire;
        public Renderer Indicator;
        public Material IdleMaterial, ActiveMaterial;
        public bool Powered => Source ? Source.IsActive && (!SecondSource || SecondSource.IsActive) : NodeSource && NodeSource.HasActivated;
        void LateUpdate()
        {
            if (Wire) Wire.sharedMaterial=Powered ? ActiveMaterial : IdleMaterial;
            if (Indicator) Indicator.sharedMaterial=Powered ? ActiveMaterial : IdleMaterial;
        }
    }
}
