using UnityEngine;
using UnityEngine.EventSystems;

namespace BrickPanic
{
    /// <summary>Shows the shared HUD tooltip while the mouse is over this UI element (title + body).</summary>
    public class BPHoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Tooltip title (yellow).")]
        public string title;
        [Tooltip("Tooltip text. Rich text is allowed.")]
        [TextArea] public string body;
        bool over;

        public void OnPointerEnter(PointerEventData e) => over = true;
        public void OnPointerExit(PointerEventData e) => over = false;
        void OnDisable() => over = false;

        void Update()
        {
            if (!over) return;
            var m = UnityEngine.InputSystem.Mouse.current;
            if (m != null) BPTooltip.Show(title, body, m.position.ReadValue());
        }
    }
}
