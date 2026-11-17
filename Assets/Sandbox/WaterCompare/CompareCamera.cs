using UnityEngine;
using UnityEngine.InputSystem;

namespace DamRush.WaterCompare
{
    /// <summary>Simple free camera: WASD/arrows move, right mouse rotates, wheel zooms.</summary>
    public class CompareCamera : MonoBehaviour
    {
        public Vector3 target = new Vector3(32f, 0f, 80f);
        public float distance = 120f, yaw = -90f, pitch = 55f;
        Vector3 allTarget; float allDistance;

        void Start() { allTarget = target; allDistance = distance; Apply(); }

        public void Focus(Vector3 p) { target = p; distance = 45f; }
        public void ViewAll() { target = allTarget; distance = allDistance; }

        void LateUpdate()
        {
            var kb = Keyboard.current; var m = Mouse.current;
            float dt = Time.unscaledDeltaTime;
            if (kb != null)
            {
                Vector3 f = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, r = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                Vector3 move = Vector3.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move += f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move -= f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move += r;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move -= r;
                target += move * distance * 0.8f * dt;
            }
            if (m != null)
            {
                if (m.rightButton.isPressed) { var d = m.delta.ReadValue(); yaw += d.x * 0.2f; pitch = Mathf.Clamp(pitch - d.y * 0.2f, 15f, 85f); }
                float wheel = m.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f) distance = Mathf.Clamp(distance * (wheel > 0 ? 0.9f : 1.1f), 8f, 220f);
            }
            Apply();
        }

        void Apply()
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = target - transform.forward * distance;
        }
    }
}
