using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// Between the battle turn and the shop turn the floor changes tile by tile, like the station in
    /// Unrailed (Wong, 2026-11-24): the board's tiles (with towers, machines and decor) fall away one by
    /// one, starting in the centre and moving out; then the shop's tiles come up the same way, centre first.
    /// Going back, the shop falls from the centre and the board fills up again. The grid is wiped away from
    /// the centre outwards (like an eraser) and drawn again from the outside in when the board is back.
    /// Pieces are the visible children of Board/KenneyArt and ShopFloor/KenneyArt, plus towers and machines.
    /// The colliders (Board/Map cubes) never move.
    /// </summary>
    public class BoardTransition : MonoBehaviour
    {
        public static BoardTransition Instance { get; private set; }
        public Transform board;
        public Transform shop;
        public TDConfig config;

        [Header("Tile by tile")]
        [Tooltip("How far a tile falls (it goes behind the background).")]
        public float depth = 16f;
        [Tooltip("Seconds between the centre tile and the outer tiles starting to move.")]
        public float spread = 0.8f;
        [Tooltip("Seconds one tile takes to fall or rise.")]
        public float pieceTime = 0.45f;
        [Tooltip("When the second floor starts, as a part of the first floor's spread (0.5 = halfway).")]
        public float overlap = 0.8f;
        [Tooltip("Seconds to wipe or draw the grid.")]
        public float gridTime = 0.6f;

        public bool Busy { get; private set; }

        GridOverlay grid;

        class Piece { public Transform t; public Vector3 rest; public float delay; }

        void Awake()
        {
            Instance = this;
            shop.position = new Vector3(shop.position.x, 0f, shop.position.z);   // the shop floor stands where the board is
            shop.gameObject.SetActive(false);
            grid = board.GetComponentInChildren<GridOverlay>(true);
        }

        public void Sink(Action done = null) => StartCoroutine(Swap(board, shop, true, done));
        public void Rise(Action done = null) => StartCoroutine(Swap(shop, board, false, done));

        IEnumerator Swap(Transform from, Transform to, bool toShop, Action done)
        {
            Busy = true;
            // 1) wipe the grid from the centre out (board -> shop)
            if (toShop && grid != null) StartCoroutine(GridTo(0f, grid.FullRadius));

            var down = Pieces(from);
            to.gameObject.SetActive(true);
            var up = Pieces(to);
            foreach (var p in up) Place(p, -depth);   // under the floor before the first frame

            float t = 0f;
            float upStart = spread * overlap + pieceTime * 0.5f;
            float end = upStart + spread + pieceTime;
            while (t < end)
            {
                t += Time.deltaTime;
                foreach (var p in down)
                {
                    float k = Mathf.Clamp01((t - p.delay) / pieceTime);
                    Place(p, -depth * k * k * k);                    // ease in: slow start, fast fall
                }
                foreach (var p in up)
                {
                    float k = Mathf.Clamp01((t - upStart - p.delay) / pieceTime);
                    Place(p, Rise(k));                                 // rise with a small fixed pop
                }
                yield return null;
            }
            foreach (var p in down) Place(p, 0f);
            foreach (var p in up) Place(p, 0f);
            from.gameObject.SetActive(false);

            // 2) draw the grid again from the outside in (shop -> board)
            if (!toShop && grid != null) yield return GridTo(grid.FullRadius, 0f);
            Busy = false;
            done?.Invoke();
        }

        IEnumerator GridTo(float a, float b)
        {
            float t = 0f;
            grid.holeRadius = a;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, gridTime));
                grid.holeRadius = Mathf.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            grid.holeRadius = b;
        }

        static void Place(Piece p, float y)
        {
            if (p.t != null) p.t.localPosition = p.rest + Vector3.up * y;
        }

        [Tooltip("How far a rising tile pops above its place before it settles (metres).")]
        public float pop = 0.25f;

        /// <summary>Height while rising: fast from below, a small pop above the floor, then settle at 0.</summary>
        float Rise(float k)
        {
            if (k >= 1f) return 0f;
            float u = 1f - k;
            return -depth * u * u * u + pop * Mathf.Sin(Mathf.PI * Mathf.Clamp01((k - 0.55f) / 0.45f));
        }

        /// <summary>The moving parts of a floor, each with a start delay from its distance to the centre.</summary>
        List<Piece> Pieces(Transform floor)
        {
            var list = new List<Piece>();
            var art = floor.Find("KenneyArt");
            if (art != null)
                foreach (Transform group in art)
                {
                    if (group.GetComponent<GridOverlay>() != null) continue;     // the grid has its own effect
                    if (group.childCount > 0 && group.GetComponent<Renderer>() == null && group.name != "Castle" && !group.name.StartsWith("Zone"))
                        foreach (Transform c in group) Add(list, c);             // a folder (Tiles, SpawnAndBase)
                    else Add(list, group);
                }
            foreach (Transform c in floor)
                if (c.GetComponent<Tower>() != null || c.GetComponent<BuffMachine>() != null) Add(list, c);
            float max = 0.01f;
            foreach (var p in list) max = Mathf.Max(max, p.delay);
            foreach (var p in list) p.delay = p.delay / max * spread;
            return list;
        }

        void Add(List<Piece> list, Transform t)
        {
            Vector3 w = t.position; w.y = 0f;
            Vector3 c = board.position; c.y = 0f;
            // rest = the normal height (all floors rest at local y of their own layout)
            list.Add(new Piece { t = t, rest = RestOf(t), delay = Vector3.Distance(w, c) });
        }

        readonly Dictionary<Transform, Vector3> rests = new Dictionary<Transform, Vector3>();

        /// <summary>Remembers the first known resting position (a piece may be mid-fall when asked again).</summary>
        Vector3 RestOf(Transform t)
        {
            if (!rests.TryGetValue(t, out var r)) { r = t.localPosition; rests[t] = r; }
            return r;
        }
    }
}
