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
            string clock = UITheme.Clock(data.Time);
            if (clock != previous) { Clock.text = clock; previous = clock; }
            Cycle.text = "ЦИКЛ " + data.Cycle.ToString("00");
            Clock.color = data.Duration - data.Time <= 5 ? UITheme.Error : UITheme.Text;
        }
    }
}
