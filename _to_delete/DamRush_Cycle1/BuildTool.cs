using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DamRush
{
    /// <summary>
    /// Free placement of parts in the build phase.
    /// LMB place, RMB remove (full refund before its first flood, half after),
    /// Q/E turn, R stand up / lay down, G snap on/off, 1-3 shape, 4-6 material, Space = flood.
    /// </summary>
    public class BuildTool : MonoBehaviour
    {
        public DamGame game;
        public Camera cam;
        public ValleyTerrain terrain;
        public ShapeDef[] shapes;
        public MaterialDef[] materials;
        public Material ghostMaterial;

        public int ShapeIndex { get; private set; }
        public int MaterialIndex { get; private set; }
        public bool Snap { get; private set; } = true;
        public bool GhostValid { get; private set; }
        public string Problem { get; private set; } = "";

        public ShapeDef Shape => shapes[ShapeIndex];
        public MaterialDef Mat => materials[MaterialIndex];
        public int CurrentCost => PartFactory.Cost(Shape, Mat);

        float yaw;
        float tilt;
        GameObject ghost;
        MaterialPropertyBlock block;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        readonly Collider[] overlap = new Collider[16];

        void Start()
        {
            block = new MaterialPropertyBlock();
            RebuildGhost();
        }

        public void SelectShape(int i) { ShapeIndex = Mathf.Clamp(i, 0, shapes.Length - 1); RebuildGhost(); }
        public void SelectMaterial(int i) { MaterialIndex = Mathf.Clamp(i, 0, materials.Length - 1); }
        public void ToggleSnap() { Snap = !Snap; if (Snap) yaw = Mathf.Round(yaw / 90f) * 90f; }

        void RebuildGhost()
        {
            if (ghost != null) Destroy(ghost);
            ghost = PartFactory.CreateShell(Shape, "Ghost");
            Destroy(ghost.GetComponent<Collider>());
            var r = ghost.GetComponent<Renderer>();
            r.sharedMaterial = ghostMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ghost.SetActive(false);
        }

        Quaternion Rotation => Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, tilt);

        /// <summary>Half size of the rotated shape measured along a direction.</summary>
        float ExtentAlong(Quaternion rot, Vector3 dir)
        {
            Vector3 h = Shape.size * 0.5f;
            return Mathf.Abs(Vector3.Dot(rot * Vector3.right, dir)) * h.x
                 + Mathf.Abs(Vector3.Dot(rot * Vector3.up, dir)) * h.y
                 + Mathf.Abs(Vector3.Dot(rot * Vector3.forward, dir)) * h.z;
        }

        void Update()
        {
            HandleKeys();
            if (!game.CanBuild) { if (ghost != null) ghost.SetActive(false); return; }

            var mouse = Mouse.current;
            if (mouse == null) return;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (overUI || !Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                ghost.SetActive(false);
                return;
            }

            // right click removes a part
            if (mouse.rightButton.wasPressedThisFrame)
            {
                var part = hit.collider.GetComponent<Part>();
                if (part != null)
                {
                    game.Refund(part.IsActive ? part.cost / 2 : part.cost);
                    Destroy(part.gameObject);
                    return;
                }
            }

            Quaternion rot = Rotation;
            Vector3 n = hit.normal;
            bool onGround = hit.collider.GetComponent<ValleyTerrain>() != null;
            if (onGround || Vector3.Dot(n, Vector3.up) > 0.7f) n = Vector3.up;
            Vector3 pos = hit.point + n * ExtentAlong(rot, n);

            if (Snap)
            {
                float s = PartFactory.Config.snapStep;
                pos.x = Mathf.Round(pos.x / s) * s;
                pos.z = Mathf.Round(pos.z / s) * s;
            }

            if (onGround)
            {
                // sit on the highest ground point under the part, so it never starts inside the hill
                float hx = ExtentAlong(rot, Vector3.right) * 0.9f, hz = ExtentAlong(rot, Vector3.forward) * 0.9f;
                float top = terrain.GroundAt(pos.x, pos.z);
                top = Mathf.Max(top, terrain.GroundAt(pos.x - hx, pos.z - hz), terrain.GroundAt(pos.x + hx, pos.z - hz));
                top = Mathf.Max(top, terrain.GroundAt(pos.x - hx, pos.z + hz), terrain.GroundAt(pos.x + hx, pos.z + hz));
                pos.y = top + ExtentAlong(rot, Vector3.up);
            }

            ghost.SetActive(true);
            ghost.transform.SetPositionAndRotation(pos, rot);

            GhostValid = Check(pos, rot);
            block.SetColor(BaseColor, GhostValid ? new Color(0.3f, 1f, 0.4f, 0.45f) : new Color(1f, 0.25f, 0.2f, 0.45f));
            ghost.GetComponent<Renderer>().SetPropertyBlock(block);

            if (GhostValid && mouse.leftButton.wasPressedThisFrame && game.TrySpend(CurrentCost))
                PartFactory.Create(Shape, Mat, pos, rot);
        }

        bool Check(Vector3 pos, Quaternion rot)
        {
            if (pos.x < 0f || pos.z < 0f || pos.x > terrain.LengthX || pos.z > terrain.WidthZ) { Problem = "Outside the valley"; return false; }
            if (CurrentCost > game.Budget) { Problem = "Not enough budget"; return false; }
            Vector3 half = Shape.size * 0.5f - Vector3.one * 0.03f;
            int count = Physics.OverlapBoxNonAlloc(pos, half, overlap, rot);
            for (int i = 0; i < count; i++)
            {
                if (overlap[i].GetComponent<ValleyTerrain>() != null) continue;
                Problem = "Blocked by " + overlap[i].name;
                return false;
            }
            Problem = "";
            return true;
        }

        void HandleKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            float step = Snap ? 90f : PartFactory.Config.snapAngle;
            if (kb.qKey.wasPressedThisFrame) yaw -= step;
            if (kb.eKey.wasPressedThisFrame) yaw += step;
            if (kb.rKey.wasPressedThisFrame) tilt = tilt == 0f ? 90f : 0f;
            if (kb.gKey.wasPressedThisFrame) ToggleSnap();
            if (kb.digit1Key.wasPressedThisFrame) SelectShape(0);
            if (kb.digit2Key.wasPressedThisFrame) SelectShape(1);
            if (kb.digit3Key.wasPressedThisFrame) SelectShape(2);
            if (kb.digit4Key.wasPressedThisFrame) SelectMaterial(0);
            if (kb.digit5Key.wasPressedThisFrame) SelectMaterial(1);
            if (kb.digit6Key.wasPressedThisFrame) SelectMaterial(2);
            if (kb.spaceKey.wasPressedThisFrame) game.StartFlood();
        }
    }
}
