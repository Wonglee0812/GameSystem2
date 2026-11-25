using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PokerTD.EditorTools
{
    /// <summary>
    /// Dresses the Poker TD board with the Kenney Tower Defense Kit (CC0):
    /// grass and path tiles (straight, corner, end) picked from the path shape, small decorations,
    /// a spawn hole at the start of the path, a castle at the end, and the PTDArt asset that
    /// towers, enemies, machines and ammo use at runtime.
    /// The old cubes stay for clicks (renderer off). Menu: Poker TD/Apply Kenney Art. Safe to run twice.
    /// </summary>
    public static class PokerTDKenneyBuilder
    {
        const string Kit = "Assets/Progression/Art/Kenney_TD/";
        const string ArtAsset = "Assets/Progression/Art/Resources/PTDArt.asset";
        const int Cols = 14, Rows = 9;
        const float Tile = 2f;
        const int Seed = 7;

        static readonly Vector2Int N = Vector2Int.up, S = Vector2Int.down, E = Vector2Int.right, W = Vector2Int.left;

        static GameObject M(string name)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + name + ".fbx");
            if (go == null) Debug.LogError("Kenney model missing: " + name);
            return go;
        }

        [MenuItem("Poker TD/Apply Kenney Art")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
            MakeArtAsset();
            var board = GameObject.Find("Board");
            if (board == null) { Debug.LogError("No Board in the open scene (open PokerTD.unity)."); return; }
            DressBoard(board.transform);
            var move = Object.FindFirstObjectByType<BoardTransition>(FindObjectsInactive.Include);
            if (move != null && move.shop != null)
            {
                PokerTDShopBuilder.BuildFloor(move.shop);   // the 3D shop (zones) replaces the old pedestals
                move.shop.gameObject.SetActive(false);   // hidden in edit mode (it overlaps the board); BoardTransition shows it in the shop turn
            }
            EditorSceneManager.MarkSceneDirty(board.scene);
            Debug.Log("Poker TD: Kenney art applied. Save the scene to keep it.");
        }

        static void MakeArtAsset()
        {
            var a = AssetDatabase.LoadAssetAtPath<PTDArt>(ArtAsset);
            if (a == null) { a = ScriptableObject.CreateInstance<PTDArt>(); AssetDatabase.CreateAsset(a, ArtAsset); }
            a.cannonBase = M("tower-round-bottom-a");
            a.cannonWeapon = M("weapon-cannon");
            a.gatlingBase = M("tower-square-bottom-a");
            a.gatlingTop = M("tower-square-top-a");
            a.gatlingWeapon = M("weapon-turret");
            a.trapBase = M("spawn-round");   // metal plate; the spikes are made in code (Tower.BuildSpikes)
            a.trapWeapon = null;
            a.spikeMaterial = SpikeMaterial();
            a.machineBase = M("tower-round-bottom-b");
            a.machineCrystals = M("tower-round-crystals");
            a.ufoNormal = M("enemy-ufo-a");
            a.ufoSpeed = M("enemy-ufo-b");
            a.ufoTank = M("enemy-ufo-c");
            a.ufoBoom = M("enemy-ufo-d");
            a.cannonball = M("weapon-ammo-cannonball");
            a.bullet = M("weapon-ammo-bullet");
            EditorUtility.SetDirty(a);
            AssetDatabase.SaveAssets();
        }

        static Material SpikeMaterial()
        {
            const string path = "Assets/Progression/Materials/M_TD_Spike.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", new Color(0.78f, 0.8f, 0.9f));   // same light steel as the Kenney metal
            m.SetFloat("_Smoothness", 0.55f);
            m.SetFloat("_Metallic", 0.3f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Vector3 Cell(Vector2Int c) => new Vector3((c.x - (Cols - 1) / 2f) * Tile, 0f, (c.y - (Rows - 1) / 2f) * Tile);
        static Vector2Int CellOf(Vector3 p) => new Vector2Int(Mathf.RoundToInt(p.x / Tile + (Cols - 1) / 2f), Mathf.RoundToInt(p.z / Tile + (Rows - 1) / 2f));

        static Transform Put(GameObject model, Transform parent, Vector3 pos, float yaw, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            // not static: the board and the shop floor move between turns
            return go.transform;
        }

        // Kenney path tiles at yaw 0: straight = N-S, corner = N+W, end = open to N.
        // Unity yaw turns N to E (90), S (180), W (270).
        static float YawFor(Vector2Int dir) => dir == N ? 0f : dir == E ? 90f : dir == S ? 180f : 270f;

        static void PathTile(HashSet<Vector2Int> open, out string model, out float yaw)
        {
            bool n = open.Contains(N), s = open.Contains(S), e = open.Contains(E), w = open.Contains(W);
            if (open.Count == 1) { model = "tile-end-round"; yaw = YawFor(new List<Vector2Int>(open)[0]); return; }
            if (n && s) { model = "tile-straight"; yaw = 0f; return; }
            if (e && w) { model = "tile-straight"; yaw = 90f; return; }
            model = "tile-corner-round";
            yaw = n && w ? 0f : e && n ? 90f : s && e ? 180f : 270f;
        }

        static void DressBoard(Transform board)
        {
            var old = board.Find("KenneyArt");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("KenneyArt").transform;
            root.SetParent(board, false);
            var tiles = new GameObject("Tiles").transform; tiles.SetParent(root, false);
            var extra = new GameObject("SpawnAndBase").transform; extra.SetParent(root, false);
            var rng = new System.Random(Seed);

            // read the grid from the cube names (Path_c_r / Node_c_r)
            var map = board.Find("Map");
            var nodes = new Dictionary<Vector2Int, BuildNode>();
            var path = new HashSet<Vector2Int>();
            foreach (Transform t in map)
            {
                var parts = t.name.Split('_');
                if (parts.Length != 3) continue;
                var c = new Vector2Int(int.Parse(parts[1]), int.Parse(parts[2]));
                var node = t.GetComponent<BuildNode>();
                if (node == null) continue;
                nodes[c] = node;
                if (node.isPath) path.Add(c);
            }

            // route: start, end, and a spawn waypoint one tile outside the board
            var route = board.GetComponentInChildren<PathRoute>();
            var pts = new List<Transform>(route.points);
            if (pts[0].name == "Waypoint_Spawn") pts.RemoveAt(0);
            Vector2Int start = CellOf(pts[0].position), end = CellOf(pts[pts.Count - 1].position);
            Vector2Int outDir = start.x == 0 ? W : start.x == Cols - 1 ? E : start.y == 0 ? S : N;
            Vector2Int spawn = start + outDir;
            var spawnWp = route.transform.Find("Waypoint_Spawn");
            if (spawnWp == null) { spawnWp = new GameObject("Waypoint_Spawn").transform; spawnWp.SetParent(route.transform, false); }
            spawnWp.SetSiblingIndex(0);
            spawnWp.position = Cell(spawn);
            pts.Insert(0, spawnWp);
            Undo.RecordObject(route, "Kenney route");
            route.points = pts.ToArray();
            EditorUtility.SetDirty(route);

            string[] decoModels = { "detail-tree", "detail-tree", "detail-rocks", "detail-dirt", "detail-tree-large", "detail-crystal" };
            foreach (var kv in nodes)
            {
                Vector2Int c = kv.Key;
                var node = kv.Value;
                Undo.RecordObject(node, "Kenney tile");
                Undo.RecordObject(node.transform, "Kenney tile");
                var cubeR = node.GetComponent<Renderer>();
                if (cubeR != null) cubeR.enabled = false;
                // every cube: top at y = 0.2 (the Kenney tile surface), full tile width for clicks
                node.transform.position = new Vector3(node.transform.position.x, 0f, node.transform.position.z);
                node.transform.localScale = new Vector3(Tile, 0.4f, Tile);

                string model = "tile";
                float yaw = 0f;
                if (node.isPath)
                {
                    var open = new HashSet<Vector2Int>();
                    foreach (var d in new[] { N, S, E, W })
                        if (path.Contains(c + d) || c + d == spawn) open.Add(d);
                    PathTile(open, out model, out yaw);
                }
                var tile = Put(M(model), tiles, Cell(c) + Vector3.down * 0.2f, yaw, Tile);
                tile.name = (node.isPath ? "Path_" : "Grass_") + c.x + "_" + c.y;
                node.art = tile.GetComponentsInChildren<Renderer>();
                node.deco = null;
                if (!node.isPath && rng.NextDouble() < 0.22)
                {
                    var d = Put(M(decoModels[rng.Next(decoModels.Length)]), tile,
                        tile.position + new Vector3((float)(rng.NextDouble() - 0.5) * 1.1f, 0.4f, (float)(rng.NextDouble() - 0.5) * 1.1f),
                        rng.Next(4) * 90f, 0.7f + (float)rng.NextDouble() * 0.35f);   // parent tile is already x2
                    d.name = "Deco";
                    node.deco = d.gameObject;
                }
                EditorUtility.SetDirty(node);
            }

            // grid lines on top of the tiles, so each build tile is easy to see
            var grid = new GameObject("Grid", typeof(MeshFilter), typeof(MeshRenderer), typeof(GridOverlay));
            grid.transform.SetParent(root, false);
            var go = grid.GetComponent<GridOverlay>();
            go.cols = Cols; go.rows = Rows; go.tile = Tile;

            // spawn: a spawn hole tile outside the board, open towards the path, with trees beside it
            Put(M("tile-spawn-end"), extra, Cell(spawn) + Vector3.down * 0.2f, YawFor(-outDir), Tile).name = "SpawnTile";
            Vector2Int side = outDir == W || outDir == E ? N : E;
            Put(M("tile-tree-double"), extra, Cell(spawn + side) + Vector3.down * 0.2f, 0f, Tile).name = "SpawnSideA";
            Put(M("tile-rock"), extra, Cell(spawn - side) + Vector3.down * 0.2f, 90f, Tile).name = "SpawnSideB";

            // castle at the end of the path (the base the enemies attack)
            var castle = new GameObject("Castle").transform;
            castle.SetParent(extra, false);
            Vector3 cp = Cell(end) + Vector3.up * 0.2f;
            castle.position = cp;
            float y = 0f;
            foreach (var part in new[] { "tower-square-bottom-c", "tower-square-middle-b", "tower-square-roof-a" })
            {
                var p = Put(M(part), castle, cp + Vector3.up * y, 0f, 1.7f);
                var rs = p.GetComponentsInChildren<Renderer>();
                Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                y = b.max.y - cp.y;
            }

            foreach (var n in new[] { "Spawn", "Base" })
            {
                var g = board.Find(n);
                var r = g != null ? g.GetComponent<Renderer>() : null;
                if (r != null) { Undo.RecordObject(r, "Kenney"); r.enabled = false; }
            }
        }

        /// <summary>Shop floor: a dirt yard with grass around it, wood fences at the back and
        /// round tower bases with crystals where the pedestals were.</summary>
        static void DressShop(Transform shop)
        {
            var old = shop.Find("KenneyArt");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("KenneyArt").transform;
            root.SetParent(shop, false);
            var rng = new System.Random(Seed + 1);
            for (int c = 0; c < Cols; c++)
                for (int r = 0; r < Rows; r++)
                {
                    bool edge = c == 0 || r == 0 || c == Cols - 1 || r == Rows - 1;
                    string model = edge ? (rng.NextDouble() < 0.35 ? "tile-tree" : "tile") : "tile-dirt";
                    var t = Put(M(model), root, shop.position + Cell(new Vector2Int(c, r)) + Vector3.down * 0.2f, rng.Next(4) * 90f, Tile);
                    t.name = "Shop_" + c + "_" + r;
                }
            for (int c = 1; c < Cols - 1; c += 2)
                Put(M("wood-structure"), root, shop.position + Cell(new Vector2Int(c, Rows - 2)) + Vector3.up * 0.2f, 0f, Tile).name = "Fence";
            foreach (Transform p in shop)
            {
                if (!p.name.StartsWith("Pedestal") && p.name != "Stage") continue;
                var r = p.GetComponent<Renderer>();
                if (r != null) { Undo.RecordObject(r, "Kenney"); r.enabled = false; }
                if (p.name == "Stage") continue;
                var basePos = new Vector3(p.position.x, shop.position.y + 0.2f, p.position.z);
                var b = Put(M("tower-round-bottom-c"), root, basePos, 0f, 1.6f);
                b.name = p.name + "_Kenney";
                Put(M("tower-round-crystals"), b, basePos + Vector3.up * 0.96f, 0f, 1f);
            }
        }
    }
}
