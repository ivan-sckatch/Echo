using TMPro;
using UnityEngine;
namespace EchoGame
{
    public sealed class PulseButton : MonoBehaviour, IInteractable
    {
        public string StableId;
        public TimedDoor Door;
        public Level03Controller Level;
        public Renderer Face;
        public Material ReadyMaterial, PressedMaterial;
        public string Prompt => $"Активировать кнопку · дверь на {Door.Window:0.#} секунд";
        public bool CanInteract => Level && Level.Cycles.Game.State == GameState.Playing;
        public int PressCount { get; private set; }
        public void Interact() => Press(Level.Cycles.Elapsed);
        public void Press(float time) { if (!CanInteract) return; PressCount++; Door.Pulse(time); Refresh(time); }
        public void Refresh(float time) { if (Face) Face.sharedMaterial = Door.OpenUntil > time ? PressedMaterial : ReadyMaterial; }
        public void ResetButton() { PressCount = 0; if (Face) Face.sharedMaterial = ReadyMaterial; }
    }
}
