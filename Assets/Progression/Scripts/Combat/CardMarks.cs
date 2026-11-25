using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PokerTD
{
    /// <summary>
    /// The cards stuck on one enemy. Each new card restarts a short timer; when the
    /// timer runs out (or 5 cards are on it) the best poker hand bursts for damage.
    /// - The same card (same rank and suit) only marks once; hitting again does not restart the timer.
    /// - The timer gets shorter as more cards are on the enemy (config.markWait).
    /// - The burst uses the buffs of the tower that added the last card.
    /// - Every hit also does the card's own value as damage at once (Card.Value).
    /// - Test option (TDGame.BurstAtBestHand): burst at once when the marks already make the best
    ///   hand the tower cards can reach, because waiting cannot give a higher hand. Only for Pair or higher.
    /// </summary>
    public class CardMarks : MonoBehaviour
    {
        public IReadOnlyList<Card> Cards => cards;
        public float TimeLeft => timer;

        readonly List<Card> cards = new List<Card>();
        Enemy enemy;
        Tower lastTower;
        float timer;
        TextMeshPro text;
        MarkRow row;   // the small card sprites above the head
        float lockedUntil;

        /// <summary>Seconds after a burst in which no new card can mark this enemy (the burst animation
        /// is still playing; a card added then looked like part of the old hand). Hits still do damage.</summary>
        public const float BurstLock = 0.6f;
        Transform timerBar;

        public void Init(Enemy owner, Transform head)
        {
            enemy = owner;
            var go = new GameObject("Marks");
            go.transform.SetParent(head, false);
            go.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            text = go.AddComponent<TextMeshPro>();
            text.fontSize = 4.5f;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(3f, 0.5f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontStyle = FontStyles.Bold;
            text.enabled = false;   // the cards are shown as sprites now (MarkRow)
            row = MarkRow.Create(head, Vector3.up * 0.55f);
            var bar = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(bar.GetComponent<Collider>());
            bar.name = "MarkTimer";
            bar.transform.SetParent(head, false);
            bar.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            var r = bar.GetComponent<Renderer>();
            r.sharedMaterial = enemy.BarMaterial;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", new Color(1f, 0.85f, 0.3f));
            r.SetPropertyBlock(block);
            timerBar = bar.transform;
            Refresh();
        }

        public void AddMark(Card card, Tower from)
        {
            if (enemy == null || enemy.Health <= 0f) return;
            var config = TDGame.Instance.config;
            Juice.CardHit(enemy, card);
            enemy.TakeDamage(card.Value);              // the hit itself
            if (enemy == null || enemy.Health <= 0f) return;
            if (Time.time < lockedUntil) return;       // burst still playing: no new marks
            if (cards.Contains(card))
            {
                if (config.sameCardResetsTimer) timer = config.WaitAfter(cards.Count);   // old rule
                row.Hit(card);
                return;   // same card: no new mark, the burst timer keeps running
            }
            cards.Add(card);
            lastTower = from;
            if (cards.Count >= 5) { Burst(); return; }
            var game = TDGame.Instance;
            var now = game.Effective(HandEvaluator.Evaluate(cards));
            // High Card never bursts early: one gatling with High Card was strong enough to win (playtest 2026-11-12)
            if (game.BurstAtBestHand && now > HandType.HighCard && now >= game.BestReachableHand) { Burst(); return; }
            timer = config.WaitAfter(cards.Count);
            Refresh();
        }

        void Update()
        {
            if (cards.Count == 0) return;
            timer -= Time.deltaTime;
            float full = TDGame.Instance.config.WaitAfter(cards.Count);
            timerBar.localScale = new Vector3(1.14f * Mathf.Clamp01(timer / full), 0.05f, 1f);
            row.SetUrgency(1f - Mathf.Clamp01(timer / full));
            if (timer <= 0f) Burst();
        }

        /// <summary>Bursts the marks now (Detonate item). Does nothing without marks.</summary>
        public void BurstNow() { if (cards.Count > 0) Burst(); }

        void Burst()
        {
            var hand = HandEvaluator.Evaluate(cards);
            var result = DamageCalc.Burst(hand, lastTower);
            if (!Juice.Burst(transform.position, result, cards))   // juice shows chips x mult itself
                FloatText.Show($"{HandEvaluator.Name(result.hand)}\n<size=80%>{result.damage:#,0}</size>",
                               transform.position + Vector3.up * 2.4f, HandColor(result.hand));
            row.Sync(cards);   // the card that caused the burst gets its sprite first, so the whole hand bursts
            row.BurstAll();
            lockedUntil = Time.time + BurstLock;
            cards.Clear();
            Refresh();
            enemy.TakeDamage(result.damage);
        }

        void Refresh()
        {
            if (text == null) return;
            var parts = new string[cards.Count];
            for (int i = 0; i < cards.Count; i++) parts[i] = cards[i].Rich;
            text.text = string.Join(" ", parts);
            if (row != null) row.Sync(cards);
            timerBar.gameObject.SetActive(cards.Count > 0);
        }

        /// <summary>The enemy is gone: its card sprites burn away on their own.</summary>
        void OnDestroy()
        {
            if (row != null) row.Release();
        }

        static Color HandColor(HandType hand) =>
            hand >= HandType.FourOfAKind ? new Color(1f, 0.3f, 0.8f) :
            hand >= HandType.Straight ? new Color(1f, 0.75f, 0.2f) :
            hand >= HandType.Pair ? new Color(0.5f, 0.85f, 1f) : Color.white;
    }
}
