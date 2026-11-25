using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BrickPanic
{
    /// <summary>Test only (until the round system): P / T / V = turn on physics and start the earthquake / tsunami / volcano, F5 = new map (not during the quake),
    /// F6 = spawn a test crowd with three towers.
    /// Also shows a temporary debug HUD.</summary>
    public class RoundTester : MonoBehaviour
    {
        public BuildController build;
        public PhysicsActivator activator;
        public Earthquake quake;
        public Tsunami tsunami;
        public Eruption eruption;
        public MapGenerator map;

        Disaster current;
        bool activated;
        bool AnyRunning => (quake != null && quake.Running) || (tsunami != null && tsunami.Running) || (eruption != null && eruption.Running);

        GUIStyle style;
        bool started;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.pKey.wasPressedThisFrame) StartDisaster(quake);
            if (kb.tKey.wasPressedThisFrame) StartDisaster(tsunami);
            if (kb.vKey.wasPressedThisFrame) StartDisaster(eruption);
            if (kb.f5Key.wasPressedThisFrame && !AnyRunning) NewMap();
            if (kb.f6Key.wasPressedThisFrame && !started) SpawnTestCrowd();
        }

        /// <summary>Test: 14 people and three 2x2 towers (plastic, wood, plastic) near the island centre.</summary>
        [ContextMenu("Spawn Test Crowd")]
        public void SpawnTestCrowd()
        {
            int ci = map.size / 2, cj = map.size / 2 - 6, placed = 0;
            for (int k = 0; k < 80 && placed < 14; k++)
            {
                int i = ci + (k % 8) - 4, j = cj + (k / 8) - 3;
                if (!map.Inside(i, j) || map.Zones[i, j] == MapGenerator.ZoneType.Volcano || map.Zones[i, j] == MapGenerator.ZoneType.Crater) continue;
                Vector3 p = map.transform.TransformPoint(map.CellCentre(i, j));
                p.y = map.TopY(i, j) + 0.05f;
                if (Physics.CheckCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.5f, 0.28f)) continue;
                Instantiate(build.personPrefab, p, Quaternion.Euler(0f, k * 47f, 0f), build.peopleRoot);
                placed++;
            }
            ShapeDef tower = null;
            foreach (var s in build.shapes) if (s.kind == ShapeKind.Brick && s.studs == new Vector2Int(2, 2)) tower = s;
            for (int t = 0; t < 3; t++)
            {
                int ti = ci - 3 + t * 3, tj = cj + 4;
                if (!map.Inside(ti, tj)) continue;
                Vector3 p = map.transform.TransformPoint(map.CellCentre(ti, tj));
                float y = map.TopY(ti, tj);
                for (int r = 0; r < 9; r++) BlockFactory.Create(tower, build.materials[t % 2], new Vector3(p.x, y + r * BrickGrid.BrickHeight, p.z), 0, build.blocksRoot);
            }
        }

        [ContextMenu("Start Earthquake")] public void StartQuake() => StartDisaster(quake);
        [ContextMenu("Start Tsunami")] public void StartTsunami() => StartDisaster(tsunami);
        [ContextMenu("Start Volcano")] public void StartVolcano() => StartDisaster(eruption);

        /// <summary>Turns on physics (once) and starts a disaster, if none is running.</summary>
        public void StartDisaster(Disaster d)
        {
            if (d == null || AnyRunning) return;
            started = true;
            build.buildEnabled = false;
            if (!activated) { activated = true; activator.Activate(new List<Block>(Block.All)); }
            current = d;
            d.Begin();
        }

        void NewMap()
        {
            for (int i = Block.All.Count - 1; i >= 0; i--) Destroy(Block.All[i].gameObject);
            for (int i = Person.All.Count - 1; i >= 0; i--) Destroy(Person.All[i].gameObject);
            Person.Dead = 0;
            Person.Causes.Clear();
            Block.JointsMade = Block.JointsBroken = Block.Lost = 0;
            Block.BreakLog.Clear();
            started = false;
            activated = false;
            Person.Fainted = 0;
            Block.Burned = 0;
            build.ResetBuild();
            map.Generate();
        }

        static string DeathText()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kv in Person.Causes) sb.Append($"{kv.Key} {kv.Value} ");
            return sb.ToString().Trim();
        }

        void OnGUI()
        {
            if (style == null) style = new GUIStyle(GUI.skin.box) { fontSize = 17, alignment = TextAnchor.UpperLeft };
            string txt = started
                ? $"{(current != null ? current.displayName.ToUpper() : "")} {(AnyRunning ? "running" : "over (P/T/V = another one)")}   people alive {Person.AliveCount}   dead {Person.Dead} ({DeathText()})   fainted {Person.Fainted}   bricks fell off: {Block.Lost}  burned: {Block.Burned}   joints broken: {Block.JointsBroken}/{Block.JointsMade}\nF5 = new map"
                : $"Money {build.money:0}   seed {map.Seed}   people {Person.AliveCount}   MODE [Tab]: {build.CurrentMode}\n" +
                  (build.CurrentMode == BuildController.Mode.People ? $"Person price {build.personPrice:0}\n" :
                  $"Material [1-4]: {build.Material.displayName}   Shape [Z/X]: {build.Shape.displayName}   price {build.Shape.Price(build.Material):0}{(build.Carrying ? "   (moving: free)" : "")}\n") +
                  "Left click place | short right click sell 50% | middle click move | R rotate | Ctrl+Z undo\n" +
                  "Hold right mouse + WASD/QE fly | P quake | T tsunami | V volcano | F5 new map | F6 test crowd";
            GUI.Box(new Rect(10, 10, 900, started ? 56 : 100), txt, style);
        }
    }
}
