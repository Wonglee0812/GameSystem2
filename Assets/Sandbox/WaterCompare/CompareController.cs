using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DamRush.WaterCompare
{
    /// <summary>
    /// The water compare scene: lanes with the same valley, each running one version of the
    /// water. Buttons act on all lanes at once, so the versions can be compared side by side:
    /// flood (x4 inflow), dam break, a train of waves, reset, game speed.
    /// Mouse tools (left button): Push = drag through the water to push it, Wave = click to drop
    /// a bump of water that runs out as a ring wave, Block = click or hold to drop stone blocks,
    /// Boat = click to put a small boat on the water. With "Mirror" on, the same action happens
    /// at the same spot in every lane. Water +/- raises or lowers the water in every wet cell.
    /// Keys: 1-4 look at a lane, 0 all lanes, Tab next tool, M mirror, F flood, B dam break,
    /// G waves, R reset, H hide the panel, PageUp/PageDown water level, Space pause.
    /// </summary>
    public class CompareController : MonoBehaviour
    {
        public WaterSimBase[] sims;
        public LaneGround[] grounds;
        public Transform[] lanes;
        public CompareCamera cam;
        public SurfaceCpu[] surfaces = new SurfaceCpu[0];
        public Material blockMaterial;
        [TextArea] public string[] webNotes;

        enum Tool { Camera, Push, Wave, Block, Boat }
        static readonly string[] ToolNames = { "Camera", "Push", "Wave", "Block", "Boat" };
        Material boatHull, boatCabin;
        readonly List<GameObject> boats = new List<GameObject>();
        float blockTimer;
        Tool tool = Tool.Push;
        bool flood, breach, paused, waveTrain, mirror = true, hidePanel;
        int speedIndex = 0;
        static readonly float[] Speeds = { 1f, 2f, 5f };
        float fps, waveTimer;
        Rect panelRect;

        // push drag state
        bool dragging; int dragLane; Vector2 dragLast;

        class FallingBlock { public Transform[] cubes; public int[] lanes; public Vector2 centre; public float y, vy, landTop; }
        readonly List<FallingBlock> falling = new List<FallingBlock>();
        readonly List<GameObject> blockObjects = new List<GameObject>();
        const float BlockSize = 1.2f, BlockHeight = 1.0f;

        void Update()
        {
            Shader.SetGlobalFloat("_DR_Time", Time.unscaledTime);
            fps = Mathf.Lerp(fps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.05f);
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.fKey.wasPressedThisFrame) ToggleFlood();
                if (kb.bKey.wasPressedThisFrame) ToggleBreach();
                if (kb.gKey.wasPressedThisFrame) waveTrain = !waveTrain;
                if (kb.rKey.wasPressedThisFrame) ResetAll();
                if (kb.hKey.wasPressedThisFrame) hidePanel = !hidePanel;
                if (kb.mKey.wasPressedThisFrame) mirror = !mirror;
                if (kb.tabKey.wasPressedThisFrame) tool = (Tool)(((int)tool + 1) % 5);
                if (kb.pageUpKey.wasPressedThisFrame) AddLevel(0.15f);
                if (kb.pageDownKey.wasPressedThisFrame) AddLevel(-0.15f);
                if (kb.spaceKey.wasPressedThisFrame) TogglePause();
                if (kb.digit1Key.wasPressedThisFrame) Focus(0);
                if (kb.digit2Key.wasPressedThisFrame) Focus(1);
                if (kb.digit3Key.wasPressedThisFrame) Focus(2);
                if (kb.digit4Key.wasPressedThisFrame) Focus(3);
                if (kb.digit0Key.wasPressedThisFrame) cam.ViewAll();
            }

            // wave maker at the top of the reservoir, like a paddle in a wave tank: every 3 s it
            // shoves a strip of water across the stream downstream for a third of a second,
            // so a wave front runs down the reservoir and hits the dam (game time, follows the speed)
            if (waveTrain && !paused)
            {
                waveTimer -= Time.deltaTime;
                if (waveTimer <= 0f) waveTimer = 3f;
                float since = 3f - waveTimer;
                if (since < 0.35f)
                {
                    for (float x = 4f; x <= 8f; x += 2f)
                    {
                        var p = new Vector2(x, CompareShape.ChannelZ(x));
                        var dir = new Vector2(1f, 0.77f * Mathf.Cos(x * 0.11f)).normalized; // along the stream
                        // push only: the wave moves water but adds none (a splash here raised the reservoir)
                        foreach (var s in sims) s.Push(p, 3.5f, dir * 2.5f);
                    }
                }
            }

            MouseTools();
            UpdateFalling();
        }

        // ---------- mouse tools ----------

        void MouseTools()
        {
            var m = Mouse.current;
            if (m == null || tool == Tool.Camera || paused) { dragging = false; return; }
            Vector2 sp = m.position.ReadValue();
            Vector2 guiPos = new Vector2(sp.x, Screen.height - sp.y);
            if (!hidePanel && panelRect.Contains(guiPos)) { dragging = false; return; }
            if (!m.leftButton.isPressed) { dragging = false; blockTimer = 0f; return; }
            if (!Pick(sp, out int lane, out Vector2 local)) { dragging = false; return; }

            bool pressed = m.leftButton.wasPressedThisFrame;
            switch (tool)
            {
                case Tool.Push:
                    if (dragging && lane == dragLane)
                    {
                        Vector2 v = (local - dragLast) / Mathf.Max(0.005f, Time.unscaledDeltaTime);
                        v = Vector2.ClampMagnitude(v, 8f);
                        if (v.sqrMagnitude > 0.01f) foreach (int k in Targets(lane)) sims[k].Push(local, 2f, v);
                    }
                    dragging = true; dragLane = lane; dragLast = local;
                    break;
                case Tool.Wave:
                    if (pressed) foreach (int k in Targets(lane)) sims[k].Splash(local, 2.5f, 0.7f);
                    break;
                case Tool.Block:
                    // hold to spam blocks (one every 0.12 s, a little scattered)
                    blockTimer -= Time.unscaledDeltaTime;
                    if (pressed || blockTimer <= 0f)
                    {
                        blockTimer = 0.12f;
                        DropBlock(lane, local + (pressed ? Vector2.zero : Random.insideUnitCircle * 0.8f));
                    }
                    break;
                case Tool.Boat:
                    if (pressed) foreach (int k in Targets(lane)) boats.Add(Boat.Create(lanes[k], sims[k], local, BoatMat(ref boatHull, new Color(0.62f, 0.36f, 0.2f)), BoatMat(ref boatCabin, new Color(0.95f, 0.93f, 0.88f))).gameObject);
                    break;
            }
        }

        IEnumerable<int> Targets(int lane)
        {
            if (!mirror) { yield return lane; yield break; }
            for (int k = 0; k < sims.Length; k++) yield return k;
        }

        /// <summary>Finds the lane and the point on its ground under the mouse (marches along the view ray).</summary>
        bool Pick(Vector2 screen, out int lane, out Vector2 local)
        {
            lane = -1; local = Vector2.zero;
            var c = Camera.main;
            if (c == null) return false;
            Ray ray = c.ScreenPointToRay(screen);
            for (float t = 1f; t < 500f; t += 0.25f)
            {
                Vector3 p = ray.GetPoint(t);
                for (int k = 0; k < lanes.Length; k++)
                {
                    Vector3 l = p - lanes[k].position;
                    if (l.x < 0f || l.x > CompareShape.LengthX || l.z < 0f || l.z > CompareShape.WidthZ) continue;
                    if (l.y <= sims[k].GroundWithBlocks(l.x, l.z))
                    {
                        lane = k; local = new Vector2(l.x, l.z);
                        return true;
                    }
                }
            }
            return false;
        }

        void DropBlock(int lane, Vector2 centre)
        {
            var targets = new List<int>(Targets(lane));
            // the block lands on the highest ground (or block) under its footprint
            float h = 0f, half = BlockSize * 0.5f;
            var s0 = sims[lane];
            for (float dx = -half; dx <= half; dx += 0.5f)
                for (float dz = -half; dz <= half; dz += 0.5f)
                    h = Mathf.Max(h == 0f ? float.MinValue : h, s0.GroundWithBlocks(centre.x + dx, centre.y + dz));
            var fb = new FallingBlock { lanes = targets.ToArray(), centre = centre, y = h + 10f, vy = 0f, landTop = h + BlockHeight, cubes = new Transform[targets.Count] };
            for (int n = 0; n < targets.Count; n++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Dropped block";
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(lanes[targets[n]], false);
                go.transform.localScale = new Vector3(BlockSize, BlockHeight, BlockSize);
                if (blockMaterial != null) go.GetComponent<Renderer>().sharedMaterial = blockMaterial;
                fb.cubes[n] = go.transform;
                blockObjects.Add(go);
            }
            falling.Add(fb);
        }

        void UpdateFalling()
        {
            for (int f = falling.Count - 1; f >= 0; f--)
            {
                var fb = falling[f];
                fb.vy -= 20f * Time.deltaTime;
                fb.y += fb.vy * Time.deltaTime;
                float bottom = fb.landTop - BlockHeight;
                bool landed = fb.y <= bottom;
                if (landed) fb.y = bottom;
                foreach (var c in fb.cubes) if (c != null) c.localPosition = new Vector3(fb.centre.x, fb.y + BlockHeight * 0.5f, fb.centre.y);
                if (!landed) continue;
                var area = new Rect(fb.centre.x - BlockSize * 0.5f, fb.centre.y - BlockSize * 0.5f, BlockSize, BlockSize);
                foreach (int k in fb.lanes)
                {
                    sims[k].AddBlock(area, fb.landTop);
                    sims[k].Splash(fb.centre, BlockSize * 1.6f, 0.25f); // the hit throws a ring of water out
                }
                falling.RemoveAt(f);
            }
        }

        // ---------- buttons ----------

        void Focus(int k) { if (k < lanes.Length) cam.Focus(lanes[k].position + new Vector3(CompareShape.LengthX * 0.45f, 0f, CompareShape.WidthZ * 0.5f)); }
        void ToggleFlood() { flood = !flood; foreach (var s in sims) s.FloodFactor = flood ? 4f : 1f; }
        void ToggleBreach() { breach = !breach; foreach (var s in sims) s.SetBreach(breach); foreach (var g in grounds) g.SetBreach(breach); }
        void AddLevel(float dh) { foreach (var s in sims) s.AddLevel(dh); }

        static Material BoatMat(ref Material m, Color c)
        {
            if (m == null) m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c };
            return m;
        }

        void ResetAll()
        {
            foreach (var b in boats) if (b != null) Destroy(b);
            boats.Clear();
            breach = false; flood = false; waveTrain = false;
            falling.Clear();
            foreach (var go in blockObjects) if (go != null) Destroy(go);
            blockObjects.Clear();
            foreach (var s in sims) { s.ClearBlocks(); s.SetBreach(false); s.FloodFactor = 1f; s.ResetWater(); }
            foreach (var g in grounds) g.SetBreach(false);
        }
        void TogglePause() { paused = !paused; Time.timeScale = paused ? 0f : Speeds[speedIndex]; }

        GUIStyle box, label, small, head;

        void OnGUI()
        {
            if (box == null)
            {
                box = new GUIStyle(GUI.skin.box) { normal = { background = Tex(new Color(0.06f, 0.08f, 0.1f, 0.9f)) } };
                label = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15, wordWrap = true, normal = { textColor = Color.white } };
                small = new GUIStyle(label) { fontSize = 13 };
                head = new GUIStyle(label) { fontSize = 20, fontStyle = FontStyle.Bold };
            }
            GUI.Label(new Rect(10, Screen.height - 28, 1400, 24), "1-4 look at a lane, 0 all · WASD move · right mouse rotate · wheel zoom · left mouse = tool (Tab) · M mirror · F flood · B dam break · G waves · PgUp/PgDn water · R reset · H panel · Space pause", small);
            if (hidePanel) { panelRect = Rect.zero; DrawLaneNames(); return; }

            float w = 470;
            panelRect = new Rect(Screen.width - w - 10, 10, w, 222 + sims.Length * 96);
            GUI.Box(panelRect, GUIContent.none, box);
            GUILayout.BeginArea(new Rect(panelRect.x + 12, panelRect.y + 8, panelRect.width - 24, panelRect.height - 16));
            GUILayout.Label($"WATER COMPARE   <size=14>{fps:0} fps</size>", head);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(flood ? "Flood ON (F)" : "Flood x4 (F)", GUILayout.Height(28))) ToggleFlood();
            if (GUILayout.Button(breach ? "Close dam (B)" : "Dam break (B)", GUILayout.Height(28))) ToggleBreach();
            if (GUILayout.Button(waveTrain ? "Waves ON (G)" : "Waves (G)", GUILayout.Height(28))) waveTrain = !waveTrain;
            if (GUILayout.Button("Reset (R)", GUILayout.Height(28))) ResetAll();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed:", small, GUILayout.Width(50));
            for (int i = 0; i < Speeds.Length; i++)
                if (GUILayout.Button((speedIndex == i ? "> " : "") + Speeds[i] + "x", GUILayout.Height(26))) { speedIndex = i; paused = false; Time.timeScale = Speeds[i]; }
            if (GUILayout.Button(paused ? "Play" : "Pause", GUILayout.Height(26))) TogglePause();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mouse:", small, GUILayout.Width(50));
            for (int i = 0; i < ToolNames.Length; i++)
                if (GUILayout.Button(((int)tool == i ? "> " : "") + ToolNames[i], GUILayout.Height(26))) tool = (Tool)i;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Water:", small, GUILayout.Width(50));
            if (GUILayout.Button("Level +0.15 m (PgUp)", GUILayout.Height(26))) AddLevel(0.15f);
            if (GUILayout.Button("Level -0.15 m (PgDn)", GUILayout.Height(26))) AddLevel(-0.15f);
            GUILayout.EndHorizontal();
            mirror = GUILayout.Toggle(mirror, $" Mirror the mouse action in every lane (M)   blocks: {(sims.Length > 0 ? sims[0].BlockCount : 0)}  boats: {boats.Count}", small);
            for (int k = 0; k < sims.Length; k++)
            {
                var s = sims[k];
                string web = webNotes != null && k < webNotes.Length ? webNotes[k] : "";
                float surf = k < surfaces.Length && surfaces[k] != null ? surfaces[k].BuildMs : 0f;
                string cost = s is SimGpu ? $"GPU sim + GPU surface (CPU only sends {s.StepsLastFrame} steps: {s.FrameMs:0.00} ms)" : $"CPU: sim {s.FrameMs:0.0} ms + surface mesh {surf:0.0} ms per frame ({s.StepsLastFrame} steps)";
                GUILayout.Space(2);
                GUILayout.Label($"<b>{k + 1}  {s.label}</b>  <size=13>{s.CellsX} x {s.CellsZ} cells, step {s.StepDt * 1000f:0} ms</size>\n<size=13>{cost}\n{web}</size>", label);
            }
            GUILayout.EndArea();
            DrawLaneNames();
        }

        void DrawLaneNames()
        {
            var c = Camera.main;
            if (c == null) return;
            for (int k = 0; k < sims.Length && k < lanes.Length; k++)
            {
                Vector3 p = c.WorldToScreenPoint(lanes[k].position + new Vector3(-2f, 6f, CompareShape.WidthZ * 0.5f));
                if (p.z < 0) continue;
                GUI.Label(new Rect(p.x - 40, Screen.height - p.y - 20, 300, 40), $"<b>{sims[k].label.Substring(0, 1)}</b>", head);
            }
        }

        static Texture2D Tex(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }
    }
}
