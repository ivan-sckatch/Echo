using System;
using UnityEngine;
namespace EchoGame
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract { get; }
        void Interact();
    }
    // Gameplay owns target acquisition. The UI only receives TargetChanged.
    public sealed class PlayerInteraction : MonoBehaviour
    {
        public GameManager Game;
        public Camera View;
        [Min(.1f)] public float Range = 3;
        public LayerMask Mask = ~0;
        public event Action<bool, string> TargetChanged;
        public event Action<string> InteractionPerformed;
        IInteractable target;
        float nextCheck;
        void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .1f;
            IInteractable next = null;
            if (Game.State == GameState.Playing && Physics.Raycast(View.transform.position, View.transform.forward, out var hit, Range, Mask, QueryTriggerInteraction.Ignore))
                next = hit.collider.GetComponentInParent<IInteractable>();
            if (next != null && !next.CanInteract) next = null;
            if (ReferenceEquals(next, target)) return;
            target = next; TargetChanged?.Invoke(target != null, target != null ? target.Prompt : "");
        }
        public void TryInteract()
        {
            if (Game.State != GameState.Playing || target == null || !target.CanInteract) return;
            string prompt = target.Prompt; target.Interact(); InteractionPerformed?.Invoke(prompt);
        }
    }
}
