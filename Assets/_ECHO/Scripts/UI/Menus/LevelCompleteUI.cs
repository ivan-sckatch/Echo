using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class LevelCompleteUI : MonoBehaviour
    {
        public UIPanelMotion Motion;
        public TMP_Text Summary;
        public Button Replay, MainMenu, NextLevel;
        public void Bind(UIManager ui)
        { Replay.onClick.AddListener(() => ui.Commands?.ReplayLevel()); MainMenu.onClick.AddListener(() => ui.Commands?.MainMenu()); if (NextLevel) NextLevel.onClick.AddListener(() => ui.Commands?.NextLevel()); }
        public void Apply(UISnapshot data)
        {
            Summary.text = $"{data.Level}\n\nИспользовано Эхо: {data.EchoesUsed}\nВременных циклов: {data.Cycle}";
            if (NextLevel) NextLevel.gameObject.SetActive(!string.IsNullOrEmpty(data.NextScene) && Application.CanStreamedLevelBeLoaded(data.NextScene));
        }
    }
}
