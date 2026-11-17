using UnityEngine;
using UnityEngine.InputSystem;

namespace DamRush
{
    /// <summary>
    /// Top-down build camera like in city builders (Timberborn):
    /// WASD / arrows pan, middle mouse drag rotates, scroll wheel zooms.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public GroundHeights terrain;
        public Camera cam;
        public Vector3 focus = new Vector3(20f, 1f, 12f);
        public float yaw = 0f, pitch = 52f, distance = 26f;
        public float minDistance = 8f, maxDistance = 50f;
        public float panSpeed = 1.1f;   // times the distance, per second
        public float rotateSpeed = 0.25f;

        void LateUpdate()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            float dt = Time.unscaledDeltaTime;

            if (kb != null)
            {
                Vector2 move = Vector2.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                if (move != Vector2.zero)
                {
                    var rot = Quaternion.Euler(0f, yaw, 0f);
                    focus += rot * new Vector3(move.x, 0f, move.y).normalized * panSpeed * distance * 0.5f * dt;
                }
            }

            if (mouse != null)
            {
                if (mouse.middleButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    yaw += d.x * rotateSpeed;
                    pitch = Mathf.Clamp(pitch - d.y * rotateSpeed, 20f, 85f);
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f && !ColonyHud.PointerOverHud(mouse.position.ReadValue())) // scrolling a HUD list must not zoom
                    distance = Mathf.Clamp(distance * (scroll > 0f ? 0.9f : 1.1f), minDistance, maxDistance);
            }

            if (terrain != null)
            {
                focus.x = Mathf.Clamp(focus.x, 0f, terrain.LengthX);
                focus.z = Mathf.Clamp(focus.z, 0f, terrain.WidthZ);
                focus.y = terrain.GroundAt(focus.x, focus.z);
            }

            var look = Quaternion.Euler(pitch, yaw, 0f);
            cam.transform.SetPositionAndRotation(focus - look * Vector3.forward * distance, look);
        }
    }
}
