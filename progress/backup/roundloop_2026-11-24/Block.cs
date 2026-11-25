using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrickPanic
{
    /// <summary>One placed brick. It is a static collider while building and gets a Rigidbody when the disaster starts.</summary>
    public class Block : MonoBehaviour
    {
        [Tooltip("Shape of this brick (set by BlockFactory).")]
        public ShapeDef shape;
        [Tooltip("Material of this brick (set by BlockFactory).")]
        public MaterialDef material;

        [Tooltip("True = the player placed it (Ctrl+Z gives the full price back). False = a map piece.")]
        public bool placedByPlayer;
        [Tooltip("True = the player already paid for it (moving a brick is free, so a moved brick keeps this).")]
        public bool paid;

        [HideInInspector] public int quarterTurns;
        public Color? tint;

        /// <summary>Price of this brick (shape x material).</summary>
        public float Price => shape.Price(material);

        public Rigidbody Body { get; private set; }
        public BoxCollider Box { get; private set; }

        public static readonly List<Block> All = new List<Block>();
        public static int JointsMade, JointsBroken;
        /// <summary>Broken joints per material name and the biggest force seen (for tuning).</summary>
        public static readonly Dictionary<string, Vector2> BreakLog = new Dictionary<string, Vector2>();

        void Awake() { Box = GetComponent<BoxCollider>(); }
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>Adds a Rigidbody with the mass from shape x material.</summary>
        public Rigidbody MakeDynamic(int solverIterations)
        {
            if (Body != null) return Body;
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = shape.Mass(material);
            Body.solverIterations = solverIterations;
            Body.interpolation = RigidbodyInterpolation.None;
            return Body;
        }

        /// <summary>Called by the wear system when a joint is used up (OnJointBreak is not called for that).</summary>
        public void WornOut()
        {
            JointsBroken++;
            string key = material.displayName + " (worn)";
            BreakLog.TryGetValue(key, out var v);
            BreakLog[key] = new Vector2(v.x + 1, v.y);
        }

        bool burning;

        /// <summary>Lava set this brick on fire: it turns dark and is gone after the material's heat time.
        /// Materials with heatSeconds = 0 (stone, steel) do not burn.</summary>
        public void Ignite()
        {
            if (burning || material.heatSeconds <= 0f) return;
            burning = true;
            StartCoroutine(Burn(material.heatSeconds));
        }

        IEnumerator Burn(float seconds)
        {
            var r = GetComponent<MeshRenderer>();
            var mpb = new MaterialPropertyBlock();
            Color start = r.sharedMaterial.GetColor("_BaseColor");
            Color burnt = new Color(0.12f, 0.1f, 0.09f);
            float nextFlame = 0f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = t / seconds;
                if (t >= nextFlame)
                {
                    nextFlame = t + 0.12f;
                    BrickFx.Flame(transform.TransformPoint(Box != null ? Box.center : Vector3.zero) + Vector3.up * 0.3f, 0.45f);
                }
                mpb.SetColor("_BaseColor", Color.Lerp(Color.Lerp(start, new Color(1f, 0.45f, 0.1f), 0.6f), burnt, k));
                r.SetPropertyBlock(mpb);
                yield return null;
            }
            Burned++;
            Destroy(gameObject);
        }

        /// <summary>Bricks burned away by lava.</summary>
        public static int Burned;

        /// <summary>Bricks and people that fall below this height are lost.</summary>
        public static int Lost;

        void OnJointBreak(float force)
        {
            JointsBroken++;
            string key = material.displayName + (Time.timeSinceLevelLoad < SettleUntil ? " (settle)" : "");
            BreakLog.TryGetValue(key, out var v);
            BreakLog[key] = new Vector2(v.x + 1, Mathf.Max(v.y, force));
        }

        /// <summary>Breaks before this time (s) count as "settle" breaks in the log.</summary>
        public static float SettleUntil;
    }
}
