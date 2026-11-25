"""Poker TD: burst lock + full burst visual (CardMarks), click a tower to give it the selected card
(CardHand, BuildManager). Run from the project root. Safe to run twice."""
import sys

def patch(path, edits):
    s = open(path, encoding='utf-8').read()
    for old, new, marker in edits:
        if marker in s:
            continue
        if old not in s:
            sys.exit(f'NOT FOUND in {path}: {old[:70]!r}')
        s = s.replace(old, new, 1)
    open(path, 'w', encoding='utf-8').write(s)
    print('patched', path)

patch('Assets/Progression/Scripts/Combat/CardMarks.cs', [
    ("        MarkRow row;   // the small card sprites above the head\n",
     "        MarkRow row;   // the small card sprites above the head\n"
     "        float lockedUntil;\n\n"
     "        /// <summary>Seconds after a burst in which no new card can mark this enemy (the burst animation\n"
     "        /// is still playing; a card added then looked like part of the old hand). Hits still do damage.</summary>\n"
     "        public const float BurstLock = 0.6f;\n",
     "BurstLock = 0.6f"),
    ("            enemy.TakeDamage(card.Value);              // the hit itself\n            if (enemy == null || enemy.Health <= 0f) return;\n",
     "            enemy.TakeDamage(card.Value);              // the hit itself\n            if (enemy == null || enemy.Health <= 0f) return;\n"
     "            if (Time.time < lockedUntil) return;       // burst still playing: no new marks\n",
     "Time.time < lockedUntil"),
    ("            row.BurstAll();\n",
     "            row.Sync(cards);   // the card that caused the burst gets its sprite first, so the whole hand bursts\n"
     "            row.BurstAll();\n"
     "            lockedUntil = Time.time + BurstLock;\n",
     "lockedUntil = Time.time + BurstLock;"),
])

patch('Assets/Progression/Scripts/Game/CardHand.cs', [
    ("        /// <summary>Sorts the hand by rank",
     "        /// <summary>Click-to-place: puts the leftmost selected card on the tower. The other selected\n"
     "        /// cards stay selected, so the player can click the next tower. Returns false without a selection.</summary>\n"
     "        public bool AssignFirstSelected(Tower tower)\n"
     "        {\n"
     "            if (Selected.Count == 0 || tower == null) return false;\n"
     "            int index = int.MaxValue;\n"
     "            foreach (int i in Selected) index = Math.Min(index, i);\n"
     "            if (index < 0 || index >= Cards.Count) return false;\n"
     "            var keep = new List<Card>();\n"
     "            foreach (int i in Selected) if (i != index && i < Cards.Count) keep.Add(Cards[i]);\n"
     "            var card = Cards[index];\n"
     "            Cards.RemoveAt(index);\n"
     "            if (tower.HasCard) Cards.Insert(index, tower.Card);\n"
     "            tower.SetCard(card);\n"
     "            ApplySort();\n"
     "            Selected.Clear();\n"
     "            foreach (var c in keep) { int j = Cards.IndexOf(c); if (j >= 0) Selected.Add(j); }\n"
     "            Changed?.Invoke();\n"
     "            return true;\n"
     "        }\n\n"
     "        /// <summary>Sorts the hand by rank",
     "AssignFirstSelected(Tower tower)"),
])

patch('Assets/Progression/Scripts/Game/BuildManager.cs', [
    ("                if (hovered.tower != null && pointer.press.wasPressedThisFrame) game.Hand.TakeBack(hovered.tower);\n",
     "                if (hovered.tower != null && pointer.press.wasPressedThisFrame)\n"
     "                {\n"
     "                    // selected card(s) in the hand: give the first one to this tower; else take its card back\n"
     "                    var hand = UI.HandView.Instance;\n"
     "                    if (game.Hand.Selected.Count > 0 && hand != null) hand.AssignSelected(hovered.tower);\n"
     "                    else { if (hand != null) hand.NoteReturnFrom(hovered.tower); game.Hand.TakeBack(hovered.tower); }\n"
     "                }\n",
     "hand.AssignSelected(hovered.tower)"),
    ("                game.Hand.TakeBack(t);   // its card goes back to the hand\n",
     "                if (UI.HandView.Instance != null) UI.HandView.Instance.NoteReturnFrom(t);\n"
     "                game.Hand.TakeBack(t);   // its card goes back to the hand\n",
     "NoteReturnFrom(t);"),
])

patch('Assets/Progression/Scripts/UI/HandView.cs', [
    ("        CardHand Hand => TDGame.Instance.Hand;\n",
     "        CardHand Hand => TDGame.Instance.Hand;\n\n"
     "        public static HandView Instance { get; private set; }\n\n"
     "        void Awake() => Instance = this;\n",
     "public static HandView Instance"),
    ("        static Tower TowerAt(Vector2 screen)",
     "        /// <summary>Click-to-place: the first selected card flies onto the clicked tower.</summary>\n"
     "        public void AssignSelected(Tower tower)\n"
     "        {\n"
     "            if (tower == null || Hand.Selected.Count == 0) return;\n"
     "            int index = int.MaxValue;\n"
     "            foreach (int i in Hand.Selected) index = Mathf.Min(index, i);\n"
     "            if (index >= Hand.Cards.Count) return;\n"
     "            assignTowerLocal = TowerLocal(tower);\n"
     "            assignedCard = Hand.Cards[index];\n"
     "            returnedCard = tower.HasCard ? tower.Card : (Card?)null;\n"
     "            Hand.AssignFirstSelected(tower);\n"
     "        }\n\n"
     "        /// <summary>A tower's card is about to come back to the hand: it flies from the tower, not from the deck.</summary>\n"
     "        public void NoteReturnFrom(Tower tower)\n"
     "        {\n"
     "            if (tower == null || !tower.HasCard) return;\n"
     "            assignTowerLocal = TowerLocal(tower);\n"
     "            returnedCard = tower.Card;\n"
     "        }\n\n"
     "        static Tower TowerAt(Vector2 screen)",
     "public void AssignSelected(Tower tower)"),
])
