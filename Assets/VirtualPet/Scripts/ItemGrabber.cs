using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace WindPet
{
    /// <summary>
    /// Lets the player pick up props with the mouse (or touch), like a friendly ghost.
    /// Hover: the prop's ink outline turns white.
    /// Click (press and release quickly, without moving): a tool goes into the item slot.
    /// Hold or move while pressed: the prop lifts and follows the pointer (drag).
    /// Release: it drops with physics and keeps its speed (a throw).
    ///
    /// Depth (method A, "surface drag"): a ray from the pointer hits the desk,
    /// shelf or any Environment surface, and the prop floats just above that point.
    /// Pointing higher on the desk moves it further back, so the 3D room can be
    /// used with a 2D pointer. If nothing is under the pointer, the prop keeps
    /// its depth and moves on a camera-facing plane.
    /// </summary>
    public class ItemGrabber : MonoBehaviour
    {
        public Camera cam;
        public LayerMask itemLayers;
        public LayerMask surfaceLayers;
        [Tooltip("The pet has priority: no grabbing while it is hovered or held.")]
        public PetDrag pet;

        [Header("Carry")]
        [Tooltip("Gap between the surface and the bottom of the held prop.")]
        public float liftHeight = 0.35f;
        public float followStrength = 14f;
        public float maxSpeed = 12f;
        [Tooltip("How quickly a held prop turns back upright.")]
        public float uprightStrength = 6f;
        public float maxThrowSpeed = 8f;

        [Header("Click or drag")]
        [Tooltip("Pressing longer than this starts a drag instead of a click.")]
        public float holdTime = 0.2f;
        [Tooltip("Moving the pointer this many pixels while pressed starts a drag.")]
        public float dragPixels = 10f;

        [Header("Highlight")]
        public Material hoverOutline;
        public Color hoverTint = new Color(1.25f, 1.25f, 1.25f, 1f);
        [Tooltip("How much brighter a held prop gets, so the player sees what they carry.")]
        public float heldBrightness = 1.6f;
        [Tooltip("Small pulse on top of the brightness.")]
        public float heldPulse = 0.1f;
        public float heldPulseSpeed = 5f;

        /// <summary>True while the pointer is over a prop or holding one.</summary>
        public static bool IsBusy { get; private set; }
        /// <summary>True while a prop is held (the pet cannot be grabbed then).</summary>
        public static bool IsBusyWithItem { get; private set; }
        public Rigidbody Held => held;
        public static ItemGrabber Instance { get; private set; }
        public Rigidbody Hovered => hovered;

        /// <summary>Lets go of the held prop at once (for example when a bill is posted).</summary>
        public void ForceRelease()
        {
            if (held != null) Release();
        }

        Rigidbody hovered, held;
        Renderer[] hoverOutlines;
        Material[] savedMaterials;
        Renderer hoverMain;
        MaterialPropertyBlock block;
        Renderer[] heldRenderers;
        float heldHalfHeight;
        float heldDepth;
        Vector3 target;
        readonly RaycastHit[] hits = new RaycastHit[8];
        Rigidbody pressed;          // prop under the pointer when the button went down
        float pressTime;
        Vector2 pressPos;

        void Awake()
        {
            Instance = this;
            if (cam == null) cam = Camera.main;
            block = new MaterialPropertyBlock();
        }

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;
            Ray ray = cam.ScreenPointToRay(pointer.position.ReadValue());

            if (held == null)
            {
                bool petFirst = pet != null && (pet.IsHovered || pet.IsHeld);
                bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                Rigidbody under = null;
                if (!petFirst && !overUI && Physics.Raycast(ray, out RaycastHit hit, 100f, itemLayers, QueryTriggerInteraction.Ignore))
                    under = hit.rigidbody;
                SetHover(under);

                Vector2 pos = pointer.position.ReadValue();
                if (hovered != null && pointer.press.wasPressedThisFrame)
                {
                    // wait to see if this is a click or a drag
                    pressed = hovered;
                    pressTime = Time.unscaledTime;
                    pressPos = pos;
                }
                else if (pressed != null && pointer.press.wasReleasedThisFrame)
                {
                    // quick click: a tool goes into the item slot (swapping the held one),
                    // but with a tool in hand a click on its target uses the tool there
                    var tool = pressed.GetComponent<HoldableItem>();
                    bool target = pressed.GetComponentInParent<Bill>() != null || pressed.GetComponentInParent<InkPad>() != null;
                    pressed = null;
                    var belt = ToolBelt.Instance;
                    if (belt != null && belt.Current != null && (tool == null || target))
                        belt.UseAt(pos);
                    else if (tool != null && belt != null)
                    {
                        ClearHover();
                        belt.Equip(tool);
                    }
                }
                else if (pressed != null && pointer.press.isPressed &&
                         (Time.unscaledTime - pressTime > holdTime || (pos - pressPos).sqrMagnitude > dragPixels * dragPixels))
                {
                    var rb = pressed;
                    pressed = null;
                    SetHover(rb);
                    Grab(rb);
                }
            }
            else
            {
                target = CarryTarget(ray);
                Brighten();
                if (pointer.press.wasReleasedThisFrame) Release();
            }
            IsBusy = hovered != null || held != null;
            IsBusyWithItem = held != null;
        }

        void FixedUpdate()
        {
            if (held == null) return;
            Vector3 v = (target - held.worldCenterOfMass) * followStrength;
            held.linearVelocity = Vector3.ClampMagnitude(v, maxSpeed);
            // turn slowly back upright, so carried bottles do not hang sideways
            var prop = held.GetComponent<PropBody>();
            Quaternion upright = prop != null ? prop.Upright(held.rotation)
                                              : Quaternion.FromToRotation(held.transform.up, Vector3.up) * held.rotation;
            held.MoveRotation(Quaternion.Slerp(held.rotation, upright, uprightStrength * Time.fixedDeltaTime));
            held.angularVelocity *= 0.8f;
        }

        Vector3 CarryTarget(Ray ray)
        {
            int n = Physics.RaycastNonAlloc(ray, hits, 100f, surfaceLayers | itemLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            RaycastHit? surface = null;
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (h.rigidbody == held) continue;               // never stand on yourself
                if (h.distance < best) { best = h.distance; surface = h; }
            }
            if (surface.HasValue)
            {
                var h = surface.Value;
                heldDepth = Vector3.Dot(h.point - cam.transform.position, cam.transform.forward);
                return h.point + Vector3.up * (liftHeight + heldHalfHeight);
            }
            // nothing below the pointer: keep the current depth
            return ray.GetPoint(heldDepth / Mathf.Max(0.01f, Vector3.Dot(ray.direction, cam.transform.forward)));
        }

        void Grab(Rigidbody rb)
        {
            held = rb;
            held.isKinematic = false;
            held.useGravity = false;
            held.WakeUp();
            var b = BoundsOf(rb);
            heldHalfHeight = b.extents.y;
            heldDepth = Vector3.Dot(b.center - cam.transform.position, cam.transform.forward);
            target = held.worldCenterOfMass;
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in rb.GetComponentsInChildren<Renderer>())
                if (!r.name.EndsWith("_Outline")) list.Add(r);
            heldRenderers = list.ToArray();
        }

        void Brighten()
        {
            float k = heldBrightness + Mathf.Sin(Time.time * heldPulseSpeed) * heldPulse;
            foreach (var r in heldRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                // _BaseColor multiplies the baked texture, so values above 1 make it brighter
                Color baseCol = r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor")
                    ? r.sharedMaterial.GetColor("_BaseColor") : Color.white;
                block.SetColor("_BaseColor", new Color(baseCol.r * k, baseCol.g * k, baseCol.b * k, baseCol.a));
                r.SetPropertyBlock(block);
            }
        }

        void Release()
        {
            held.useGravity = true;
            held.linearVelocity = Vector3.ClampMagnitude(held.linearVelocity, maxThrowSpeed);
            foreach (var r in heldRenderers)
                if (r != null) r.SetPropertyBlock(null);
            heldRenderers = null;
            held = null;
            // a prop without an outline keeps its hover tint while the pointer is still on it
            if (hoverMain != null && hovered != null && hoverOutlines != null && hoverOutlines.Length == 0)
            {
                hoverMain.GetPropertyBlock(block);
                block.SetColor("_BaseColor", hoverTint);
                hoverMain.SetPropertyBlock(block);
            }
        }

        void SetHover(Rigidbody rb)
        {
            if (rb == hovered) return;
            ClearHover();
            hovered = rb;
            if (rb == null) return;

            var outlines = new System.Collections.Generic.List<Renderer>();
            foreach (var r in rb.GetComponentsInChildren<Renderer>())
                if (r.name.EndsWith("_Outline")) outlines.Add(r);
            hoverOutlines = outlines.ToArray();
            savedMaterials = new Material[hoverOutlines.Length];
            for (int i = 0; i < hoverOutlines.Length; i++)
            {
                savedMaterials[i] = hoverOutlines[i].sharedMaterial;
                if (hoverOutline != null) hoverOutlines[i].sharedMaterial = hoverOutline;
            }
            // props without an outline mesh get a light tint instead
            hoverMain = rb.GetComponent<Renderer>();
            if (hoverOutlines.Length == 0 && hoverMain != null)
            {
                hoverMain.GetPropertyBlock(block);
                block.SetColor("_BaseColor", hoverTint);
                hoverMain.SetPropertyBlock(block);
            }
        }

        void ClearHover()
        {
            if (hoverOutlines != null)
                for (int i = 0; i < hoverOutlines.Length; i++)
                    if (hoverOutlines[i] != null) hoverOutlines[i].sharedMaterial = savedMaterials[i];
            if (hoverMain != null) hoverMain.SetPropertyBlock(null);
            hoverOutlines = null;
            hoverMain = null;
            hovered = null;
        }

        static Bounds BoundsOf(Rigidbody rb)
        {
            var rs = rb.GetComponentsInChildren<Renderer>();
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(rb.position, Vector3.one * 0.2f);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
