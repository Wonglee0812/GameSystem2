using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrickPanic
{
    /// <summary>The few DOTween UI shortcuts Brick Panic needs. DOTween's own UI module is a loose script in
    /// Assets/Plugins, which an assembly definition (BrickPanic.asmdef) cannot see, so the same tweens are made
    /// here with DOTween.To. The target is the component, so DOKill() on it stops them.</summary>
    public static class BPTween
    {
        public static Tweener DOAnchorPos(this RectTransform t, Vector2 to, float time) =>
            DOTween.To(() => t.anchoredPosition, v => t.anchoredPosition = v, to, time).SetTarget(t);

        public static Tweener DOAnchorPosY(this RectTransform t, float y, float time) =>
            DOTween.To(() => t.anchoredPosition.y, v => t.anchoredPosition = new Vector2(t.anchoredPosition.x, v), y, time).SetTarget(t);

        public static Tweener DOFade(this Graphic g, float alpha, float time) =>
            DOTween.To(() => g.color.a, a => { var c = g.color; c.a = a; g.color = c; }, alpha, time).SetTarget(g);

        public static Tweener DOFade(this TMP_Text t, float alpha, float time) =>
            DOTween.To(() => t.alpha, a => t.alpha = a, alpha, time).SetTarget(t);

        public static Tweener DOShakeAnchorPos(this RectTransform t, float time, Vector2 strength, int vibrato = 10, float randomness = 90f)
        {
            Vector2 home = t.anchoredPosition;
            return DOTween.Shake(() => (Vector3)t.anchoredPosition, v => t.anchoredPosition = v, time, strength, vibrato, randomness, true)
                .SetTarget(t).OnKill(() => { if (t != null) t.anchoredPosition = home; });
        }
    }
}
