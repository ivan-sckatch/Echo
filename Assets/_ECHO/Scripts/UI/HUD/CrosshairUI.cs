using UnityEngine;
using UnityEngine.UI;
namespace EchoGame.UI
{
    public sealed class CrosshairUI : MonoBehaviour
    {
        public Image Dot;
        public void SetInteractable(bool available) { Dot.color = available ? UITheme.Cyan : UITheme.Text; }
        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
