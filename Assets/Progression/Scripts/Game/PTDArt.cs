using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// Links to the 3D models of the Kenney Tower Defense Kit (CC0) that the game spawns at runtime:
    /// tower parts, weapons, buff machine parts, UFO enemies and ammo.
    /// The asset is Art/Resources/PTDArt.asset; the menu "Poker TD/Apply Kenney Art" creates and fills it.
    /// When the asset is missing, towers, enemies and machines fall back to the graybox primitives.
    /// </summary>
    [CreateAssetMenu(menuName = "Poker TD/Art Set")]
    public class PTDArt : ScriptableObject
    {
        [Header("Towers (body parts are stacked from the ground up)")]
        public GameObject cannonBase;
        public GameObject cannonWeapon;
        public GameObject gatlingBase;
        public GameObject gatlingTop;
        public GameObject gatlingWeapon;
        public GameObject trapBase;
        public GameObject trapWeapon;
        [Tooltip("Material for the spikes of the trap (made in code).")]
        public Material spikeMaterial;

        [Header("Buff machines")]
        public GameObject machineBase;
        public GameObject machineCrystals;

        [Header("Enemies")]
        public GameObject ufoNormal;
        public GameObject ufoSpeed;
        public GameObject ufoTank;
        public GameObject ufoBoom;

        [Header("Ammo")]
        public GameObject cannonball;
        public GameObject bullet;

        static PTDArt instance;
        public static PTDArt I
        {
            get
            {
                if (instance == null) instance = Resources.Load<PTDArt>("PTDArt");
                return instance;
            }
        }

        public GameObject Ufo(string enemyName)
        {
            switch (enemyName)
            {
                case "Speed": return ufoSpeed;
                case "Tank": return ufoTank;
                case "Boom": return ufoBoom;
                default: return ufoNormal;
            }
        }

        /// <summary>Spawns a model under a parent, without colliders.</summary>
        public static Transform Place(GameObject model, Transform parent, Vector3 localPos, float scale, float yaw = 0f)
        {
            if (model == null) return null;
            var go = Instantiate(model, parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            return go.transform;
        }

        /// <summary>Puts one part on top of the stack (local height y) and returns the new top height.</summary>
        public static float Stack(Transform parent, float y, float scale, GameObject part, out Transform placed)
        {
            placed = Place(part, parent, Vector3.up * y, scale);
            if (placed == null) return y;
            var rs = placed.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return y;
            Bounds b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return parent.InverseTransformPoint(b.max).y;
        }

        public static float Stack(Transform parent, float y, float scale, params GameObject[] parts)
        {
            foreach (var p in parts) y = Stack(parent, y, scale, p, out _);
            return y;
        }

        /// <summary>Tints every renderer under t (the Kenney colour map is multiplied by the colour).</summary>
        public static void Tint(Transform t, Color c)
        {
            if (t == null) return;
            var block = new MaterialPropertyBlock();
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", c);
                r.SetPropertyBlock(block);
            }
        }
    }
}
