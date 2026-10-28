using TMPro;
using UnityEngine;
namespace EchoGame
{
    public sealed class PulseButton : MonoBehaviour, IInteractable
    {
        public string StableId;
        public TimedDoor Door;
        public Level03Controller Level;
        public ChamberIsolation Isolation;
        public Renderer Face;
        public Material ReadyMaterial, PressedMaterial;
        public string Prompt => Isolation ? $"Кнопка {Isolation.ChamberName} · изоляция · {Door.DoorLabel}" : $"Активировать кнопку · дверь на {Door.Window:0.#} секунд";
        public bool CanInteract => Level && Level.Cycles.Game.State == GameState.Playing && (!Isolation || !Isolation.Isolated);
        public int PressCount { get; private set; }
        public void Interact() => Press(Level.Cycles.Elapsed);
        public void Press(float time) { if (!CanInteract) return; PressCount++; if (Isolation) Isolation.Lock(); Door.Pulse(time); Refresh(time); }
        public void Refresh(float time) { if (Face) Face.sharedMaterial = Door.IsOpenAt(time) ? PressedMaterial : ReadyMaterial; }
        public void ResetButton() { PressCount = 0; if (Face) Face.sharedMaterial = ReadyMaterial; }
    }
}
