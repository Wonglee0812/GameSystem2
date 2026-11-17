using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Phase C orders: the two labs, water wheels and machines. The rules and numbers live in
    /// Industry (data); this part only places them on the map and draws them.
    /// - Labs and machines stand on a finished base, Base Mk2 or Dam Mk2 (like a storage).
    /// - A water wheel stands on a free hex with flowing water.
    /// - Tools from the tech tree stay locked until the research is done.
    /// </summary>
    public partial class BuildOrders
    {
        /// <summary>Machine the leader places next with the machine tool (id in Industry.machines).</summary>
        [HideInInspector] public string machineType = "splitter";

        Industry Ind => colony != null ? colony.industry : null;

        public static bool IsPlatformBuildingTool(BuildTool t) =>
            t == BuildTool.Nursery || t == BuildTool.Storage || t == BuildTool.MaterialLab || t == BuildTool.BuildingLab || t == BuildTool.Machine;

        static BuildingKind KindFor(BuildTool t) =>
            t == BuildTool.Nursery ? BuildingKind.Nursery : t == BuildTool.Storage ? BuildingKind.Storage
            : t == BuildTool.MaterialLab ? BuildingKind.MaterialLab : t == BuildTool.BuildingLab ? BuildingKind.BuildingLab : BuildingKind.Machine;

        /// <summary>Locked tools say which research opens them.</summary>
        bool Locked(BuildTool tool, out string why)
        {
            why = "";
            if (Ind == null) return false;
            if (Ind.ToolUnlocked(tool, machineType, out string needs)) return false;
            why = $"Locked: research {needs} first (Industry tab)";
            return true;
        }

        /// <summary>Extra rules for labs and machines on a platform (the platform itself is checked before).</summary>
        bool CanOrderOnPlatform(BuildTool tool, out string why)
        {
            if (tool == BuildTool.MaterialLab || tool == BuildTool.BuildingLab)
            {
                var k = KindFor(tool);
                foreach (var b in Buildings) if (b.Kind == k) { why = $"There is already a {b.Title.ToLower()}"; return false; }
                why = Def(k).rule;
                return true;
            }
            var m = Ind?.Machine(machineType);
            if (m == null) { why = "Pick a machine type first"; return false; }
            why = $"{m.name}: {m.recipe.Text()}\nNeeds a water wheel on a hex next to it and one operator";
            return true;
        }

        bool CanOrderC(BuildTool tool, HexCell c, out string why)
        {
            if (tool != BuildTool.WaterWheel) { why = ""; return false; }
            if (SiteOn(c) != null) { why = "Something is already planned here"; return false; }
            float f = Ind.WheelFactor(c, out string w);
            if (f <= 0f && !w.StartsWith("frozen")) { why = $"Wheel needs flowing water: {w}"; return false; }
            why = $"Water wheel: {w}\nDrives up to {Ind.machinesPerWheel} machines on the hexes next to it";
            return true;
        }

        void OrderC(BuildTool tool, HexCell c)
        {
            if (tool != BuildTool.WaterWheel) return;
            var site = new BuildingSite(Ind.wheelDef, c, null, null, water, baseHeight);
            sites.Add(site);
            c.blocked = true; c.building = site.Title;
        }

        /// <summary>A new lab or machine on a platform.</summary>
        BuildingSite NewPlatformBuilding(BuildTool tool, HexCell c)
        {
            var kind = KindFor(tool);
            if (kind == BuildingKind.Machine)
            {
                var m = Ind.Machine(machineType);
                var def = new BuildingDef { kind = BuildingKind.Machine, name = m.name, rule = m.recipe.Text(), stages = m.stages };
                return new BuildingSite(def, c, PlatformOn(c), null, water, baseHeight) { machine = m };
            }
            return new BuildingSite(Def(kind), c, PlatformOn(c), null, water, baseHeight);
        }

        string FunctionC(BuildingSite b)
        {
            switch (b.Kind)
            {
                case BuildingKind.MaterialLab:
                case BuildingKind.BuildingLab:
                {
                    var lab = b.Kind == BuildingKind.MaterialLab ? LabKind.Material : LabKind.Building;
                    var cur = Ind.CurrentResearch(lab);
                    if (cur == null) return "Research: nothing now. Open the Industry tab to start one";
                    Ind.Researching(cur, out float p);
                    return $"Researching {cur.name}: {p * 100f:0}%";
                }
                case BuildingKind.WaterWheel:
                    return $"Water wheel: {b.powerWhy}";
                case BuildingKind.Machine:
                {
                    string wait = Ind.WaitReason(b);
                    string mode = b.auto && Ind.Automation ? "auto" : $"{b.ordered} ordered";
                    return $"{b.machine.recipe.Text()}  ({mode}, made {b.made})\n" + (wait.Length > 0 ? $"<color=#ffb070>Waiting: {wait}</color>" : $"<color=#8fe08f>Working at {b.power:0.00}x</color>");
                }
            }
            return "";
        }

        // ------------------------------------------------------------------ looks

        void LabLook(BuildingSite b, Transform root)
        {
            root.position = new Vector3(b.hex.center.x, b.Floor, b.hex.center.z);
            Piece(root, PrimitiveType.Cube, woodMat, new Vector3(0f, 0.6f, 0f), new Vector3(2.2f, 1.2f, 1.8f), Vector3.zero);
            if (b.stage < 1) return;
            Piece(root, PrimitiveType.Cube, mudMat, new Vector3(0f, 1.35f, 0f), new Vector3(2.5f, 0.3f, 2.1f), Vector3.zero);
            // a coloured flag on the roof tells the two labs apart
            var mat = b.Kind == BuildingKind.MaterialLab ? stoneMat : FoodMat;
            Piece(root, PrimitiveType.Cube, mat, new Vector3(0.8f, 1.9f, 0f), new Vector3(0.5f, 0.6f, 0.06f), Vector3.zero);
        }

        Material MachineMat(BuildingSite b)
        {
            if (b.look == null) { b.look = new Material(stoneMat); b.look.color = b.machine.colour; b.look.SetColor("_BaseColor", b.machine.colour); }
            return b.look;
        }

        void MachineLook(BuildingSite b, Transform root)
        {
            root.position = new Vector3(b.hex.center.x, b.Floor, b.hex.center.z);
            Piece(root, PrimitiveType.Cube, woodMat, new Vector3(0f, 0.4f, 0f), new Vector3(1.8f, 0.8f, 1.4f), Vector3.zero);
            if (b.stage < 1) return;
            Piece(root, PrimitiveType.Cylinder, MachineMat(b), new Vector3(0f, 1.0f, 0f), new Vector3(1.0f, 0.3f, 1.0f), Vector3.zero);
            Piece(root, PrimitiveType.Cube, MachineMat(b), new Vector3(0.9f, 0.6f, 0f), new Vector3(0.2f, 0.9f, 0.2f), Vector3.zero);
        }

        void WheelLook(BuildingSite b, Transform root)
        {
            float surface = water.SurfaceAt(b.hex.center.x, b.hex.center.z, out bool wet);
            float axle = (wet ? surface : b.hex.minGround) + 0.5f;
            root.position = new Vector3(b.hex.center.x, axle, b.hex.center.z);
            // two posts and an axle; the paddle wheel turns with the water ("Spin")
            Piece(root, PrimitiveType.Cylinder, woodMat, new Vector3(-0.9f, -0.2f, 0f), new Vector3(0.15f, 1.0f, 0.15f), Vector3.zero);
            Piece(root, PrimitiveType.Cylinder, woodMat, new Vector3(0.9f, -0.2f, 0f), new Vector3(0.15f, 1.0f, 0.15f), Vector3.zero);
            if (b.stage < 1) return;
            var spin = new GameObject("Spin").transform;
            spin.SetParent(root, false);
            Piece(spin, PrimitiveType.Cylinder, woodMat, Vector3.zero, new Vector3(0.12f, 0.95f, 0.12f), new Vector3(0f, 0f, 90f));
            for (int i = 0; i < 6; i++)
                Piece(spin, PrimitiveType.Cube, woodMat, Quaternion.Euler(i * 60f, 0f, 0f) * new Vector3(0f, 0.75f, 0f), new Vector3(1.2f, 0.6f, 0.08f), new Vector3(i * 60f, 0f, 0f));
        }
    }
}
