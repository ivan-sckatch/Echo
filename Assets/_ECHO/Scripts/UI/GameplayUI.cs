using UnityEngine;
namespace EchoGame
{
    // Retained as a compatibility entry point for older scenes and cycle references.
    // All rendering now belongs to the uGUI prefab and its UIManager.
    public sealed class GameplayUI : MonoBehaviour
    {
        public TimeCycleManager Cycles;
        public GameManager Game;
        public UI.UIEventBridge Bridge;
        public void Hint(string text, float seconds = 8) { if (Bridge) Bridge.PushNotice(text, seconds); }
    }
}
