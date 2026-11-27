using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EchoGame.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class UITextButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public TMP_Text Label;
        public Image Marker;
        bool hovered, selected;
        bool wasInteractable;
        Button button;
        void OnEnable() { button = GetComponent<Button>(); Refresh(); }
        void OnDisable() { hovered = selected = false; Refresh(); }
        void LateUpdate()
        {
            if (button && button.IsInteractable() != wasInteractable) Refresh();
        }
        public void OnPointerEnter(PointerEventData data)
        {
            hovered = true;
            if (button && button.IsInteractable() && EventSystem.current) EventSystem.current.SetSelectedGameObject(gameObject);
            Refresh();
        }
        public void OnPointerExit(PointerEventData data) { hovered = false; Refresh(); }
        public void OnSelect(BaseEventData data) { selected = true; Refresh(); }
        public void OnDeselect(BaseEventData data) { selected = false; Refresh(); }
        public void Refresh()
        {
            if (!button) button = GetComponent<Button>();
            bool enabledControl = button && button.IsInteractable();
            wasInteractable = enabledControl;
            if (Marker) Marker.enabled = enabledControl && (hovered || selected);
            if (Label) Label.color = !enabledControl ? new Color(.56f, .6f, .62f, .65f) :
                hovered || selected ? Color.white : new Color(.86f, .89f, .9f);
        }
    }
}
