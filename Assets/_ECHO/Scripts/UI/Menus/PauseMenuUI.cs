using UnityEngine;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class PauseMenuUI : MonoBehaviour
    {
        public UIPanelMotion Motion;
        public Button Resume, Restart, Settings, MainMenu;
        public void Bind(UIManager ui)
        {
            Resume.onClick.AddListener(() => ui.Commands?.Resume());
            Restart.onClick.AddListener(() => ui.Commands?.RestartCycle());
            Settings.onClick.AddListener(ui.OpenSettings);
            MainMenu.onClick.AddListener(() => ui.Commands?.MainMenu());
        }
    }
}
