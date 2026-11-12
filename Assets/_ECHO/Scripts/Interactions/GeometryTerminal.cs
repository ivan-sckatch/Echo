using UnityEngine;
using TMPro;
namespace EchoGame
{
    public sealed class GeometryTerminal : MonoBehaviour, IInteractable
    {
        public string StableId, Label;
        public int Section;
        public bool PlayerOnly;
        public float Window = 20;
        public Level05Controller Level;
        public Renderer Face;
        public Material IdleMaterial, ActiveMaterial;
        public Material ReadyMaterial;
        public Transform Handle;
        public TMP_Text Countdown;
        [Range(0,3)] public int SequenceOrder;
        public float ActiveUntil { get; private set; } = -1;
        public int Source { get; private set; } = -1;
        public string Prompt => Label;
        public bool CanInteract => Level && Level.Cycles.Game.State == GameState.Playing && Level.ActiveSection == Section;
        public bool IsActive => CanInteract && Level.Cycles.Elapsed < ActiveUntil;
        public float Remaining => IsActive ? ActiveUntil-Level.Cycles.Elapsed : 0;
        public void Interact() => Pulse(0);
        public void Pulse(int source)
        {
            if (!CanInteract || PlayerOnly && source != 0) return;
            if (Handle && IsActive)
            {
                ActiveUntil=-1;Source=-1;
                if (SequenceOrder>0) Level.ResetLiftSequence();
                Refresh();return;
            }
            if (SequenceOrder>0 && !Level.AdvanceLiftSequence(this)) return;
            Source = source; ActiveUntil = Level.Cycles.Elapsed + Window;
            if (source>0) Level.Cycles.MarkEchoUsed(source);
            Refresh();
        }
        public void Refresh()
        {
            if (Face) Face.sharedMaterial = IsActive ? ActiveMaterial :
                SequenceOrder>0 && SequenceOrder==Level.LiftSequenceStep+1 && ReadyMaterial ? ReadyMaterial : IdleMaterial;
            if (Handle) Handle.localRotation=Quaternion.Euler(IsActive ? -125 : -55,0,0);
            if (Countdown)
            {
                Countdown.gameObject.SetActive(IsActive);
                if (IsActive) Countdown.text=Mathf.CeilToInt(Remaining).ToString();
            }
        }
        void LateUpdate()
        {
            if (Countdown && Countdown.gameObject.activeSelf)
                Countdown.transform.rotation=Quaternion.LookRotation(Countdown.transform.position-Level.Cycles.Player.View.position);
        }
        public void ResetTerminal() { Source = -1; ActiveUntil = -1; Refresh(); }
    }
}
