"""Poker TD: show cards in the world as sprites (WorldCard) on towers and enemy marks.
Run from the project root. Safe to run twice: each edit checks if it is already there."""
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

marks = 'Assets/Progression/Scripts/Combat/CardMarks.cs'
patch(marks, [
    ("        TextMeshPro text;\n",
     "        TextMeshPro text;\n        MarkRow row;   // the small card sprites above the head\n",
     "MarkRow row;"),
    ("            text.fontStyle = FontStyles.Bold;\n",
     "            text.fontStyle = FontStyles.Bold;\n            text.enabled = false;   // the cards are shown as sprites now (MarkRow)\n            row = MarkRow.Create(head, Vector3.up * 0.55f);\n",
     "row = MarkRow.Create("),
    ("                if (config.sameCardResetsTimer) timer = config.WaitAfter(cards.Count);   // old rule\n",
     "                if (config.sameCardResetsTimer) timer = config.WaitAfter(cards.Count);   // old rule\n                row.Hit(card);\n",
     "row.Hit(card);"),
    ("            timerBar.localScale = new Vector3(1.14f * Mathf.Clamp01(timer / full), 0.05f, 1f);\n",
     "            timerBar.localScale = new Vector3(1.14f * Mathf.Clamp01(timer / full), 0.05f, 1f);\n            row.SetUrgency(1f - Mathf.Clamp01(timer / full));\n",
     "row.SetUrgency("),
    ("            cards.Clear();\n            Refresh();\n            enemy.TakeDamage(result.damage);",
     "            row.BurstAll();\n            cards.Clear();\n            Refresh();\n            enemy.TakeDamage(result.damage);",
     "row.BurstAll();"),
    ("            text.text = string.Join(\" \", parts);\n",
     "            text.text = string.Join(\" \", parts);\n            if (row != null) row.Sync(cards);\n",
     "row.Sync(cards);"),
    ("        static Color HandColor(HandType hand) =>",
     "        /// <summary>The enemy is gone: its card sprites burn away on their own.</summary>\n        void OnDestroy()\n        {\n            if (row != null) row.Release();\n        }\n\n        static Color HandColor(HandType hand) =>",
     "void OnDestroy()"),
])

tower = 'Assets/Progression/Scripts/Combat/Tower.cs'
patch(tower, [
    ("        TextMeshPro label;\n",
     "        TextMeshPro label;\n        WorldCard cardView;   // the tower's card as a sprite above it\n",
     "WorldCard cardView;"),
    ("            label.rectTransform.sizeDelta = new Vector2(3f, 1f);\n            UpdateLabel();",
     "            label.rectTransform.sizeDelta = new Vector2(3f, 1f);\n"
     "            cardView = WorldCard.Create(transform, lab.transform.localPosition, 0.9f, 10);\n"
     "            cardView.gameObject.SetActive(false);\n"
     "            lab.transform.localPosition += Vector3.up * 0.85f;   // \"zzz\" and \"?\" above the card\n"
     "            UpdateLabel();",
     "cardView = WorldCard.Create("),
    ("            Card = card;\n            HasCard = true;\n            UpdateLabel();",
     "            Card = card;\n            HasCard = true;\n"
     "            cardView.gameObject.SetActive(true);\n            cardView.SetCard(card);\n            cardView.PopIn();\n"
     "            UpdateLabel();",
     "cardView.PopIn();"),
    ("            HasCard = false;\n            UpdateLabel();",
     "            HasCard = false;\n"
     "            if (cardView != null && cardView.gameObject.activeSelf) cardView.DissolveOut(false);\n"
     "            UpdateLabel();",
     "cardView.DissolveOut(false);"),
    ("            label.text = Stunned ? \"<color=#FF8040>zzz</color>\" : HasCard ? Card.Rich : \"<color=#888888>?</color>\";",
     "            label.text = Stunned ? \"<color=#FF8040>zzz</color>\" : HasCard ? \"\" : \"<color=#888888>?</color>\";\n"
     "            if (cardView != null) cardView.SetGrey(Stunned);",
     "cardView.SetGrey(Stunned);"),
    ("            Projectile.Fire(this, target, material);",
     "            Projectile.Fire(this, target, material);\n            cardView.Punch(0.35f);",
     "cardView.Punch(0.35f);"),
    ("                if (any) Pulse();",
     "                if (any) { Pulse(); cardView.Punch(0.6f); }",
     "cardView.Punch(0.6f);"),
])
