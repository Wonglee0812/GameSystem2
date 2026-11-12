using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace WindPet
{
    /// <summary>
    /// The item slot in the bottom-right corner. It holds one tool at a time.
    /// - Click a tool in the room (handled by ItemGrabber): it disappears
    ///   into the slot and its icon shows here and next to the cursor.
    /// - Tools (fan, dryer): left click makes a puff of wind food at the cursor,
    ///   right click throws the tool.
    /// - Other items: left or right click throws them towards the cursor.
    /// - While a tool is held, props can still be dragged (hold), and a click on
    ///   another tool swaps them.
    /// Hitting the pet with a thrown item lowers its mood. While something is
    /// held, the mouse cursor is hidden and the item's icon takes its place.
    /// - Click the slot: the tool is put down gently in front of the camera.
    /// </summary>
    public class ToolBelt : MonoBehaviour, IPointerClickHandler
    {
        public static ToolBelt Instance { get; private set; }
        public static bool HasTool => Instance != null && Instance.Current != null;

        public Camera cam;
        public Image slotIcon;
        public RectTransform cursorIcon;
        public Vector2 cursorOffset = Vector2.zero;
        public Wallet wallet;
        public LayerMask surfaceLayers;

        [Header("Pet")]
        public PetDrag pet;
        public PetStats stats;

        [Header("Food puffs")]
        public Material puffMaterial;
        public float puffCooldown = 0.2f;
        public int maxPuffs = 8;
        [Tooltip("Puffs stop rising at this height of the screen (0-1).")]
        public float puffMaxViewportY = 0.85f;

        [Header("Throw")]
        public float throwSpeed = 11f;
        [Tooltip("Mood lost when a thrown tool hits the pet.")]
        public float hitMoodPenalty = 15f;

        public HoldableItem Current { get; private set; }
        float nextPuff;
        float shake;
        Vector2 slotBase;
        Image slotImage;
        Color slotColor;

        void Awake()
        {
            Instance = this;
            if (cam == null) cam = Camera.main;
            if (wallet == null) wallet = FindAnyObjectByType<Wallet>();
            slotImage = GetComponent<Image>();
            if (slotImage != null) slotColor = slotImage.color;
            slotBase = ((RectTransform)transform).anchoredPosition;
            ShowIcon(null);
        }

        void Update()
        {
            UpdateShake();
            if (Current == null) return;
            var pointer = Pointer.current;
            if (pointer == null) return;
            Vector2 screen = pointer.position.ReadValue();
            if (cursorIcon != null) cursorIcon.position = screen + cursorOffset;

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (overUI) return;
            bool right = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            // pressing on a prop is handled by ItemGrabber: a quick click uses the
            // tool there (UseAt), holding drags the prop while the tool stays here
            bool overProp = ItemGrabber.Instance != null && ItemGrabber.Instance.Hovered != null;
            if (pointer.press.wasPressedThisFrame && !overProp) UseAt(screen);
            else if (right) Throw(screen);
        }

        void UpdateShake()
        {
            if (shake <= 0f) return;
            shake -= Time.unscaledDeltaTime;
            var rt = (RectTransform)transform;
            float k = Mathf.Max(shake, 0f) / 0.35f;
            rt.anchoredPosition = slotBase + new Vector2(Mathf.Sin(Time.unscaledTime * 60f) * 10f * k, 0f);
            if (slotImage != null) slotImage.color = Color.Lerp(slotColor, new Color(0.8f, 0.1f, 0.1f, slotColor.a), k);
        }

        // ---------- Slot ----------

        public void Equip(HoldableItem tool)
        {
            if (tool == null || tool == Current) return;
            if (Current != null) PlaceBack(Current, tool.transform.position, Vector3.zero);   // swap
            Current = tool;
            var rb = tool.GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            tool.gameObject.SetActive(false);
            ShowIcon(tool.icon);
        }

        /// <summary>Clicking the slot puts the tool down in front of the camera.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Current == null || eventData.button != PointerEventData.InputButton.Left) return;
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.3f, 0f));
            Vector3 spot = Physics.Raycast(ray, out RaycastHit hit, 50f, surfaceLayers) ? hit.point + Vector3.up * 0.8f : ray.GetPoint(5f);
            var tool = Current;
            Clear();
            PlaceBack(tool, spot, Vector3.zero);
        }

        /// <summary>The held item is used up (a potion was drunk): it leaves the slot for good.</summary>
        public void ConsumeCurrent()
        {
            var item = Current;
            Clear();
            if (item != null) Destroy(item.gameObject);
        }

        void Clear()
        {
            Current = null;
            ShowIcon(null);
        }

        void ShowIcon(Sprite icon)
        {
            if (slotIcon != null) { slotIcon.sprite = icon; slotIcon.enabled = icon != null; }
            if (cursorIcon != null)
            {
                var img = cursorIcon.GetComponent<Image>();
                if (img != null) img.sprite = icon;
                cursorIcon.gameObject.SetActive(icon != null);
            }
            // the icon replaces the mouse cursor while an item is held
            Cursor.visible = Current == null;
        }

        static void PlaceBack(HoldableItem tool, Vector3 position, Vector3 velocity)
        {
            var prop = tool.GetComponent<PropBody>();
            Quaternion rotation = prop != null ? prop.RestRotation : tool.transform.rotation;
            tool.transform.SetPositionAndRotation(position, rotation);
            tool.gameObject.SetActive(true);
            var rb = tool.GetComponent<Rigidbody>();
            if (rb == null) return;
            rb.position = position;
            rb.rotation = rotation;
            rb.useGravity = true;
            rb.WakeUp();
            rb.linearVelocity = velocity;
        }

        // ---------- Using the tool ----------

        /// <summary>Point under the cursor on the pet's flying plane, so the pet can reach it.</summary>
        Vector3 OnPetPlane(Vector2 screen)
        {
            float z = pet != null ? pet.transform.position.z : 0f;
            var plane = new Plane(Vector3.back, new Vector3(0f, 0f, z));
            Ray ray = cam.ScreenPointToRay(screen);
            return plane.Raycast(ray, out float d) ? ray.GetPoint(d) : ray.GetPoint(6f);
        }

        /// <summary>Uses the held tool at a screen point. A plain item has no use: it is thrown.</summary>
        public void UseAt(Vector2 screen)
        {
            if (Current != null && !Current.Use(this, screen)) Throw(screen);
        }

        /// <summary>The slot shakes red: "you cannot do that now".</summary>
        public void Shake() => shake = 0.35f;

        /// <summary>A puff of wind food at the cursor. The dryer runs on electricity, or on money when there is none.</summary>
        public void MakePuff(ToolItem tool, Vector2 screen)
        {
            if (Time.time < nextPuff || FoodPuff.All.Count >= maxPuffs) return;
            nextPuff = Time.time + puffCooldown;
            var grid = PowerGrid.Instance;
            bool paid = tool.powerPerPuff <= 0f && tool.costPerPuff <= 0f;
            if (!paid && tool.powerPerPuff > 0f && grid != null && grid.TryUse(tool.powerPerPuff)) paid = true;
            if (!paid && tool.costPerPuff > 0f && wallet != null && wallet.TrySpend(tool.costPerPuff))
            {
                paid = true;
                FloatingText.Show("-€" + tool.costPerPuff.ToString("0.00"), OnPetPlane(screen) + Vector3.up * 0.4f, new Color(0.85f, 0.25f, 0.2f));
            }
            if (!paid) { Shake(); return; }           // no electricity and not enough money
            Vector3 p = OnPetPlane(screen);
            if (pet != null) p.y = Mathf.Max(p.y, pet.floorY + 0.35f);   // never inside the desk
            // highest point the puff may float to at this depth
            float depth = Vector3.Dot(p - cam.transform.position, cam.transform.forward);
            float maxY = cam.ViewportToWorldPoint(new Vector3(0.5f, puffMaxViewportY, depth)).y;
            FoodPuff.Create(p, tool.PuffColor, tool.food, tool.temperatureChange, puffMaterial, maxY);
        }

        void Throw(Vector2 screen)
        {
            var tool = Current;
            Clear();
            // start just inside the bottom-right corner of the view, near the slot
            Vector3 slotScreen = slotIcon != null ? (Vector3)RectTransformUtility.WorldToScreenPoint(null, slotIcon.rectTransform.position) : new Vector3(Screen.width * 0.9f, Screen.height * 0.15f);
            Vector3 start = cam.ScreenPointToRay(slotScreen).GetPoint(3.5f);
            Vector3 target = OnPetPlane(screen);
            // aim a little higher so gravity brings it onto the target
            float time = Vector3.Distance(start, target) / throwSpeed;
            Vector3 velocity = (target - start) / Mathf.Max(time, 0.05f) - 0.5f * Physics.gravity * time;
            PlaceBack(tool, start, velocity);
            var rb = tool.GetComponent<Rigidbody>();
            if (rb != null) rb.angularVelocity = Random.onUnitSphere * 8f;
            Cursor.visible = true;
            var hit = tool.gameObject.AddComponent<ThrownItem>();
            hit.pet = pet;
            hit.stats = stats;
            hit.moodPenalty = hitMoodPenalty;
        }
    }
}
