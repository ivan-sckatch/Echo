using UnityEngine;

namespace EchoGame
{
    // Driven by the same simulation steps as scanning; pause and reset cannot leave it running.
    public sealed class PhaseNodePresentation : MonoBehaviour
    {
        public Renderer[] Body, Core, Anomaly;
        public Transform[] IrisPivots;
        public Transform LeftGuard, RightGuard;
        public bool Iris;
        public float GuardTravel=.16f;
        public float Reveal { get; private set; }
        public float Opening { get; private set; }
        public float SignalFlash { get; private set; }
        float clock;
        MaterialPropertyBlock properties;
        static readonly int RevealId=Shader.PropertyToID("_Reveal");
        static readonly int ColorId=Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId=Shader.PropertyToID("_EmissionColor");
        public void ResetPresentation(PhaseNode node)
        {
            clock=0; Opening=SignalFlash=0; Reveal=node.InitiallyHidden?0:1; Apply(node);
        }
        public void Step(PhaseNode node,float dt)
        {
            clock+=dt;
            float target=node.IsDiscovered?1:node.StrongestDiscoveryProgress*.85f;
            Reveal=Mathf.MoveTowards(Reveal,target,dt*4);
            float opening=node.IsDiscovered?node.StrongestActivationProgress:0;
            Opening=Mathf.MoveTowards(Opening,opening,dt*4);
            SignalFlash=node.ActivatedThisStep?.35f:Mathf.Max(0,SignalFlash-dt);
            Apply(node);
        }
        void Apply(PhaseNode node)
        {
            if(properties==null)properties=new MaterialPropertyBlock();
            bool signal=node.IsSignaling||SignalFlash>0;
            var coreColor=signal?new Color(.12f,.9f,.47f):new Color(.12f,.76f,.84f);
            Paint(Body,Reveal,Color.white,Color.black,false);
            Paint(Core,Reveal,coreColor,coreColor*(signal?.8f:.12f+.4f*Opening),true);
            // Fine broken contours remain visible before discovery without revealing a solid housing.
            float anomaly=node.InitiallyHidden?(1-Reveal)*(.045f+.025f*(.5f+.5f*Mathf.Sin(clock*2.1f))):0;
            Paint(Anomaly,1,new Color(.26f,.49f,.52f,anomaly),new Color(.05f,.14f,.15f)*anomaly,true);
            if(Iris&&IrisPivots!=null)
                for(int i=0;i<IrisPivots.Length;i++)
                {
                    var pivot=IrisPivots[i];if(!pivot)continue;
                    pivot.localRotation=Quaternion.Euler(0,0,Opening*40);
                    pivot.localPosition=new Vector3(.29f+.07f*Opening,0,-.067f-i*.0015f);
                }
            if(LeftGuard)LeftGuard.localPosition=Vector3.left*GuardTravel*Opening;
            if(RightGuard)RightGuard.localPosition=Vector3.right*GuardTravel*Opening;
        }
        void Paint(Renderer[] renderers,float reveal,Color color,Color emission,bool overrideColor)
        {
            if(renderers==null)return;
            foreach(var renderer in renderers)
            {
                if(!renderer)continue;
                renderer.enabled=reveal>.001f&&(renderers!=Anomaly||color.a>.001f);
                properties.Clear();properties.SetFloat(RevealId,reveal);
                if(overrideColor){properties.SetColor(ColorId,color);properties.SetColor(EmissionId,emission);}
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
