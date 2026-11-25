using UnityEngine;
using UnityEngine.InputSystem;

namespace BrickPanic
{
    /// <summary>Free fly camera. Hold right mouse = look around, WASD = move, Q/E = down/up,
    /// Shift = fast, mouse wheel = change speed. A short right click (no drag) is not used for looking:
    /// it is reported in RightClicked, so the build tool can use it to sell.</summary>
    [DefaultExecutionOrder(-20)]
    public class FlyCamera : MonoBehaviour
    {
        [Tooltip("Move speed in metres per second. Default 12.")]
        public float speed = 12f;
        [Tooltip("Speed multiplier while Shift is held. Default 3.")]
        public float fastMultiplier = 3f;
        [Tooltip("Mouse look speed in degrees per pixel. Default 0.15.")]
        public float lookSpeed = 0.15f;
        [Tooltip("Each wheel notch changes the speed by this factor. Default 1.15.")]
        public float wheelStep = 1.15f;
        [Tooltip("Lowest and highest move speed in metres per second. Default 2 - 80.")]
        public Vector2 speedRange = new Vector2(2f, 80f);
        [Tooltip("Time in seconds to reach the wanted speed (smooth start and stop). Default 0.12.")]
        public float smoothing = 0.12f;

        [Tooltip("Mouse move in pixels before a right-button hold becomes camera look. Default 6.")]
        public float dragThreshold = 6f;
        [Tooltip("True = the mouse wheel changes fly speed. Turn off if the wheel is used for something else. Default true.")]
        public bool wheelChangesSpeed = true;

        /// <summary>True for one frame when the right button was released without dragging (a click).</summary>
        public bool RightClicked { get; private set; }
        /// <summary>True while the right button is held and the camera is turning.</summary>
        public bool Looking { get; private set; }

        float yaw, pitch, dragged;
        Vector3 velocity;

        void OnEnable()
        {
            var e = transform.eulerAngles;
            yaw = e.y;
            pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            RightClicked = false;
            if (mouse.rightButton.wasPressedThisFrame) { dragged = 0f; Looking = false; }
            if (mouse.rightButton.isPressed && !Looking)
            {
                dragged += mouse.delta.ReadValue().magnitude;
                if (dragged > dragThreshold) Looking = true;
            }
            if (mouse.rightButton.wasReleasedThisFrame)
            {
                if (!Looking) RightClicked = true;
                Looking = false;
            }
            Cursor.lockState = Looking ? CursorLockMode.Locked : CursorLockMode.None;
            if (Looking)
            {
                Vector2 d = mouse.delta.ReadValue();
                yaw += d.x * lookSpeed;
                pitch = Mathf.Clamp(pitch - d.y * lookSpeed, -89f, 89f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            float wheel = mouse.scroll.ReadValue().y;
            if (wheelChangesSpeed && Mathf.Abs(wheel) > 0.01f)
                speed = Mathf.Clamp(speed * (wheel > 0f ? wheelStep : 1f / wheelStep), speedRange.x, speedRange.y);

            Vector3 move = Vector3.zero;
            if (kb.wKey.isPressed) move += transform.forward;
            if (kb.sKey.isPressed) move -= transform.forward;
            if (kb.dKey.isPressed) move += transform.right;
            if (kb.aKey.isPressed) move -= transform.right;
            if (kb.eKey.isPressed) move += Vector3.up;
            if (kb.qKey.isPressed) move -= Vector3.up;
            float s = speed * (kb.leftShiftKey.isPressed ? fastMultiplier : 1f);
            Vector3 target = move.normalized * s;
            velocity = Vector3.Lerp(velocity, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.001f, smoothing)));
            transform.position += velocity * Time.unscaledDeltaTime;
        }
    }
}
