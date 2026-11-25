using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BrickPanic
{
    /// <summary>Sandcastle-style building: a preview brick snaps to the stud grid on whatever is under the mouse.
    /// Two tools (Wong, 2026-11-25): BUILD (V) - click places one brick, drag draws a straight line of bricks along X or Z
    /// (drawing over a line stacks a new row on top); ERASE (C) - click removes one brick or person, drag selects an area
    /// and removes everything in it. Keys: Tab bricks/people, 1-4 material, Z / X shape, R rotate, middle click pick up
    /// (move for free), short right click sell one, Ctrl+Z undo (a whole line at once).</summary>
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

        public enum Tool { Build, Erase }
        public Tool CurrentTool { get; private set; }

        [Header("Line build and area erase")]
        [Tooltip("Most bricks in one drawn line. Default 40.")]
        public int maxLine = 40;
        [Tooltip("Most bricks and people one area erase can remove. Default 250.")]
        public int maxErase = 250;
        [Tooltip("Colour of the erase preview.")]
        public Color eraseColor = new Color(1f, 0.25f, 0.2f, 0.5f);
        [Tooltip("Seconds between two bricks of a line dropping in (a quick wave). Default 0.025.")]
        public float lineStagger = 0.025f;

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
        readonly List<List<Block>> history = new List<List<Block>>();   // one entry per click or drawn line
        readonly List<MeshRenderer> ghosts = new List<MeshRenderer>();
        readonly List<Vector3> linePos = new List<Vector3>();
        readonly List<bool> lineOk = new List<bool>();
        readonly List<Block> eraseBlocks = new List<Block>();
        readonly List<Person> erasePeople = new List<Person>();
        bool dragging;
        Vector3 dragStart, dragEnd;
        Mesh boxGhostMesh;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            if (blocksRoot == null) blocksRoot = new GameObject("PlayerBricks").transform;
            ghost = new GameObject("BuildPreview");
            ghostFilter = ghost.AddComponent<MeshFilter>();
            ghostRenderer = ghost.AddComponent<MeshRenderer>();
            ghostRenderer.sharedMaterial = ghostMaterial;
            ghostRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ghosts.Add(ghostRenderer);
            mpb = new MaterialPropertyBlock();
            boxGhostMesh = MapGenerator.BoxMesh();
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
            if (!buildEnabled || MapBuildIn.Busy || BrickPanicHud.PopupOpen) { HideGhosts(0); dragging = false; return; }

            ReadKeys(kb);
            Vector2 screen = mouse.position.ReadValue();
            bool looking = flyCamera != null && flyCamera.Looking;
            // the mouse over the HUD (buttons, panels) must not build or erase in the world behind it
            bool overUi = !looking && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (looking || (overUi && !dragging)) { HideGhosts(0); return; }

            if (CurrentTool == Tool.Erase) EraseUpdate(mouse, screen);
            else if (CurrentMode == Mode.People)
            {
                AimPerson(screen);
                ghost.SetActive(aimed);
                HideGhosts(1);
                if (mouse.leftButton.wasPressedThisFrame && valid) PlacePerson();
            }
            else BuildUpdate(mouse, screen);

            if (dragging) return;
            if (CurrentTool == Tool.Build && mouse.middleButton.wasPressedThisFrame) PickUp(screen);
            bool sellClick = flyCamera != null ? flyCamera.RightClicked : mouse.rightButton.wasReleasedThisFrame;
            if (sellClick) Sell(screen);
        }

        // ------------------------------------------------------------ build: click = one brick, drag = a line

        void BuildUpdate(Mouse mouse, Vector2 screen)
        {
            Aim(screen);
            if (!dragging)
            {
                ghost.SetActive(aimed && ghostFilter.sharedMesh != null);
                HideGhosts(1);
                if (mouse.leftButton.wasPressedThisFrame && aimed)
                {
                    if (carrying) { if (valid) Place(); }   // a moved brick is placed alone
                    else { dragging = true; dragStart = dragEnd = placePos; }
                }
                return;
            }
            if (aimed) dragEnd = placePos;
            PlanLine(dragStart, dragEnd);
            if (!mouse.leftButton.isPressed) { dragging = false; PlaceLine(); HideGhosts(1); }
        }

        /// <summary>Bricks from a to b along X or Z (whichever is longer), one footprint apart. Each one sits on whatever is
        /// under it, so a line over an older line becomes the next row of a wall. Fills linePos / lineOk and the previews.</summary>
        void PlanLine(Vector3 a, Vector3 b)
        {
            linePos.Clear();
            lineOk.Clear();
            var shape = Shape;
            bool swap = turns % 2 == 1;
            float s = BrickGrid.Stud;
            float fx = (swap ? shape.studs.y : shape.studs.x) * s;
            float fz = (swap ? shape.studs.x : shape.studs.y) * s;
            float h = shape.Height;
            Vector3 d = b - a;
            bool alongX = Mathf.Abs(d.x) >= Mathf.Abs(d.z);
            float step = alongX ? fx : fz;
            float len = alongX ? d.x : d.z;
            int count = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(len) / step) + 1, 1, Mathf.Max(1, maxLine));
            Vector3 axis = (alongX ? Vector3.right : Vector3.forward) * (len < 0f ? -1f : 1f);
            float top = Mathf.Max(a.y, b.y) + h * 2f + 1f;
            var ext = new Vector3(fx * 0.5f - 0.04f, 0.02f, fz * 0.5f - 0.04f);
            var half = new Vector3(fx * 0.5f - 0.05f, h * 0.5f - 0.04f, fz * 0.5f - 0.05f);
            var rot = Quaternion.Euler(0f, 90f * turns, 0f);
            float price = shape.Price(Material), cost = 0f;
            int shown = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 c = a + axis * (step * i);
                Vector3 p = c;
                if (i > 0)
                {
                    if (!Physics.BoxCast(new Vector3(c.x, top, c.z), ext, Vector3.down, out var down, Quaternion.identity, 300f, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore)) continue;
                    p = new Vector3(c.x, Mathf.Round(down.point.y / BrickGrid.PlateHeight) * BrickGrid.PlateHeight, c.z);
                }
                bool free = !Physics.CheckBox(p + Vector3.up * (h * 0.5f + 0.02f), half, Quaternion.identity, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore);
                bool ok = free && cost + price <= money;
                if (ok) cost += price;
                linePos.Add(p);
                lineOk.Add(ok);
                ShowGhost(shown++, shape.mesh, p, rot, Vector3.one, ok ? okColor : badColor);
            }
            HideGhosts(shown);
        }

        void PlaceLine()
        {
            var group = new List<Block>();
            float price = Shape.Price(Material);
            for (int i = 0; i < linePos.Count; i++)
            {
                if (!lineOk[i] || money < price) continue;
                var b = BlockFactory.Create(Shape, Material, linePos[i], turns, blocksRoot);
                b.placedByPlayer = true;
                b.paid = true;
                money -= price;
                DropIn(b, group.Count * lineStagger);
                group.Add(b);
            }
            if (group.Count > 0) history.Add(group);
        }

        // ------------------------------------------------------------ erase: click = one, drag = an area

        void EraseUpdate(Mouse mouse, Vector2 screen)
        {
            var ray = cam.ScreenPointToRay(screen);
            bool hitOk = Physics.Raycast(ray, out var hit, maxDistance, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore);
            if (!dragging)
            {
                int n = 0;
                if (hitOk)
                {
                    var person = hit.collider.GetComponentInParent<Person>();
                    var block = person == null ? hit.collider.GetComponent<Block>() : null;
                    if (person != null) ShowGhost(n++, personGhostMesh, person.transform.position + Vector3.up * 0.9f * person.transform.localScale.y, Quaternion.identity, new Vector3(0.7f, 0.95f, 0.7f) * person.transform.localScale.y, eraseColor);
                    else if (block != null) n = GhostBlock(n, block);
                }
                HideGhosts(n);
                if (mouse.leftButton.wasPressedThisFrame && hitOk) { dragging = true; dragStart = dragEnd = hit.point; }
                return;
            }
            // keep the area on the plane where the drag started, so it does not jump over walls
            var plane = new Plane(Vector3.up, dragStart);
            if (plane.Raycast(ray, out float enter)) dragEnd = ray.GetPoint(enter);
            CollectErase(screen);
            if (!mouse.leftButton.isPressed) { dragging = false; StartCoroutine(EraseAll(new List<Block>(eraseBlocks), new List<Person>(erasePeople))); HideGhosts(0); }
        }

        /// <summary>A short drag (under half a metre) erases what is under the mouse; a longer drag erases every brick
        /// and person whose centre is inside the rectangle (all heights).</summary>
        void CollectErase(Vector2 screen)
        {
            eraseBlocks.Clear();
            erasePeople.Clear();
            Vector3 min = Vector3.Min(dragStart, dragEnd), max = Vector3.Max(dragStart, dragEnd);
            int n = 0;
            if (max.x - min.x < 0.5f && max.z - min.z < 0.5f)
            {
                var ray = cam.ScreenPointToRay(screen);
                if (Physics.Raycast(ray, out var hit, maxDistance, BrickGrid.QueryMask, QueryTriggerInteraction.Ignore))
                {
                    var person = hit.collider.GetComponentInParent<Person>();
                    var block = person == null ? hit.collider.GetComponent<Block>() : null;
                    if (person != null) erasePeople.Add(person);
                    else if (block != null) { eraseBlocks.Add(block); n = GhostBlock(n, block); }
                }
                HideGhosts(n);
                return;
            }
            // the area itself: a thin red slab on the ground
            ShowGhost(n++, boxGhostMesh, new Vector3((min.x + max.x) * 0.5f, dragStart.y + 0.05f, (min.z + max.z) * 0.5f), Quaternion.identity,
                      new Vector3(max.x - min.x, 0.1f, max.z - min.z), new Color(eraseColor.r, eraseColor.g, eraseColor.b, 0.25f));
            foreach (var b in Block.All)
            {
                if (b == null || eraseBlocks.Count >= maxErase) continue;
                Vector3 c = b.Centre;
                if (c.x < min.x || c.x > max.x || c.z < min.z || c.z > max.z) continue;
                eraseBlocks.Add(b);
                n = GhostBlock(n, b);
            }
            foreach (var p in Person.All)
            {
                if (p == null || erasePeople.Count + eraseBlocks.Count >= maxErase) continue;
                Vector3 c = p.transform.position;
                if (c.x < min.x || c.x > max.x || c.z < min.z || c.z > max.z) continue;
                erasePeople.Add(p);
                ShowGhost(n++, personGhostMesh, c + Vector3.up * 0.9f * p.transform.localScale.y, Quaternion.identity, new Vector3(0.7f, 0.95f, 0.7f) * p.transform.localScale.y, eraseColor);
            }
            HideGhosts(n);
        }

        int GhostBlock(int n, Block b)
        {
            var mf = b.GetComponent<MeshFilter>();
            if (mf == null) return n;
            ShowGhost(n, mf.sharedMesh, b.transform.position - Vector3.up * 0.01f, b.transform.rotation, Vector3.one * 1.04f, eraseColor);
            return n + 1;
        }

        /// <summary>Removes the bricks one after the other (a quick cascade of brick breaks) and the people at once.
        /// Bricks give back sellRefund of their price, people their full price.</summary>
        System.Collections.IEnumerator EraseAll(List<Block> blocks, List<Person> people)
        {
            foreach (var p in people)
            {
                if (p == null) continue;
                money += personPrice;
                BrickFx.Snap(p.transform.position + Vector3.up, Color.white);
                Destroy(p.gameObject);
            }
            blocks.RemoveAll(b => b == null);
            blocks.Sort((x, y) => Vector3.Distance(x.Centre, dragStart).CompareTo(Vector3.Distance(y.Centre, dragStart)));
            foreach (var b in blocks)
            {
                money += Mathf.Round(b.Price * sellRefund);
                RemoveFromHistory(b);
                if (b.Box != null) b.Box.enabled = false;   // gone for building at once, the break shows a moment later
            }
            float wait = 0f, step = blocks.Count > 1 ? Mathf.Min(0.02f, 0.6f / blocks.Count) : 0f;
            foreach (var b in blocks)
            {
                if (b == null) continue;
                BrickFx.Break(b, 0.6f);
                Destroy(b.gameObject);
                wait += step;
                while (wait >= Time.deltaTime && wait > 0.0001f) { wait -= Time.deltaTime; yield return null; }
            }
        }

        // ------------------------------------------------------------ previews

        void ShowGhost(int i, Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale, Color color)
        {
            while (ghosts.Count <= i)
            {
                var go = new GameObject("BuildPreview" + ghosts.Count);
                go.AddComponent<MeshFilter>();
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = ghostMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ghosts.Add(r);
            }
            var g = ghosts[i];
            g.GetComponent<MeshFilter>().sharedMesh = mesh;
            g.transform.SetPositionAndRotation(pos, rot);
            g.transform.localScale = scale;
            mpb.SetColor("_BaseColor", color);
            g.SetPropertyBlock(mpb);
            if (!g.gameObject.activeSelf) g.gameObject.SetActive(true);
        }

        void HideGhosts(int from)
        {
            for (int i = from; i < ghosts.Count; i++) if (ghosts[i].gameObject.activeSelf) ghosts[i].gameObject.SetActive(false);
        }

        void RemoveFromHistory(Block b)
        {
            foreach (var g in history) g.Remove(b);
        }

        /// <summary>HUD: build or erase.</summary>
        public void SetTool(Tool tool)
        {
            if (tool == CurrentTool) return;
            DropCarry();
            dragging = false;
            HideGhosts(0);
            CurrentTool = tool;
        }

        void ReadKeys(Keyboard kb)
        {
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            if (ctrl && kb.zKey.wasPressedThisFrame) { Undo(); return; }
            for (int k = 0; k < materials.Length && k < 4; k++)
                if (kb[Key.Digit1 + k].wasPressedThisFrame) { DropCarry(); materialIndex = k; CurrentTool = Tool.Build; }
            if (!ctrl && kb.zKey.wasPressedThisFrame) { DropCarry(); shapeIndex = (shapeIndex - 1 + shapes.Length) % shapes.Length; }
            if (kb.xKey.wasPressedThisFrame) { DropCarry(); shapeIndex = (shapeIndex + 1) % shapes.Length; }
            if (kb.rKey.wasPressedThisFrame) turns = (turns + 1) % 4;
            if (kb.tabKey.wasPressedThisFrame) { DropCarry(); CurrentMode = CurrentMode == Mode.Bricks ? Mode.People : Mode.Bricks; CurrentTool = Tool.Build; }
            if (!dragging && kb.vKey.wasPressedThisFrame) SetTool(Tool.Build);
            if (!dragging && kb.cKey.wasPressedThisFrame) SetTool(Tool.Erase);
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
            history.Add(new List<Block> { b });
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
            RemoveFromHistory(b);
            BrickFx.Break(b, 0.6f);
            Destroy(b.gameObject);
        }

        [Header("Juice")]
        [Tooltip("A placed brick drops in from this height (m) with a squash and a dust ring. 0 = no animation. Default 0.6.")]
        public float dropHeight = 0.6f;

        /// <summary>Placed brick: only the mesh drops in and squashes (the collider is in place at once, so stacking works).</summary>
        void DropIn(Block b, float delay = 0f)
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
            if (delay > 0f) { vis.SetActive(false); seq.AppendInterval(delay); seq.AppendCallback(() => { if (vis != null) vis.SetActive(true); }); }
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
            RemoveFromHistory(b);
            Destroy(b.gameObject);
        }

        /// <summary>HUD: pick a material (also switches to brick mode).</summary>
        public void SelectMaterial(int index)
        {
            if (index < 0 || index >= materials.Length) return;
            DropCarry();
            materialIndex = index;
            CurrentMode = Mode.Bricks;
            CurrentTool = Tool.Build;
        }

        /// <summary>HUD: pick a shape (also switches to brick mode).</summary>
        public void SelectShape(int index)
        {
            if (index < 0 || index >= shapes.Length) return;
            DropCarry();
            shapeIndex = index;
            CurrentMode = Mode.Bricks;
            CurrentTool = Tool.Build;
        }

        /// <summary>HUD: bricks or people.</summary>
        public void SetMode(Mode mode)
        {
            if (mode == CurrentMode) return;
            DropCarry();
            CurrentMode = mode;
            CurrentTool = Tool.Build;
        }

        /// <summary>Clears the undo list (new map).</summary>
        public void ResetBuild() { history.Clear(); carrying = false; buildEnabled = true; CurrentMode = Mode.Bricks; }

        /// <summary>Start of a build phase: building on, undo history empty (bricks of earlier rounds cannot be undone).</summary>
        public void BeginBuildPhase() { history.Clear(); carrying = false; buildEnabled = true; }

        /// <summary>End of a build phase: a brick still being moved is paid back, then building is off.</summary>
        public void EndBuildPhase() { DropCarry(); history.Clear(); buildEnabled = false; }

        /// <summary>Takes back the last click or drawn line (full price back).</summary>
        void Undo()
        {
            for (int i = history.Count - 1; i >= 0; i--)
            {
                var g = history[i];
                history.RemoveAt(i);
                g.RemoveAll(b => b == null);
                if (g.Count == 0) continue;
                foreach (var b in g)
                {
                    if (b.paid) money += b.Price;
                    BrickFx.Break(b, 0.5f);
                    Destroy(b.gameObject);
                }
                return;
            }
        }
    }
}
