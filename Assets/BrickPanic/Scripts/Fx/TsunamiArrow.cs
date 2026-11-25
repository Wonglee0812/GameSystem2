using UnityEngine;

namespace BrickPanic
{
    /// <summary>Also shown for the tornado (it crosses the island from the side it comes from).
    /// Forecast of the tsunami's direction (Wong, 2026-11-25): in the build phase, when the next disaster is a
    /// tsunami, a big faded arrow lies on the water side of the island and points the way the wave will go. It flows
    /// forward and fades in and out, so it reads as "water coming from here" without hiding the island.</summary>
    public class TsunamiArrow : MonoBehaviour
    {
        [Tooltip("See-through unlit material with the arrow texture (M_BP_Arrow, made by \"Brick Panic/Apply Look\").")]
        public Material arrowMaterial;
        [Tooltip("Arrow length and width in metres. Default 24.")]
        public float size = 24f;
        [Tooltip("Distance of the arrow's back end from the island edge, out over the water, in metres (below 0 = over the coast). Default -10.")]
        public float outside = -10f;
        [Tooltip("Height above the island base in metres. Default 6.")]
        public float height = 6f;
        [Tooltip("How far the arrow flows forward and back, in metres. Default 2.")]
        public float flow = 2f;
        [Tooltip("Lowest and highest alpha of the pulse. Default 0.6 - 1.")]
        public Vector2 alpha = new Vector2(0.6f, 1f);
        [Tooltip("Arrow colour (alpha comes from the pulse). White reads best against the blue sky.")]
        public Color color = new Color(0.95f, 0.99f, 1f);

        RoundManager round;
        MapGenerator map;
        GameObject arrow;
        MeshRenderer rend;
        MaterialPropertyBlock mpb;
        float shown;
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        void Start()
        {
            round = FindFirstObjectByType<RoundManager>();
            map = FindFirstObjectByType<MapGenerator>();
            arrow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            arrow.name = "TsunamiArrow";
            Destroy(arrow.GetComponent<Collider>());
            arrow.transform.SetParent(transform, false);
            rend = arrow.GetComponent<MeshRenderer>();
            rend.sharedMaterial = arrowMaterial;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            mpb = new MaterialPropertyBlock();
            arrow.SetActive(false);
        }

        void Update()
        {
            if (round == null || map == null || arrowMaterial == null) return;
            bool want = BPJuice.On && round.Current == RoundManager.State.Build && (round.Next is Tsunami || round.Next is Tornado) && !MapBuildIn.Busy;
            shown = Mathf.MoveTowards(shown, want ? 1f : 0f, Time.unscaledDeltaTime * 2.5f);
            if (shown <= 0f) { if (arrow.activeSelf) arrow.SetActive(false); return; }
            if (!arrow.activeSelf) arrow.SetActive(true);

            Vector3 dir = Tsunami.SideDirection(round.NextSide);
            float half = map.size * 0.5f * MapGenerator.Cell;
            float t = Time.unscaledTime;
            float k = Mathf.Repeat(t * 0.6f, 1f);                       // flows forward, then starts again
            Vector3 centre = map.transform.position - dir * (half + outside + size * 0.5f) + dir * (k * flow) + Vector3.up * height;
            arrow.transform.position = centre;
            arrow.transform.rotation = Quaternion.LookRotation(Vector3.down, dir);   // lies flat, texture "up" points along the wave
            arrow.transform.localScale = new Vector3(size, size, 1f);
            float a = Mathf.Lerp(alpha.x, alpha.y, 0.5f + 0.5f * Mathf.Sin(t * 3f)) * Mathf.Sin(k * Mathf.PI) * shown;
            mpb.SetColor(ColorId, new Color(color.r, color.g, color.b, a));
            rend.SetPropertyBlock(mpb);
        }
    }
}
