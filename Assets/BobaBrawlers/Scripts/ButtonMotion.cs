using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace BobaBrawlers
{
    public sealed class ButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        bool hovered, selected;
        Button button;
        void Awake() { button = GetComponent<Button>(); }
        public void OnPointerEnter(PointerEventData e) { hovered = true; }
        public void OnPointerExit(PointerEventData e) { hovered = false; }
        public void OnSelect(BaseEventData e) { selected = true; }
        public void OnDeselect(BaseEventData e) { selected = false; }
        void Update()
        {
            bool active = (hovered || selected) && button.interactable && !GamePreferences.ReducedMotion;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * (active ? 1.035f : 1f), Time.unscaledDeltaTime * 16);
        }
    }
}
