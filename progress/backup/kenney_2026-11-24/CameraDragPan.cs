using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PokerTD
{
    /// <summary>
    /// Hold and drag the table to move the camera (Wong, 2026-11-24): the point of the table
    /// under the pointer stays under the pointer, like grabbing a map. A press only becomes a
    /// drag after the pointer moves 'dragThreshold' pixels, so a normal click still works.
    /// Presses that start on the HUD (cards, buttons) never drag the camera.
    /// Clicks on the board are reported on release through <see cref="Clicked"/>, so other
    /// scripts (BuildManager, ItemTable) do not build or use something at the start of a drag.
    /// The camera stays within 'limit' of its start position (x: left/right, y: forward/back).
    /// </summary>
    [DefaultExecutionOrder(-50)]   // before BuildManager and ItemTable read Clicked
    public class CameraDragPan : MonoBehaviour
    {
        [Tooltip("Pixels the pointer must move before a press becomes a drag.")]
        public float dragThreshold = 8f;
        [Tooltip("How far the camera may move from its start position (x: left/right, y: forward/back).")]
        public Vector2 limit = new Vector2(8f, 6f);
        [Tooltip("Height of the table plane that is grabbed.")]
        public float tableHeight = 0f;

        /// <summary>True for one frame when the left button was released without dragging (a click on the board).</summary>
        public static bool Clicked { get; private set; }
        public static bool Dragging { get; private set; }

        Vector3 start;
        bool pressed, onUI;
        Vector2 pressScreen;
        Vector3 grabWorld;
        Camera cam;

        void Start()
        {
            cam = GetComponent<Camera>();
            start = transform.position;
        }

        void OnDisable() { Clicked = false; Dragging = false; }

        void Update()
        {
            Clicked = false;
            var pointer = Pointer.current;
            if (pointer == null || cam == null) return;
            Vector2 p = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                pressed = true;
                Dragging = false;
                pressScreen = p;
                onUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            }
            if (pressed && pointer.press.isPressed && !onUI)
            {
                if (!Dragging && (p - pressScreen).magnitude > dragThreshold && TablePoint(pressScreen, out grabWorld))
                    Dragging = true;
                if (Dragging && TablePoint(p, out Vector3 now))
                {
                    // move the camera so the grabbed point is under the pointer again
                    Vector3 pos = transform.position + (grabWorld - now);
                    pos.x = Mathf.Clamp(pos.x, start.x - limit.x, start.x + limit.x);
                    pos.z = Mathf.Clamp(pos.z, start.z - limit.y, start.z + limit.y);
                    pos.y = start.y;
                    transform.position = pos;
                }
            }
            if (pressed && pointer.press.wasReleasedThisFrame)
            {
                Clicked = !Dragging && !onUI;
                pressed = false;
                Dragging = false;
            }
        }

        /// <summary>Where the screen point hits the flat table (a plane at tableHeight).</summary>
        bool TablePoint(Vector2 screen, out Vector3 world)
        {
            var ray = cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, new Vector3(0f, tableHeight, 0f));
            if (plane.Raycast(ray, out float d)) { world = ray.GetPoint(d); return true; }
            world = Vector3.zero;
            return false;
        }
    }
}
