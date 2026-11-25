using UnityEngine;
using UnityEngine.InputSystem;

namespace PokerTD
{
    /// <summary>
    /// Edge panning, as in many strategy games: when the pointer stays close to a screen edge,
    /// the camera slides towards that side. The speed grows the closer the pointer is to the
    /// edge, the movement is smoothed, and the camera never goes further than 'limit' from
    /// where it started (x = left/right, y = forward/back on the table). Height and angle stay.
    /// Off while the game window has no focus, so the camera does not drift when Alt-Tabbing.
    /// </summary>
    public class CameraEdgePan : MonoBehaviour
    {
        [Tooltip("Pixels from the screen edge where panning starts.")]
        public float edgeSize = 24f;
        [Tooltip("World units per second at the very edge.")]
        public float speed = 9f;
        [Tooltip("How far the camera may move from its start position (x: left/right, y: forward/back).")]
        public Vector2 limit = new Vector2(6f, 4f);
        [Tooltip("Higher = the camera follows the target faster.")]
        public float smoothing = 6f;

        Vector3 start;
        Vector2 offset, target;

        void Start() => start = transform.position;

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || !Application.isFocused) return;
            Vector2 p = pointer.position.ReadValue();
            // pointer outside the window (Editor Game view): do nothing
            if (p.x < 0f || p.y < 0f || p.x > Screen.width || p.y > Screen.height) { Settle(); return; }

            Vector2 dir = Vector2.zero;
            dir.x = Edge(p.x, Screen.width);
            dir.y = Edge(p.y, Screen.height);
            target += dir * speed * Time.unscaledDeltaTime;
            target.x = Mathf.Clamp(target.x, -limit.x, limit.x);
            target.y = Mathf.Clamp(target.y, -limit.y, limit.y);
            Settle();
        }

        /// <summary>-1..1: how deep the pointer is in the low or high edge band (0 in the middle).</summary>
        float Edge(float v, float size)
        {
            if (v < edgeSize) return -(1f - v / edgeSize);
            if (v > size - edgeSize) return (v - (size - edgeSize)) / edgeSize;
            return 0f;
        }

        void Settle()
        {
            offset = Vector2.Lerp(offset, target, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
            transform.position = start + new Vector3(offset.x, 0f, offset.y);
        }
    }
}
