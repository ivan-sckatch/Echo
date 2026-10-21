using TMPro;
using UnityEngine;
namespace EchoGame.UI
{
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        public TMP_Text Label;
        public UIPanelMotion Motion;
        bool available, allowed;
        public void SetPrompt(bool active, string text)
        { available = active; Label.text = active ? "[F] " + text : ""; Motion.Show(available && allowed); }
        public void SetAllowed(bool visible) { allowed = visible; Motion.Show(available && allowed); }
    }
}
