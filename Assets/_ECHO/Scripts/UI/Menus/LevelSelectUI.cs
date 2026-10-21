using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class LevelSelectUI : MonoBehaviour
    {
        public UIPanelMotion Motion;
        public Button FirstLevel, SecondLevel, Back;
        public void Bind(UIManager ui)
        {
            FirstLevel.onClick.AddListener(() => SceneManager.LoadScene("Level_01"));
            if (SecondLevel) { SecondLevel.gameObject.SetActive(Application.CanStreamedLevelBeLoaded("Level_02")); SecondLevel.onClick.AddListener(() => SceneManager.LoadScene("Level_02")); }
            Back.onClick.AddListener(ui.Back);
        }
    }
}
