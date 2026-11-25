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
    /// Mouse wheel zooms towards the point under the pointer (Wong, 2026-11-24), between
    /// 'zoomIn' metres closer and 'zoomOut' metres further than the start, measured along the view.
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
        [Header("Zoom (mouse wheel)")]
        [Tooltip("How many metres the camera may move closer than its start position.")]
        public float zoomIn = 16f;
        [Tooltip("How many metres the camera may move further away than its start position.")]
        public float zoomOut = 4f;
        [Tooltip("Metres per wheel notch.")]
        public float zoomStep = 2.5f;
        [Tooltip("How fast the camera glides to the new zoom (higher = snappier).")]
        public float zoomSmooth = 12f;
        [Tooltip("Extra pan room (x, z) when fully zoomed in, so every corner of the board can be reached.")]
        public Vector2 limitZoomedIn = new Vector2(6f, 6f);

        /// <summary>True for one frame when the left button was released without dragging (a click on the board).</summary>
        public static bool Clicked { get; private set; }
        public static bool Dragging { get; private set; }

        Vector3 start;
        bool pressed, onUI;
        Vector2 pressScreen;
        Vector3 grabWorld;
        Camera cam;
        Vector3 goal;      // where the camera glides to (zoom)
        float zoom;        // metres moved along the view direction from the start (+ = closer)

        void Start()
        {
            cam = GetComponent<Camera>();
            start = transform.position;
            goal = start;
        }

        /// <summary>Glides back to the start position and zoom (end of a wave).</summary>
        public void ResetView()
        {
            zoom = 0f;
            goal = start;
            Dragging = false;
            pressed = false;
        }

        /// <summary>0 at the start zoom, 1 when fully zoomed in.</summary>
        public float ZoomAmount => zoomIn > 0f ? Mathf.Clamp01(zoom / zoomIn) : 0f;

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
                    pos.y = start.y + transform.forward.y * zoom;
                    transform.position = goal = ClampPan(pos);
                }
            }
            Zoom(p);
            if (!Dragging) transform.position = Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-zoomSmooth * Time.unscaledDeltaTime));

            if (pressed && pointer.press.wasReleasedThisFrame)
            {
                Clicked = !Dragging && !onUI;
                pressed = false;
                Dragging = false;
            }
        }

        /// <summary>Mouse wheel: move along the ray under the pointer, so the spot under the pointer stays there.</summary>
        void Zoom(Vector2 screen)
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) < 0.01f) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;   // wheel over the HUD
            float notches = Mathf.Clamp(wheel / 120f, -3f, 3f);
            if (Mathf.Abs(wheel) < 10f) notches = Mathf.Clamp(wheel, -3f, 3f);   // some devices send 1 per notch
            float step = notches * zoomStep;
            float next = Mathf.Clamp(zoom + step, -zoomOut, zoomIn);
            step = next - zoom;
            if (Mathf.Abs(step) < 0.0001f) return;
            zoom = next;
            Vector3 dir = cam.ScreenPointToRay(screen).direction;
            // keep the height tied to the zoom (start height minus the forward drop), move x/z towards the pointer
            Vector3 pos = goal + dir * step;
            pos.y = start.y + transform.forward.y * zoom;
            goal = ClampPan(pos);
        }

        Vector3 ClampPan(Vector3 pos)
        {
            Vector2 lim = limit + limitZoomedIn * ZoomAmount;
            // the view centre moves forward when zooming in: allow for it
            Vector3 fwd = transform.forward; fwd.y = 0f;
            Vector3 centre = start + fwd.normalized * (Mathf.Max(0f, zoom) * new Vector2(transform.forward.x, transform.forward.z).magnitude);
            pos.x = Mathf.Clamp(pos.x, centre.x - lim.x, centre.x + lim.x);
            pos.z = Mathf.Clamp(pos.z, centre.z - lim.y, centre.z + lim.y);
            return pos;
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
