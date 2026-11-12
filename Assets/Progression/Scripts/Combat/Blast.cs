using UnityEngine;

namespace PokerTD
{
    /// <summary>Short flash on the ground where a cannon shell lands: shows the size of the blast.</summary>
    public class Blast : MonoBehaviour
    {
        RangeRing ring;
        float age;
        const float Life = 0.35f;

        public static void Show(Vector3 at, float radius)
        {
            var go = new GameObject("Blast");
            go.transform.position = new Vector3(at.x, 0.1f, at.z);
            var b = go.AddComponent<Blast>();
            b.ring = RangeRing.Create(go.transform, radius, new Color(1f, 0.6f, 0.2f, 1f), 32, 0.25f);
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = age / Life;
            transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, t);
            ring.SetColor(new Color(1f, 0.6f, 0.2f, 1f - t));
            if (age > Life) Destroy(gameObject);
        }
    }
}
