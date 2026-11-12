using TMPro;
using UnityEngine;

namespace PokerTD
{
    /// <summary>Short text that rises and fades (hand names, damage, warnings).</summary>
    public class FloatText : MonoBehaviour
    {
        TextMeshPro text;
        float age;
        const float Life = 1.4f;

        public static void Show(string message, Vector3 position, Color color)
        {
            var go = new GameObject("FloatText");
            go.transform.position = position;
            var t = go.AddComponent<TextMeshPro>();
            t.text = message;
            t.fontSize = 5f;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
            t.rectTransform.sizeDelta = new Vector2(8f, 3f);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            go.AddComponent<FloatText>().text = t;
        }

        void Update()
        {
            age += Time.deltaTime;
            transform.position += Vector3.up * Time.deltaTime * 1.2f;
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
            var c = text.color; c.a = 1f - Mathf.Clamp01((age - Life * 0.6f) / (Life * 0.4f)); text.color = c;
            if (age > Life) Destroy(gameObject);
        }
    }
}
