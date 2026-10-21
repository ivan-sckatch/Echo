using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class LevelCompleteUI : MonoBehaviour
    {
        public UIPanelMotion Motion;
        public TMP_Text Summary;
        public Button Replay, MainMenu;
        public void Bind(UIManager ui)
        { Replay.onClick.AddListener(() => ui.Commands?.ReplayLevel()); MainMenu.onClick.AddListener(() => ui.Commands?.MainMenu()); }
        public void Apply(UISnapshot data)
        { Summary.text = $"{data.Level}\n\nИспользовано Эхо: {data.EchoesUsed}     Попыток: {data.Attempts}"; }
    }
}
