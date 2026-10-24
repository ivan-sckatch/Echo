using TMPro;
using UnityEngine;
namespace EchoGame.UI
{
    public sealed class CycleTimerUI : MonoBehaviour
    {
        public TMP_Text Cycle, Clock;
        string previous;
        public void Apply(UISnapshot data)
        {
            bool research = data.IsResearch;
            string clock = research ? "ИССЛЕДОВАНИЕ" : UITheme.Clock(Mathf.Ceil(data.Remaining));
            if (clock != previous) { Clock.text = clock; previous = clock; }
            Cycle.text = research ? "" : "ЦИКЛ " + data.Cycle.ToString("00");
            Clock.fontSize = research ? 24 : 36;
            Clock.color = !research && data.Remaining <= 5 ? UITheme.Error : UITheme.Text;
        }
    }
}
