"""Poker TD: fit the hand and the build cards into Wong's HUD zones (CardArea, TowerArea).
Run from the project root. Safe to run twice."""
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

patch('Assets/Progression/Scripts/UI/HandView.cs', [
    ("        [Header(\"Deck pile\")]",
     "        [Header(\"Zone (Wong's CardArea): the hand fills it; card size follows its height\")]\n"
     "        public RectTransform zone;\n"
     "        [Range(0.5f, 1f)] public float cardHeightInZone = 0.9f;\n\n"
     "        [Header(\"Deck pile\")]",
     "public RectTransform zone;"),
    ("            template.gameObject.SetActive(false);\n",
     "            template.gameObject.SetActive(false);\n"
     "            FitToZone();\n",
     "            FitToZone();\n"),
    ("        void OnDestroy()\n",
     "        /// <summary>Puts the hand area on the zone and sizes the cards to fit it (cards keep the 264:400 shape).</summary>\n"
     "        void FitToZone()\n"
     "        {\n"
     "            if (zone == null) return;\n"
     "            area.anchorMin = zone.anchorMin; area.anchorMax = zone.anchorMax;\n"
     "            area.pivot = zone.pivot;\n"
     "            area.anchoredPosition = zone.anchoredPosition;\n"
     "            area.sizeDelta = zone.sizeDelta;\n"
     "            float h = zone.rect.height * cardHeightInZone;\n"
     "            cardSize = new Vector2(h * 264f / 400f, h);\n"
     "            maxHandWidth = Mathf.Max(0f, zone.rect.width - cardSize.x - 24f);\n"
     "        }\n\n"
     "        void OnDestroy()\n",
     "void FitToZone()"),
])

patch('Assets/Progression/Scripts/UI/BuildCardBar.cs', [
    ("        [Header(\"Built in the scene\")]",
     "        [Header(\"Zone (Wong's TowerArea): the cards fit inside it, price tags included\")]\n"
     "        public RectTransform zone;\n\n"
     "        [Header(\"Built in the scene\")]",
     "public RectTransform zone;"),
    ("            if (area == null) Build(config);\n",
     "            if (area == null) Build(config);\n"
     "            FitToZone();\n",
     "            FitToZone();\n"),
    ("        /// <summary>Makes the fade, the area and one card per tower type. Works in Edit mode.</summary>",
     "        /// <summary>Moves the card area into the zone and sizes the cards to its height (room for the price tags).</summary>\n"
     "        public void FitToZone()\n"
     "        {\n"
     "            if (zone == null) return;\n"
     "            if (fade != null) fade.gameObject.SetActive(false);   // the zone has its own background\n"
     "            area.SetParent(zone, false);\n"
     "            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;\n"
     "            area.pivot = Vector2.zero;\n"
     "            area.offsetMin = area.offsetMax = Vector2.zero;\n"
     "            float zh = zone.rect.height, zw = zone.rect.width;\n"
     "            float h = Mathf.Max(60f, zh - 56f);                    // 56 px for the tag and margins\n"
     "            cardSize = new Vector2(h * 264f / 400f, h);\n"
     "            origin = new Vector2(cardSize.x * 0.5f + 16f, zh * 0.5f + 16f);\n"
     "            maxWidth = Mathf.Max(0f, zw - cardSize.x - 32f);\n"
     "            step = cardSize.x * 0.92f;\n"
     "            foreach (var e in towers) ResizeEntry(e);\n"
     "        }\n\n"
     "        void ResizeEntry(Entry e)\n"
     "        {\n"
     "            e.card.Rect.sizeDelta = cardSize;\n"
     "            e.tagBg.rectTransform.anchoredPosition = new Vector2(0f, -cardSize.y * 0.5f - 6f);\n"
     "            e.tagBg.rectTransform.sizeDelta = new Vector2(Mathf.Min(84f, cardSize.x * 0.85f), 30f);\n"
     "        }\n\n"
     "        /// <summary>Makes the fade, the area and one card per tower type. Works in Edit mode.</summary>",
     "public void FitToZone()"),
    ("                var e = NewEntry(\"Machine_\" + i, UiKit.MachineFace(buffIndex));\n",
     "                var e = NewEntry(\"Machine_\" + i, UiKit.MachineFace(buffIndex));\n"
     "                if (zone != null) ResizeEntry(e);\n",
     "if (zone != null) ResizeEntry(e);"),
])
