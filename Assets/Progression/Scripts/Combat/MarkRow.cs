using System.Collections.Generic;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// The small cards above an enemy's head (its marks). It is not a child of the enemy:
    /// it follows the head every frame, so when the enemy dies its cards can still burn
    /// away on screen. New cards pop in; a hit punches the cards; the cards tremble
    /// harder as the burst timer runs out.
    /// </summary>
    public class MarkRow : MonoBehaviour
    {
        public float cardWidth = 0.5f;
        public float spacing = 0.34f;
        public float fanAngle = 7f;

        readonly List<WorldCard> cards = new List<WorldCard>();
        Transform follow;
        Vector3 offset;

        public static MarkRow Create(Transform head, Vector3 offset)
        {
            var row = new GameObject("MarkRow").AddComponent<MarkRow>();
            row.follow = head;
            row.offset = offset;
            row.LateUpdate();
            return row;
        }

        /// <summary>Shows the given cards. Cards that are new pop in; the others get a small punch.</summary>
        public void Sync(IReadOnlyList<Card> marks)
        {
            bool added = false;
            for (int i = 0; i < marks.Count; i++)
            {
                if (Find(marks[i]) != null) continue;
                var wc = WorldCard.Create(transform, Vector3.zero, cardWidth, 20 + cards.Count);
                wc.SetCard(marks[i]);
                wc.PopIn();
                cards.Add(wc);
                added = true;
            }
            if (added) foreach (var c in cards) if (!c.Busy) c.Punch(0.5f);
            Layout();
        }

        /// <summary>The enemy was hit again by a card it already carries: punch that card.</summary>
        public void Hit(Card card)
        {
            var wc = Find(card);
            if (wc != null) wc.Punch(0.8f);
        }

        /// <summary>0..1: how close the burst is. Trembling starts in the second half of the timer.</summary>
        public void SetUrgency(float urgency)
        {
            float shake = Mathf.Clamp01((urgency - 0.5f) / 0.5f);
            foreach (var c in cards) c.Shake = shake;
        }

        /// <summary>All cards burst (hard shake, hold, dissolve), one after the other.</summary>
        public void BurstAll()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                c.transform.SetParent(null, true);   // stays in place even if the enemy dies now
                c.Burst(i * 0.04f);
            }
            cards.Clear();
        }

        /// <summary>The enemy is gone: its cards burn away, then the row is removed.</summary>
        public void Release()
        {
            follow = null;
            foreach (var c in cards)
            {
                c.transform.SetParent(null, true);
                c.DissolveOut(true);
            }
            cards.Clear();
            Destroy(gameObject);
        }

        WorldCard Find(Card card)
        {
            foreach (var c in cards) if (c != null && c.Card.Equals(card)) return c;
            return null;
        }

        void Layout()
        {
            int n = cards.Count;
            for (int i = 0; i < n; i++)
            {
                float mid = i - (n - 1) / 2f;
                var t = cards[i].transform;
                t.localPosition = new Vector3(mid * spacing, -mid * mid * 0.02f, -i * 0.01f);
                cards[i].Roll = -mid * fanAngle;
            }
        }

        void LateUpdate()
        {
            if (follow == null) return;
            transform.position = follow.position + offset;
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;   // so the row runs left-right on screen
        }
    }
}
