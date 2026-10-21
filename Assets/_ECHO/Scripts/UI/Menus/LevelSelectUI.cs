using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class LevelSelectUI : MonoBehaviour
    {
        public UIPanelMotion Motion;
        public Button FirstLevel, Back;
        public void Bind(UIManager ui)
        {
            FirstLevel.onClick.AddListener(() => SceneManager.LoadScene("Level_01"));
            Back.onClick.AddListener(ui.Back);
        }
    }
}
