using TMPro;
using UnityEngine;

namespace WindPet
{
    /// <summary>Short text that floats up and fades, for feedback like "+€4.80" or "No ink".</summary>
    public class FloatingText : MonoBehaviour
    {
        TextMeshPro text;
        float age;
        const float Life = 1.4f;
        Color color;

        public static void Show(string message, Vector3 position, Color color)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = position;
            var t = go.AddComponent<TextMeshPro>();
            t.text = message;
            t.fontSize = 2.4f;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
            t.outlineWidth = 0.25f;
            t.outlineColor = new Color32(43, 26, 16, 255);
            t.rectTransform.sizeDelta = new Vector2(6f, 1f);
            var f = go.AddComponent<FloatingText>();
            f.text = t;
            f.color = color;
        }

        void LateUpdate()
        {
            age += Time.deltaTime;
            transform.position += Vector3.up * 0.5f * Time.deltaTime;
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;   // always face the camera
            var c = color;
            c.a = 1f - Mathf.Clamp01((age - Life * 0.6f) / (Life * 0.4f));
            text.color = c;
            if (age > Life) Destroy(gameObject);
        }
    }
}
