using TMPro;
using UnityEngine;
namespace EchoGame.UI
{
    public sealed class EchoStatusUI : MonoBehaviour
    {
        public TMP_Text Count, Empty;
        public Transform Content;
        public TMP_Text RowPrefab;
        int previous = -1;
        public void Apply(UISnapshot data)
        {
            int count = data.Echoes.Count;
            Count.text = "● " + count + "/" + data.Capacity;
            if (Empty) Empty.gameObject.SetActive(false);
            if (Content) Content.gameObject.SetActive(false);
            if (previous >= 0 && count > previous)
            {
                var pulse = Count.GetComponent<UIPulse>();
                if (!pulse) pulse = Count.gameObject.AddComponent<UIPulse>();
                pulse.Flash(Count, UITheme.Cyan);
            }
            previous = count;
        }
    }
}
