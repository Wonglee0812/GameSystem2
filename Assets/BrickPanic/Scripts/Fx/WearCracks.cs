using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>Shows joint wear on each brick as cracks that grow (Wong, 2026-11-24). The crack texture's alpha says when
    /// each part of a crack appears (strong first), and the material cuts away everything below 1 - wear, so a little wear
    /// shows short cracks and a lot of wear covers the brick. A thin box with the crack material sits on the brick body.
    /// Wear carries over between rounds, so the player can see in the build phase which walls are weak.</summary>
    public class WearCracks : MonoBehaviour
    {
        [Tooltip("Crack material (URP Unlit, alpha clip, Crack.png): made by \"Brick Panic/Apply Look\".")]
        public Material crackMaterial;
        [Tooltip("No cracks below this wear (0-1). Default 0.08.")]
        [Range(0f, 1f)] public float minWear = 0.08f;
        [Tooltip("Seconds between two wear checks. Default 0.25.")]
        public float checkEvery = 0.25f;

        readonly Dictionary<Block, float> wear = new Dictionary<Block, float>();
        readonly Dictionary<Block, MeshRenderer> overlays = new Dictionary<Block, MeshRenderer>();
        readonly List<Block> gone = new List<Block>();
        MaterialPropertyBlock mpb;
        float next;
        static readonly int CutoffId = Shader.PropertyToID("_Cutoff");

        void Awake() { mpb = new MaterialPropertyBlock(); }

        void Update()
        {
            if (Time.time < next || crackMaterial == null || PhysicsActivator.Instance == null) return;
            next = Time.time + checkEvery;
            bool on = BPJuice.On;
            PhysicsActivator.Instance.CollectWear(wear);

            gone.Clear();
            foreach (var kv in overlays) if (kv.Key == null || kv.Value == null || !wear.ContainsKey(kv.Key) || !on) gone.Add(kv.Key);
            foreach (var b in gone)
            {
                if (overlays.TryGetValue(b, out var r) && r != null) Destroy(r.gameObject);
                overlays.Remove(b);
            }
            if (!on) return;

            foreach (var kv in wear)
            {
                var b = kv.Key;
                if (b == null || kv.Value < minWear) continue;
                if (!overlays.TryGetValue(b, out var r) || r == null) overlays[b] = r = MakeOverlay(b);
                mpb.SetFloat(CutoffId, Mathf.Lerp(0.95f, 0.08f, Mathf.InverseLerp(minWear, 1f, kv.Value)));
                r.SetPropertyBlock(mpb);
            }
        }

        MeshRenderer MakeOverlay(Block b)
        {
            var go = new GameObject("Cracks");
            go.transform.SetParent(b.transform, false);
            Vector3 size = b.shape.BodySize;
            go.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            go.transform.localScale = new Vector3(size.x + 0.02f, size.y + 0.02f, size.z + 0.02f);
            go.AddComponent<MeshFilter>().sharedMesh = MapGenerator.BoxMesh();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = crackMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return r;
        }
    }
}
