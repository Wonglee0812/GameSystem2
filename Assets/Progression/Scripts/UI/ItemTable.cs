using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PokerTD.UI
{
    /// <summary>
    /// The item cards lie on the table (3D), on Wong's item mat (ItemArea: dark mat with a dashed
    /// line and 4 dashed slots). Each card is a sprite with a box collider; the pointer is checked
    /// with a ray from the camera. Feel (DOTween): pop-in when bought, slow idle bob, lift and tilt
    /// towards the camera on hover with a punch, punch on click. A used item burns away (same
    /// dissolve shader as the world cards). Blocked items are grey; the item waiting for a hand
    /// pick stays lifted. Hover shows the item text as a tooltip.
    /// </summary>
    public class ItemTable : MonoBehaviour
    {
        [Tooltip("The mat on the table (a SpriteRenderer lying flat). Its bounds give the 4 slots.")]
        public SpriteRenderer area;
        public int slots = 4;
        [Tooltip("Slot layout inside the mat as fractions of its width (same numbers as ItemZone.png).")]
        public float firstSlotX = 118.5f / 768f;
        public float slotStepX = 177f / 768f;
        public float cardWidth = 157f / 768f;
        public float hoverLift = 0.5f;

        class Slot
        {
            public Transform root, body;
            public SpriteRenderer sprite;
            public TextMeshPro label;
            public BoxCollider box;
            public TDConfig.ItemType item;
            public bool hovering;
            public MaterialPropertyBlock block;
            public float grey, dissolve;
        }

        readonly List<Slot> cards = new List<Slot>();
        Material material;
        Slot hovered;
        static readonly int GreyId = Shader.PropertyToID("_Grey");
        static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
        static readonly int FlashId = Shader.PropertyToID("_Flash");

        void Start()
        {
            if (area == null)
            {
                var go = GameObject.Find("ItemArea");
                if (go != null) area = go.GetComponent<SpriteRenderer>();
            }
            material = Resources.Load<Material>("M_CardDissolve");
            for (int i = 0; i < slots; i++) cards.Add(null);
        }

        /// <summary>World position and size of slot i, from the mat's bounds (the mat lies flat).</summary>
        void SlotPose(int i, out Vector3 pos, out float width)
        {
            var b = area.bounds;
            float x = b.min.x + b.size.x * (firstSlotX + slotStepX * i);
            pos = new Vector3(x, b.max.y + 0.03f, b.center.z);
            width = b.size.x * cardWidth;
        }

        void Update()
        {
            var game = TDGame.Instance;
            if (game == null || area == null) return;

            while (cards.Count < game.config.itemSlots) cards.Add(null);
            for (int i = 0; i < cards.Count; i++)
            {
                var item = i < game.Items.Count ? game.Items[i] : null;
                var c = cards[i];
                if (c != null && c.item != item) { Remove(c); cards[i] = c = null; }
                if (c == null && item != null) cards[i] = c = Create(i, item);
                if (c == null) continue;
                bool blocked = game.ItemBlock(i) != null;
                SetFloat(c, GreyId, ref c.grey, blocked ? 1f : 0f);
            }
            UpdatePointer(game);
            for (int i = 0; i < cards.Count; i++) if (cards[i] != null) Animate(cards[i], i, game.PickingHandForItem == i);
            if (game.PickingHandForItem >= 0 && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) game.CancelHandPick();
        }

        Slot Create(int i, TDConfig.ItemType item)
        {
            SlotPose(i, out Vector3 pos, out float width);
            var s = new Slot { item = item, block = new MaterialPropertyBlock() };
            s.root = new GameObject("ItemCard_" + item.name).transform;
            s.root.SetParent(transform, true);
            s.root.position = pos;
            s.body = new GameObject("Body").transform;
            s.body.SetParent(s.root, false);
            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(s.body, false);
            spriteGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // face up, like the mat
            s.sprite = spriteGo.AddComponent<SpriteRenderer>();
            s.sprite.sprite = UiKit.ItemFace;
            if (material != null) s.sprite.sharedMaterial = material;
            s.sprite.sortingOrder = 5;
            float scale = width / (s.sprite.sprite != null ? s.sprite.sprite.bounds.size.x : 1f);
            spriteGo.transform.localScale = Vector3.one * scale;
            s.box = s.root.gameObject.AddComponent<BoxCollider>();
            var sb = s.sprite.bounds;
            s.box.size = new Vector3(sb.size.x, 0.1f, sb.size.z);
            // the item name on the card's lower part
            s.label = new GameObject("Name").AddComponent<TextMeshPro>();
            s.label.transform.SetParent(s.body, false);
            s.label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            s.label.transform.localPosition = new Vector3(0f, 0.02f, -sb.size.z * 0.5f - 0.35f);
            s.label.rectTransform.sizeDelta = new Vector2(width * 1.3f, 0.6f);
            UiKit.SetFont(s.label, UiKit.Number, 0.18f);
            s.label.fontSize = 3.2f;
            s.label.alignment = TextAlignmentOptions.Center;
            s.label.textWrappingMode = TextWrappingModes.NoWrap;
            s.label.color = UiKit.Cream;
            s.label.text = item.name.ToUpper();
            // pop in
            s.body.localScale = Vector3.zero;
            s.body.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetLink(s.root.gameObject);
            s.body.DOPunchRotation(new Vector3(0f, 20f, 0f), 0.45f, 8, 0.8f).SetLink(s.root.gameObject);
            return s;
        }

        void Remove(Slot s)
        {
            if (hovered == s) { hovered = null; UiKit.HideTip(); }
            s.box.enabled = false;
            s.root.name += " (used)";
            var seq = DOTween.Sequence().SetLink(s.root.gameObject);
            seq.Append(s.body.DOLocalMoveY(1.2f, 0.25f).SetEase(Ease.OutQuad));
            seq.Join(s.body.DOShakeRotation(0.3f, new Vector3(0f, 25f, 0f), 30));
            seq.Append(DOTween.To(() => s.dissolve, v => SetFloat(s, DissolveId, ref s.dissolve, v), 1f, 0.45f));
            seq.Join(DOTween.To(() => s.label.alpha, a => s.label.alpha = a, 0f, 0.3f));
            seq.OnComplete(() => Destroy(s.root.gameObject));
        }

        void UpdatePointer(TDGame game)
        {
            Slot hit = null;
            var cam = Camera.main;
            var pointer = Pointer.current;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (cam != null && pointer != null && !overUI &&
                Physics.Raycast(cam.ScreenPointToRay(pointer.position.ReadValue()), out RaycastHit h, 200f))
                foreach (var c in cards) if (c != null && h.collider == c.box) hit = c;

            if (hit != hovered)
            {
                if (hovered != null) hovered.hovering = false;
                hovered = hit;
                if (hovered != null)
                {
                    hovered.hovering = true;
                    hovered.body.DOComplete();
                    hovered.body.DOPunchRotation(new Vector3(0f, 6f, 0f), 0.2f, 20, 1f).SetLink(hovered.root.gameObject);
                }
                else UiKit.HideTip();
            }
            if (hovered == null) return;
            int index = cards.IndexOf(hovered);
            string block = game.ItemBlock(index);
            bool picking = game.PickingHandForItem == index;
            string sub = picking ? "<color=#FFD860>Click a hand in COMBOS (Esc cancels).</color>"
                       : block != null ? $"<color=#FF7060>{block}</color>" : hovered.item.description;
            UiKit.ShowTip($"<b>{hovered.item.name.ToUpper()}</b>\n<size=85%>{sub}</size>", pointer.position.ReadValue());
            if (CameraDragPan.Clicked)   // on release, not at the start of a camera drag
            {
                hovered.body.DOComplete();
                hovered.body.DOPunchScale(Vector3.one * 0.15f, 0.25f, 8, 0.8f).SetLink(hovered.root.gameObject);
                Flash(hovered);
                game.UseItem(index);
            }
        }

        void Animate(Slot s, int i, bool picking)
        {
            float t = Time.unscaledTime + i * 1.7f;
            bool up = s.hovering || picking;
            float lift = up ? hoverLift : Mathf.Sin(t * 1.3f) * 0.04f + 0.04f;
            var target = new Vector3(0f, lift, 0f);
            s.body.localPosition = Vector3.Lerp(s.body.localPosition, target, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            // tilt towards the camera when lifted; a small idle wobble otherwise
            Quaternion rot = up ? Quaternion.Euler(-22f, 0f, 0f) : Quaternion.Euler(Mathf.Sin(t) * 2f, 0f, Mathf.Cos(t * 0.9f) * 2f);
            s.root.rotation = Quaternion.Slerp(s.root.rotation, rot, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        }

        void Flash(Slot s)
        {
            float f = 0.8f;
            SetFloat(s, FlashId, ref f, 0.8f);
            DOTween.To(() => f, v => SetFloat(s, FlashId, ref f, v), 0f, 0.25f).SetLink(s.root.gameObject);
        }

        static void SetFloat(Slot s, int id, ref float store, float value)
        {
            store = value;
            if (s.sprite == null) return;
            s.sprite.GetPropertyBlock(s.block);
            s.block.SetFloat(id, value);
            s.sprite.SetPropertyBlock(s.block);
        }
    }
}
