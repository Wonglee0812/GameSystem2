using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BrickPanic
{
    /// <summary>Sandcastle-style building: a preview brick snaps to the stud grid on whatever is under the mouse.
    /// Keys: Tab bricks/people, 1-4 material, Z / X shape, R rotate, left click place, short right click sell
    /// (people: full price back), middle click pick up (move for free), Ctrl+Z undo.</summary>
    public class BuildController : MonoBehaviour
    {
        [Header("Catalogue")]
        [Tooltip("Shapes the player can build (Z / X = previous / next).")]
        public ShapeDef[] shapes;
        [Tooltip("Materials the player can build with (keys 1-4).")]
        public MaterialDef[] materials;

        [Header("People")]
        [Tooltip("Person prefab placed in People mode (Tab).")]
        public Person personPrefab;
        [Tooltip("Price of one person in coins. Default 5.")]
        public float personPrice = 5f;
        [Tooltip("Parent object for people.")]
        public Transform peopleRoot;
        [Tooltip("Chance weights for the random type of a placed person, in this order: kid, adult, elder, athlete. Default 25, 40, 20, 15.")]
        public float[] personTypeWeights = { 25f, 40f, 20f, 15f };

        [Header("Economy (placeholder until the round system)")]
        [Tooltip("Money in coins. Default 100.")]
        public float money = 100f;
        [Tooltip("Part of the price you get back when you sell a brick, 0-1. Default 0.5.")]
        [Range(0f, 1f)] public float sellRefund = 0.5f;

        [Header("Preview")]
        [Tooltip("See-through material for the preview brick (M_BP_Ghost).")]
        public Material ghostMaterial;
        [Tooltip("Preview colour when the brick can be placed.")]
        public Color okColor = new Color(0.3f, 1f, 0.4f, 0.45f);
        [Tooltip("Preview colour when it cannot be placed (blocked or too expensive).")]
        public Color badColor = new Color(1f, 0.3f, 0.3f, 0.45f);

        [Header("Aim")]
        [Tooltip("Camera used for aiming. Default: Main Camera.")]
        public Camera cam;
        [Tooltip("Camera script; its short right click is used to sell.")]
        public FlyCamera flyCamera;
        [Tooltip("Longest aim distance in metres. Default 250.")]
        public float maxDistance = 250f;
        [Tooltip("Parent object for placed bricks.")]
        public Transform blocksRoot;

        [Tooltip("False during a disaster: no building.")]
        public bool buildEnabled = true;

        public enum Mode { Bricks, People }
        public Mode CurrentMode { get; private set; }

        public ShapeDef Shape => shapes[shapeIndex];
        public MaterialDef Material => materials[materialIndex];
        public bool Carrying => carrying;
        public int MaterialIndex => materialIndex;
        public int ShapeIndex => shapeIndex;
        public int Turns => turns;

        int shapeIndex, materialIndex, turns;
        bool carrying, carryPaid, valid;
        Vector3 placePos;
        GameObject ghost;
        MeshFilter ghostFilter;
        MeshRenderer ghostRenderer;
        MaterialPropertyBlock mpb;
        Mesh personGhostMesh;
        readonly List<Block> history = new List<Block>();

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (blocksRoot == null) blocksRoot = new GameObject("PlayerBricks").transform;
            ghost = new GameObject("BuildPreview");
            ghostFilter = ghost.AddComponent<MeshFilter>();
            ghostRenderer = ghost.AddComponent<MeshRenderer>();
            ghostRenderer.sharedMaterial = ghostMaterial;
            ghostRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mpb = new MaterialPropertyBlock();
            if (peopleRoot == null) peopleRoot = new GameObject("People").transform;
            var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            personGhostMesh = cap.GetComponent<MeshFilter>().sharedMesh;
            Destroy(cap);
            shapeIndex = Mathf.Clamp(System.Array.FindIndex(shapes, s => s != null && s.displayName.Contains("2X4") && s.kind == ShapeKind.Brick), 0, shapes.Length - 1);
        }

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null || cam == null) return;
            if (!buildEnabled || MapBuildIn.Busy || BrickPanicHud.PopupOpen) { ghost.SetActive(false); return; }

            ReadKeys(kb);
            if (CurrentMode == Mode.People) AimPerson(mouse.position.ReadValue());
            else Aim(mouse.position.ReadValue());

            bool looking = flyCamera != null && flyCamera.Looking;
            // the mouse over the HUD (hotbar, buttons) must not build or sell in the world behind it
            bool overUi = !looking && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            ghost.SetActive(!looking && !overUi && ghostFilter.sharedMesh != null && aimed);

            if (looking || overUi) return;
            if (mouse.leftButton.wasPressedThisFrame && valid) { if (CurrentMode == Mode.People) PlacePerson(); else Place(); }
            if (mouse.middleButton.wasPressedThisFrame) PickUp(mouse.position.ReadValue());
            bool sellClick = flyCamera != null ? flyCamera.RightClicked : mouse.rightButton.wasReleasedThisFrame;
            if (sellClick) Sell(mouse.position.ReadValue());
        }

        void ReadKeys(Keyboard kb)
        {
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            if (ctrl && kb.zKey.wasPressedThisFrame) { Undo(); return; }
            for (int k = 0; k < materials.Length && k < 4; k++)
                if (kb[Key.Digit1 + k].wasPressedThisFrame) { DropCarry(); materialIndex = k; }
            if (!ctrl && kb.zKey.wasPressedThisFrame) { DropCarry(); shapeIndex = (shapeIndex - 1 + shapes.Length) % shapes.Length; }
            if (kb.xKey.wasPressedThisFrame) { DropCarry(); shapeIndex = (shapeIndex + 1) % shapes.Length; }
            if (kb.rKey.wasPressedThisFrame) turns = (turns + 1) % 4;
            if (kb.tabKey.wasPressedThisFrame) { DropCarry(); CurrentMode = CurrentMode == Mode.Bricks ? Mode.People : Mode.Bricks; }
        }

        /// <summary>Cancel a free move: the picked-up brick is sold back at its full price if it was paid.</summary>
        void DropCarry()
        {
            if (!carrying) return;
            carrying = false;
            if (carryPaid) money += Shape.Price(Material);
        }

        bool aimed;

        void Aim(Vector2 screen)
        {
            aimed = false;
            valid = false;
            var ray = cam.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out var hit, maxDistance, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore)) return;

            var shape = Shape;
            bool swap = turns % 2 == 1;
            float s = BrickGrid.Stud;
            float fx = (swap ? shape.studs.y : shape.studs.x) * s;
            float fz = (swap ? shape.studs.x : shape.studs.y) * s;
            float h = shape.Height;

            // step out of the surface a little, so aiming at a wall side puts the brick next to the wall
            Vector3 p = hit.point + hit.normal * 0.25f;
            float cx = Mathf.Round((p.x - fx * 0.5f) / s) * s + fx * 0.5f;
            float cz = Mathf.Round((p.z - fz * 0.5f) / s) * s + fz * 0.5f;

            // find the highest surface under the whole footprint
            var ext = new Vector3(fx * 0.5f - 0.04f, 0.02f, fz * 0.5f - 0.04f);
            if (!Physics.BoxCast(new Vector3(cx, p.y + 0.4f, cz), ext, Vector3.down, out var down, Quaternion.identity, 200f, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore)) return;
            float y = Mathf.Round(down.point.y / BrickGrid.PlateHeight) * BrickGrid.PlateHeight;
            placePos = new Vector3(cx, y, cz);
            aimed = true;

            bool free = !Physics.CheckBox(placePos + Vector3.up * (h * 0.5f + 0.02f), new Vector3(fx * 0.5f - 0.05f, h * 0.5f - 0.04f, fz * 0.5f - 0.05f),
                                          Quaternion.identity, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore);
            bool afford = carrying || money >= shape.Price(Material);
            valid = free && afford;

            ghostFilter.sharedMesh = shape.mesh;
            ghost.transform.localScale = Vector3.one;
            ghost.transform.SetPositionAndRotation(placePos, Quaternion.Euler(0f, 90f * turns, 0f));
            mpb.SetColor("_BaseColor", valid ? okColor : badColor);
            ghostRenderer.SetPropertyBlock(mpb);
        }

        /// <summary>People mode: a person can stand on any flat-enough surface with room for the body.</summary>
        void AimPerson(Vector2 screen)
        {
            aimed = false;
            valid = false;
            var ray = cam.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out var hit, maxDistance, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore)) return;
            placePos = hit.point;
            aimed = true;
            bool flat = hit.normal.y > 0.7f;
            bool free = !Physics.CheckCapsule(placePos + Vector3.up * 0.4f, placePos + Vector3.up * 1.5f, 0.28f, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore);
            valid = flat && free && money >= personPrice && personPrefab != null;
            ghostFilter.sharedMesh = personGhostMesh;
            ghost.transform.SetPositionAndRotation(placePos + Vector3.up * 0.9f, Quaternion.identity);
            ghost.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            mpb.SetColor("_BaseColor", valid ? okColor : badColor);
            ghostRenderer.SetPropertyBlock(mpb);
        }

        void PlacePerson()
        {
            var p = Instantiate(personPrefab, placePos + Vector3.up * 0.02f, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), peopleRoot);
            p.SetType(RollPersonType());
            money -= personPrice;
        }

        void Place()
        {
            float price = Shape.Price(Material);
            var b = BlockFactory.Create(Shape, Material, placePos, turns, blocksRoot);
            DropIn(b);
            b.placedByPlayer = true;
            b.paid = carrying ? carryPaid : true;
            if (!carrying) money -= price;
            carrying = false;
            history.Add(b);
        }

        /// <summary>Random person type by the weights (kid, adult, elder, athlete).</summary>
        public Person.Kind RollPersonType()
        {
            float total = 0f;
            for (int i = 0; i < personTypeWeights.Length && i < 4; i++) total += Mathf.Max(0f, personTypeWeights[i]);
            if (total <= 0f) return Person.Kind.Adult;
            float r = Random.value * total;
            for (int i = 0; i < personTypeWeights.Length && i < 4; i++)
            {
                r -= Mathf.Max(0f, personTypeWeights[i]);
                if (r <= 0f) return (Person.Kind)i;
            }
            return Person.Kind.Adult;
        }

        Block BlockUnder(Vector2 screen)
        {
            var ray = cam.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out var hit, maxDistance, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore)) return null;
            return hit.collider.GetComponent<Block>();
        }

        void Sell(Vector2 screen)
        {
            var ray = cam.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out var hit, maxDistance, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore))
            {
                var person = hit.collider.GetComponentInParent<Person>();
                if (person != null) { money += personPrice; Destroy(person.gameObject); return; }
            }
            var b = BlockUnder(screen);
            if (b == null) return;
            money += Mathf.Round(b.Price * sellRefund);
            history.Remove(b);
            BrickFx.Break(b, 0.6f);
            Destroy(b.gameObject);
        }

        [Header("Juice")]
        [Tooltip("A placed brick drops in from this height (m) with a squash and a dust ring. 0 = no animation. Default 0.6.")]
        public float dropHeight = 0.6f;

        /// <summary>Placed brick: only the mesh drops in and squashes (the collider is in place at once, so stacking works).</summary>
        void DropIn(Block b)
        {
            if (dropHeight <= 0f || !BPJuice.On) return;
            var mr = b.GetComponent<MeshRenderer>();
            var mf = b.GetComponent<MeshFilter>();
            if (mr == null || mf == null) return;
            var vis = new GameObject("Visual");
            vis.transform.SetParent(b.transform, false);
            vis.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var vr = vis.AddComponent<MeshRenderer>();
            vr.sharedMaterial = mr.sharedMaterial;
            mr.enabled = false;
            vis.transform.localPosition = Vector3.up * dropHeight;
            float w = Mathf.Max(b.shape.BodySize.x, b.shape.BodySize.z);
            var seq = DOTween.Sequence().SetLink(vis);
            seq.Append(vis.transform.DOLocalMove(Vector3.zero, 0.12f).SetEase(Ease.InQuad));
            seq.AppendCallback(() => { if (b != null) BrickFx.Land(b.transform.position, w); });
            seq.Append(vis.transform.DOScale(new Vector3(1.12f, 0.8f, 1.12f), 0.06f));
            seq.Append(vis.transform.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack));
            seq.OnComplete(() => { if (mr != null) mr.enabled = true; if (vis != null) Destroy(vis); });
            seq.OnKill(() => { if (mr != null) mr.enabled = true; });
        }

        /// <summary>Middle click: take a brick (player brick or map piece) and place it again for free.</summary>
        void PickUp(Vector2 screen)
        {
            var b = BlockUnder(screen);
            if (b == null) return;
            DropCarry();
            int si = System.Array.IndexOf(shapes, b.shape);
            int mi = System.Array.IndexOf(materials, b.material);
            if (si < 0 || mi < 0) return;   // not in the catalogue (cannot be rebuilt the same way)
            shapeIndex = si;
            materialIndex = mi;
            turns = b.quarterTurns;
            carrying = true;
            carryPaid = b.paid;
            history.Remove(b);
            Destroy(b.gameObject);
        }

        /// <summary>HUD: pick a material (also switches to brick mode).</summary>
        public void SelectMaterial(int index)
        {
            if (index < 0 || index >= materials.Length) return;
            DropCarry();
            materialIndex = index;
            CurrentMode = Mode.Bricks;
        }

        /// <summary>HUD: pick a shape (also switches to brick mode).</summary>
        public void SelectShape(int index)
        {
            if (index < 0 || index >= shapes.Length) return;
            DropCarry();
            shapeIndex = index;
            CurrentMode = Mode.Bricks;
        }

        /// <summary>HUD: bricks or people.</summary>
        public void SetMode(Mode mode)
        {
            if (mode == CurrentMode) return;
            DropCarry();
            CurrentMode = mode;
        }

        /// <summary>Clears the undo list (new map).</summary>
        public void ResetBuild() { history.Clear(); carrying = false; buildEnabled = true; CurrentMode = Mode.Bricks; }

        /// <summary>Start of a build phase: building on, undo history empty (bricks of earlier rounds cannot be undone).</summary>
        public void BeginBuildPhase() { history.Clear(); carrying = false; buildEnabled = true; }

        /// <summary>End of a build phase: a brick still being moved is paid back, then building is off.</summary>
        public void EndBuildPhase() { DropCarry(); history.Clear(); buildEnabled = false; }

        void Undo()
        {
            for (int i = history.Count - 1; i >= 0; i--)
            {
                var b = history[i];
                history.RemoveAt(i);
                if (b == null) continue;
                if (b.paid) money += b.Price;
                BrickFx.Break(b, 0.5f);
                Destroy(b.gameObject);
                return;
            }
        }
    }
}
