using UnityEngine;

namespace DamRush
{
    /// <summary>
    /// Flood defences (plan v4). Each flood danger has an answer the leader can build:
    /// - Levee: a cheap earth bank (mud) to keep water away from buildings.
    /// - Sandbags: put on a finished dam, levee or floodgate: +0.3 m and 30% less wear, for the
    ///   flood; they wash out two days after it.
    /// - Floodgate: a dam with a gate. Open, the water runs through and the gate does not wear;
    ///   closed, it holds the water like a dam.
    /// - Alarm bell: the leader rings it to send the whole family to the lodge, and gives the
    ///   all clear when the wave is gone. There is no automatic alarm for floods any more.
    /// During a flood, a dam that holds back a lot of water also wears from the pressure:
    /// a full pond before the flood makes a break much more likely.
    /// </summary>
    public partial class BuildOrders
    {
        [Header("Flood defences")]
        public DamDef levee = new DamDef
        {
            kind = DamKind.Levee, name = "Levee", wearFactor = 1f,
            rule = "Cheap earth bank (0.8 m). Put a line of levees around buildings to keep flood water out. Can stand on dry ground.",
            stages = new[] { new DamStage { name = "earth bank", mud = 3, height = 0.8f, leakiness = 0.15f } }
        };
        public DamDef floodgate = new DamDef
        {
            kind = DamKind.Floodgate, name = "Floodgate", wearFactor = 1f,
            rule = "Dam with a gate (1.6 m). Open it (click it) before a flood: the water runs through and the gate does not wear. Close it to hold the water again.",
            stages = new[]
            {
                new DamStage { name = "stone sill", stone = 3, mud = 2, height = 0.6f, leakiness = 0.5f },
                new DamStage { name = "gate", wood = 4, mud = 1, height = 1.6f, leakiness = 0.05f },
            }
        };
        public BuildingDef bellDef = new BuildingDef
        {
            kind = BuildingKind.AlarmBell, name = "Alarm bell",
            rule = "Click it to ring the alarm: the family swims to the lodge. Click again for the all clear. Floods give no alarm by themselves.",
            stages = new[] { new BuildStage { name = "post", wood = 3 }, new BuildStage { name = "bell", stone = 1, mud = 1 } }
        };
        [Tooltip("What one sandbag order costs.")]
        public BuildStage sandbagCost = new BuildStage { name = "sandbags", sand = 2 };
        [Tooltip("Height sandbags add to a dam (m), and how much less it wears with them.")]
        public float sandbagHeight = 0.3f, sandbagWearCut = 0.3f;
        [Tooltip("Game days the sandbags stay after the flood is over.")]
        public float sandbagDaysAfterFlood = 2f;
        [Tooltip("Pressure wear in a flood: condition lost per second per metre of water held above pressureSafe x dam height.")]
        public float pressureWear = 0.04f;
        public float pressureSafe = 0.4f;

        MapController mapRef;
        MapController Map => mapRef != null ? mapRef : (mapRef = FindFirstObjectByType<MapController>());
        FloodSystem Floods => colony != null ? colony.floods : null;

        public static bool IsDefenceDamTool(BuildTool t) => t == BuildTool.Levee || t == BuildTool.Floodgate;

        // ------------------------------------------------------------------ order rules

        bool CanOrderD(BuildTool tool, HexCell c, out string why)
        {
            switch (tool)
            {
                case BuildTool.AlarmBell:
                {
                    if (SiteOn(c) != null) { why = "Something is already planned here"; return false; }
                    foreach (var b in Buildings) if (b.Kind == BuildingKind.AlarmBell) { why = "There is already an alarm bell"; return false; }
                    why = "Alarm bell: click it to send the family to the lodge";
                    return true;
                }
                case BuildTool.Sandbag:
                {
                    var d = TopSiteOn(c) as DamSite;
                    return CanSandbag(d, out why);
                }
            }
            why = "";
            return false;
        }

        public bool CanSandbag(DamSite d, out string why)
        {
            if (d == null) { why = "Click a dam, levee or floodgate"; return false; }
            if (d.kind == DamKind.Spillway) { why = "A spillway is meant to overflow: no sandbags"; return false; }
            if (d.stage < 0) { why = "Nothing built here yet"; return false; }
            if (d.bags) { why = "Sandbags are already on it"; return false; }
            if (d.repair != null) { why = d.bagging ? "Sandbags are on the way" : "A repair is running: wait until it is done"; return false; }
            why = $"Sandbags: +{sandbagHeight:0.0} m and {sandbagWearCut * 100f:0}% less wear until {sandbagDaysAfterFlood:0} days after the flood. Costs {sandbagCost.CostText()}";
            return true;
        }

        public void OrderSandbags(DamSite d)
        {
            if (!CanSandbag(d, out _)) return;
            d.bagging = true;
            d.bagHeight = sandbagHeight;
            d.StartRepair(new BuildStage { name = "sandbags", sand = sandbagCost.sand, mud = sandbagCost.mud, wood = sandbagCost.wood, stone = sandbagCost.stone });
            d.priority = true;
            colony.Log($"Sandbags ordered for the {d.Title.ToLower()} at {d.hex.axial}");
        }

        void OrderD(BuildTool tool, HexCell c)
        {
            if (tool == BuildTool.AlarmBell)
            {
                var site = new BuildingSite(bellDef, c, null, null, water, baseHeight);
                sites.Add(site);
                c.blocked = true; c.building = site.Title;
            }
            else if (tool == BuildTool.Sandbag) OrderSandbags(TopSiteOn(c) as DamSite);
        }

        public void ToggleGate(DamSite d)
        {
            if (d == null || d.kind != DamKind.Floodgate || d.stage < 0) return;
            d.gateOpen = !d.gateOpen;
            d.ApplyToWater();
            UpdateVisual(d);
            colony.Log($"Floodgate at {d.hex.axial} {(d.gateOpen ? "opened: water runs through" : "closed: it holds the water")}");
        }

        public BuildingSite Bell { get { foreach (var b in Buildings) if (b.Kind == BuildingKind.AlarmBell && b.Finished) return b; return null; } }

        // ------------------------------------------------------------------ flood wear (called from UpdateDams)

        /// <summary>Extra wear in a flood from the water a dam holds back. Spillways and open gates do not wear.</summary>
        float PressureWear(DamSite s, float head)
        {
            var m = Map;
            if (m == null || !m.Flooding || m.Phase == FloodPhase.Warning || s.wearFactor <= 0f || s.GateOpen) return 0f;
            float height = s.Crest - s.hex.minGround;
            float excess = head - pressureSafe * height;
            if (excess <= 0f) return 0f;
            float flood = Mathf.Clamp01(m.FlowFactor / Mathf.Max(1f, m.floodStrength));
            return pressureWear * excess * (0.5f + flood);
        }

        float BagFactor(DamSite s) => s.bags ? 1f - sandbagWearCut : 1f;

        /// <summary>Sandbags wash out a few days after the flood.</summary>
        void UpdateSandbags(float dt)
        {
            var m = Map;
            bool flooding = m != null && m.Flooding;
            float day = colony.dayNight != null ? colony.dayNight.dayLength : 240f;
            foreach (var d in Dams)
            {
                if (!d.bags) continue;
                if (flooding) { d.bagTimer = sandbagDaysAfterFlood * day; continue; }
                d.bagTimer -= dt;
                if (d.bagTimer > 0f) continue;
                d.bags = false;
                d.ApplyToWater();
                UpdateVisual(d);
                colony.Log($"The sandbags on the {d.Title.ToLower()} at {d.hex.axial} washed out");
            }
        }

        Material sandMat;
        Material SandMat
        {
            get
            {
                if (sandMat == null) { var c = ItemInfo.Colour(Item.Sand); sandMat = new Material(stoneMat); sandMat.color = c; sandMat.SetColor("_BaseColor", c); }
                return sandMat;
            }
        }

        /// <summary>Extra looks for flood defences: a gate plank, sandbags on top.</summary>
        void DefenceLook(DamSite s, Transform root)
        {
            float top = s.Crest - root.position.y;
            if (s.kind == DamKind.Floodgate && s.stage >= 1)
            {
                // two posts and the gate plank (down when closed, up out of the water when open)
                Piece(root, PrimitiveType.Cube, woodMat, new Vector3(-1.2f, top + 0.6f, 0f), new Vector3(0.25f, 2.2f, 0.25f), Vector3.zero);
                Piece(root, PrimitiveType.Cube, woodMat, new Vector3(1.2f, top + 0.6f, 0f), new Vector3(0.25f, 2.2f, 0.25f), Vector3.zero);
                float plank = s.gateOpen ? top + 1.5f : top - 0.4f;
                Piece(root, PrimitiveType.Cube, woodMat, new Vector3(0f, plank, 0f), new Vector3(2.3f, 0.9f, 0.18f), Vector3.zero);
            }
            if (s.bags)
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 60f;
                    Vector3 p = Quaternion.Euler(0f, a, 0f) * Vector3.forward * 1.1f;
                    Piece(root, PrimitiveType.Capsule, SandMat, new Vector3(p.x, top + 0.12f, p.z), new Vector3(0.35f, 0.4f, 0.35f), new Vector3(90f, a + 90f, 0f));
                }
        }

        void BellLook(BuildingSite b, Transform root)
        {
            root.position = new Vector3(b.hex.center.x, b.hex.maxGround, b.hex.center.z);
            Piece(root, PrimitiveType.Cylinder, woodMat, new Vector3(0f, 1.2f, 0f), new Vector3(0.18f, 1.2f, 0.18f), Vector3.zero);
            Piece(root, PrimitiveType.Cube, woodMat, new Vector3(0f, 2.35f, 0f), new Vector3(1.0f, 0.12f, 0.12f), Vector3.zero);
            if (b.stage < 1) return;
            bool ringing = colony.bellRinging;
            float swing = ringing ? Mathf.Sin(Time.time * 10f) * 25f : 0f;
            Piece(root, PrimitiveType.Sphere, ringing ? SandMat : stoneMat, new Vector3(0.35f, 2.0f, 0f), new Vector3(0.45f, 0.5f, 0.45f), new Vector3(0f, 0f, swing));
        }

        string FunctionD(Site s)
        {
            if (s is DamSite d)
            {
                string t = d.kind == DamKind.Floodgate ? (d.GateOpen ? "Gate OPEN: the water runs through, no wear." : "Gate closed: holds the water like a dam.") : "";
                if (d.bags) t += (t.Length > 0 ? "\n" : "") + $"Sandbags on it (+{sandbagHeight:0.0} m)";
                return t;
            }
            if (s is BuildingSite b && b.Kind == BuildingKind.AlarmBell)
                return colony.bellRinging ? "<color=#ff8a70><b>RINGING</b>: the family stays in the lodge</color>" : "Quiet. Ring it when a flood wave comes.";
            return "";
        }
    }
}
