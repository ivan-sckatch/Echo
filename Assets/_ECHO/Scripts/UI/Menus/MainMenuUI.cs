using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
namespace EchoGame.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        public UIPanelMotion Motion;
        public Button Continue, NewGame, LevelSelect, Settings, Quit;
        public void Bind(UIManager ui)
        {
            Continue.interactable = false; // No progress/save provider exists in this prototype.
            Continue.GetComponentInChildren<TMP_Text>().color = UITheme.Muted * new Color(1, 1, 1, .55f);
            NewGame.onClick.AddListener(() => SceneManager.LoadScene("Level_01"));
            LevelSelect.onClick.AddListener(ui.OpenLevels); Settings.onClick.AddListener(ui.OpenSettings);
            Quit.onClick.AddListener(() => { if (!Application.isEditor) Application.Quit(); });
            Quit.interactable = !Application.isEditor;
            if (Application.isEditor) Quit.GetComponentInChildren<TMP_Text>().color = UITheme.Muted * new Color(1, 1, 1, .55f);
        }
    }
}
