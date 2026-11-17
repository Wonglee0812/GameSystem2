using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DamRush
{
    /// <summary>
    /// Water lab: drop test parts into still water (pool) or a current (channel),
    /// change the water and force settings live, and read the numbers.
    /// Keys are listed in the panel. Changes to sliders are written into the lab config asset.
    /// </summary>
    public class LabController : MonoBehaviour
    {
        [System.Serializable]
        public class SpawnOption
        {
            public string label;
            public ShapeDef shape;
            public MaterialDef material;
            public bool fixedWall; // kinematic: does not move, but still reports water forces
            public bool standUp;
        }

        public SandboxConfig config;
        public WaterGrid water;
        public LabTerrain terrain;
        public WaterSource source;
        public Camera cam;
        public Transform partsRoot, debrisRoot;
        public SpawnOption[] options;

        readonly List<Part> spawned = new List<Part>();
        WaterBody selected;
        float spawnYaw;
        bool showVelocity = true, showForces = true, showPanel = true;
        readonly float[] timeScales = { 0.25f, 0.5f, 1f, 2f };
        int timeScaleIndex = 2;
        string hoverText = "";
        Vector2 scroll;

        void Awake()
        {
            PartFactory.Config = config;
            PartFactory.Water = water;
            PartFactory.PartsRoot = partsRoot;
            PartFactory.DebrisRoot = debrisRoot;
            PartFactory.ClearDebris();
            Part.ForgetWelds();
            Time.timeScale = 1f;
        }

        void OnDestroy() { Time.timeScale = 1f; }

        // ------------------------------------------------------------------ input

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            for (int i = 0; i < options.Length && i < 9; i++)
                if (kb[Key.Digit1 + i].wasPressedThisFrame) Spawn(options[i]);

            if (kb.qKey.wasPressedThisFrame) spawnYaw -= 45f;
            if (kb.eKey.wasPressedThisFrame) spawnYaw += 45f;
            if (kb.spaceKey.wasPressedThisFrame) water.paused = !water.paused;
            if (kb.nKey.wasPressedThisFrame && water.paused) water.StepOnce(Time.fixedDeltaTime);
            if (kb.rKey.wasPressedThisFrame) water.ResetWater();
            if (kb.cKey.wasPressedThisFrame) water.colorMode = (WaterColorMode)(((int)water.colorMode + 1) % 3);
            if (kb.vKey.wasPressedThisFrame) showVelocity = !showVelocity;
            if (kb.bKey.wasPressedThisFrame) showForces = !showForces;
            if (kb.hKey.wasPressedThisFrame) showPanel = !showPanel;
            if (kb.tKey.wasPressedThisFrame)
            {
                timeScaleIndex = (timeScaleIndex + 1) % timeScales.Length;
                Time.timeScale = timeScales[timeScaleIndex];
            }
            if (kb.deleteKey.wasPressedThisFrame) ClearParts();

            bool overPanel = showPanel && mouse.position.ReadValue().x < PanelWidth * GuiScale;
            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            bool hit = Physics.Raycast(ray, out RaycastHit h, 300f);
            hoverText = hit ? HoverInfo(h) : "";

            if (!overPanel && hit && mouse.leftButton.wasPressedThisFrame)
                selected = h.collider.GetComponent<WaterBody>();
            if (!overPanel && hit && mouse.rightButton.wasPressedThisFrame)
            {
                var part = h.collider.GetComponent<Part>();
                if (part != null) { spawned.Remove(part); Destroy(part.gameObject); }
                else if (h.collider.GetComponent<WaterBody>() != null) Destroy(h.collider.gameObject);
            }

            DrawDebug();
        }

        void Spawn(SpawnOption o)
        {
            var mouse = Mouse.current;
            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit h, 300f)) return;
            var rot = Quaternion.Euler(0f, spawnYaw, o.standUp ? 90f : 0f);
            Vector3 half = o.shape.size * 0.5f;
            float up = Mathf.Abs((rot * Vector3.right).y) * half.x + Mathf.Abs((rot * Vector3.up).y) * half.y + Mathf.Abs((rot * Vector3.forward).y) * half.z;
            Vector3 pos = h.point + Vector3.up * (o.fixedWall ? up : up + 1.5f);
            if (o.fixedWall)
            {
                // snap walls to a 0.5 m grid so they line up
                pos.x = Mathf.Round(pos.x * 2f) / 2f;
                pos.z = Mathf.Round(pos.z * 2f) / 2f;
            }
            var part = PartFactory.Create(o.shape, o.material, pos, rot);
            if (!o.fixedWall) part.Activate();
            part.name = o.label + " " + spawned.Count;
            spawned.Add(part);
            selected = part.GetComponent<WaterBody>();
        }

        void ClearParts()
        {
            foreach (var p in spawned) if (p != null) Destroy(p.gameObject);
            spawned.Clear();
            PartFactory.ClearDebris();
            Part.ForgetWelds();
            selected = null;
        }

        string HoverInfo(RaycastHit h)
        {
            float x = h.point.x, z = h.point.z;
            float s = water.SurfaceAt(x, z, out bool wet);
            Vector2 v = water.VelocityAt(x, z);
            return $"Cursor x {x:0.0} z {z:0.0}\n" +
                   (wet ? $"Water depth {water.DepthAt(x, z):0.00} m, surface {s:0.00} m\nFlow {v.magnitude:0.00} m/s" : "Dry") +
                   (water.ObstacleAt(x, z) > 0.01f ? $"\nPart in cell: +{water.ObstacleAt(x, z):0.00} m ground" : "");
        }

        // ------------------------------------------------------------------ drawing

        void DrawDebug()
        {
            if (showVelocity)
            {
                float c = water.CellSize;
                for (int i = 0; i < water.CellsX; i += 2)
                    for (int j = 0; j < water.CellsZ; j += 2)
                    {
                        float x = (i + 0.5f) * c, z = (j + 0.5f) * c;
                        Vector2 v = water.VelocityAt(x, z);
                        if (v.sqrMagnitude < 0.0025f) continue;
                        float s = water.SurfaceAt(x, z, out _);
                        var a = new Vector3(x, s + 0.05f, z);
                        DebugLines.Arrow(a, a + new Vector3(v.x, 0f, v.y) * 0.5f, Color.Lerp(Color.cyan, Color.red, v.magnitude / 3f));
                    }
            }

            if (selected != null)
            {
                Bounds b = selected.Col.bounds;
                DrawBox(b, Color.yellow);
            }

            if (!showForces) return;
            foreach (var wb in WaterBody.All)
            {
                if (wb == null || (wb != selected && wb.Submerged <= 0f)) continue;
                Vector3 c = wb.Body.worldCenterOfMass;
                float weight = Mathf.Max(1f, wb.Body.mass * config.gravity);
                // arrows in "weights": 1 m = the part's own weight
                Vector3 buoy = Vector3.up * wb.PressureForce.y / weight;
                Vector3 push = new Vector3(wb.PressureForce.x, 0f, wb.PressureForce.z) / weight;
                Vector3 drag = wb.DragForce / weight;
                DebugLines.Arrow(c, c + Vector3.down, new Color(1f, 1f, 1f, 0.8f));
                DebugLines.Arrow(c, c + buoy, new Color(0.2f, 0.9f, 1f));
                if (push.sqrMagnitude > 0.0004f) DebugLines.Arrow(c, c + push, new Color(1f, 0.3f, 0.3f));
                if (drag.sqrMagnitude > 0.0004f) DebugLines.Arrow(c, c + drag, new Color(1f, 0.85f, 0.2f));
            }
        }

        static void DrawBox(Bounds b, Color c)
        {
            Vector3 mn = b.min, mx = b.max;
            Vector3[] p =
            {
                new Vector3(mn.x, mn.y, mn.z), new Vector3(mx.x, mn.y, mn.z), new Vector3(mx.x, mn.y, mx.z), new Vector3(mn.x, mn.y, mx.z),
                new Vector3(mn.x, mx.y, mn.z), new Vector3(mx.x, mx.y, mn.z), new Vector3(mx.x, mx.y, mx.z), new Vector3(mn.x, mx.y, mx.z),
            };
            for (int i = 0; i < 4; i++)
            {
                DebugLines.Line(p[i], p[(i + 1) % 4], c);
                DebugLines.Line(p[i + 4], p[(i + 1) % 4 + 4], c);
                DebugLines.Line(p[i], p[i + 4], c);
            }
        }

        // ------------------------------------------------------------------ panel

        const float PanelWidth = 380f;
        float GuiScale => Screen.height / 1080f;

        void OnGUI()
        {
            if (!showPanel) return;
            float k = GuiScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1f));
            float height = Screen.height / k;
            GUI.Box(new Rect(0, 0, PanelWidth, height), GUIContent.none);
            GUILayout.BeginArea(new Rect(10, 10, PanelWidth - 20, height - 20));
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("<b>WATER LAB</b>  (H hides this panel)", Rich);
            GUILayout.Label($"Sim {(water.paused ? "PAUSED (N = one step)" : "running")}  |  time x{Time.timeScale:0.##}  |  {water.StepMilliseconds:0.0} ms/step");
            GUILayout.Label($"Water volume {water.TotalVolume():0.0} m3   in {water.InflowRate:0.0} / out {water.OutflowRate:0.0} m3/s");
            GUILayout.Label($"Fastest flow {water.MaxSpeed():0.00} m/s   colour: {water.colorMode} (C)");

            Header("Water");
            config.friction = GUILayout.Toggle(config.friction == FrictionMode.Manning, " Manning bed friction (off = old damping)") ? FrictionMode.Manning : FrictionMode.FluxDamping;
            if (config.friction == FrictionMode.Manning) config.manningN = Slider("Manning n", config.manningN, 0.005f, 0.12f, "0.000");
            else config.fluxDamping = Slider("Flux damping", config.fluxDamping, 0.95f, 1f, "0.000");
            config.pipeArea = Slider("Pipe area", config.pipeArea, 0.05f, 1f, "0.00");
            config.substeps = Mathf.RoundToInt(Slider("Substeps", config.substeps, 1, 8, "0"));
            config.displaceWater = GUILayout.Toggle(config.displaceWater, " Parts push water aside (keep volume)");
            config.openEndX = GUILayout.Toggle(config.openEndX, " Water leaves at channel end");
            if (source != null) source.rate = Slider("Source m3/s", source.rate, 0f, 15f, "0.0");
            terrain.poolWaterLevel = Slider("Pool level (R resets)", terrain.poolWaterLevel, 0f, 2.8f, "0.00");

            Header("Forces on parts");
            config.pressureScale = Slider("Pressure scale", config.pressureScale, 0f, 2f, "0.00");
            config.dragCoefficient = Slider("Drag Cd", config.dragCoefficient, 0f, 3f, "0.00");
            config.waterLinearDamping = Slider("Water damping", config.waterLinearDamping, 0f, 5f, "0.00");
            config.waterAngularDamping = Slider("Water spin damping", config.waterAngularDamping, 0f, 5f, "0.00");

            Header("Spawn at cursor (Q/E turn " + spawnYaw + "°)");
            for (int i = 0; i < options.Length; i++)
                GUILayout.Label($"{i + 1}  {options[i].label}");

            Header("Selected (LMB)");
            GUILayout.Label(SelectedInfo(), Rich);

            Header("Cursor");
            GUILayout.Label(hoverText);

            Header("Keys");
            GUILayout.Label("LMB select   RMB delete   Del delete all\nSpace pause   N step   R reset water   T time speed\nC water colour   V flow arrows   B force arrows\nWASD move   MMB drag rotate   Wheel zoom");
            GUILayout.Label("<color=#9fdcff>Arrows: white = weight, cyan = water lift, red = side push, yellow = drag. 1 m = own weight.</color>", Rich);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        string SelectedInfo()
        {
            if (selected == null) return "Nothing selected";
            var b = selected.Body;
            float weight = b.mass * config.gravity;
            var part = selected.GetComponent<Part>();
            string mat = part != null ? $"{part.material.displayName} {part.shape.displayName}, density {part.material.density:0} kg/m3" : selected.name;
            Vector3 push = new Vector3(selected.PressureForce.x, 0f, selected.PressureForce.z);
            string level = float.IsNaN(selected.WaterLevelAround) ? "dry" : $"{selected.WaterLevelAround:0.00} m";
            return $"<b>{selected.name}</b>\n{mat}\n" +
                   $"mass {b.mass:0} kg, weight {weight / 1000f:0.00} kN{(b.isKinematic ? "  (fixed wall)" : "")}\n" +
                   $"under water {selected.Submerged * 100f:0}% of surface, {selected.PatchCount} patches\n" +
                   $"water lift {selected.PressureForce.y / 1000f:0.00} kN = {selected.PressureForce.y / Mathf.Max(1f, weight) * 100f:0}% of weight\n" +
                   $"side push {push.magnitude / 1000f:0.00} kN\n" +
                   $"drag {selected.DragForce.magnitude / 1000f:0.00} kN, flow {selected.FlowSpeed:0.00} m/s\n" +
                   $"speed {b.linearVelocity.magnitude:0.00} m/s, water level around {level}";
        }

        static GUIStyle rich;
        static GUIStyle Rich => rich ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };

        static void Header(string text)
        {
            GUILayout.Space(8);
            GUILayout.Label("<b>" + text + "</b>", Rich);
        }

        static float Slider(string label, float value, float min, float max, string format)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(140));
            GUILayout.Label(value.ToString(format), GUILayout.Width(50));
            GUILayout.EndHorizontal();
            return value;
        }
    }
}
