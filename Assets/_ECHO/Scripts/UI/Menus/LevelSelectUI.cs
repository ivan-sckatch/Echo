using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class LevelSelectUI : MonoBehaviour
    {
        public UIPanelMotion Motion;
        public Button FirstLevel, SecondLevel, ThirdLevel, Back;
        public void Bind(UIManager ui)
        {
            FirstLevel.onClick.AddListener(() => SceneManager.LoadScene("Level_01"));
            if (SecondLevel) { SecondLevel.gameObject.SetActive(Application.CanStreamedLevelBeLoaded("Level_02")); SecondLevel.onClick.AddListener(() => SceneManager.LoadScene("Level_02")); }
            if (ThirdLevel) { ThirdLevel.gameObject.SetActive(Application.CanStreamedLevelBeLoaded("Level_03")); ThirdLevel.onClick.AddListener(() => SceneManager.LoadScene("Level_03")); }
            if (ThirdLevel && Application.CanStreamedLevelBeLoaded("Level_04"))
            {
                var fourth = Instantiate(ThirdLevel, ThirdLevel.transform.parent); fourth.name = "Level 04";
                var rect = fourth.GetComponent<RectTransform>(); rect.anchoredPosition += Vector2.down * 96;
                fourth.onClick = new Button.ButtonClickedEvent(); fourth.onClick.AddListener(() => SceneManager.LoadScene("Level_04"));
                fourth.GetComponentInChildren<TMPro.TMP_Text>().text = "04 · Ложная цель";
                Back.GetComponent<RectTransform>().anchoredPosition += Vector2.down * 60;
                var panel = Back.transform.parent.GetComponent<RectTransform>(); panel.sizeDelta += Vector2.up * 110;
            }
            Back.onClick.AddListener(ui.Back);
        }
    }
}
