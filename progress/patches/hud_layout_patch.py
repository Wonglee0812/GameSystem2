"""Poker TD new HUD layout: hooks in TDHud, HandView, ComboView. Run from the project root. Safe to run twice."""
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

hud = 'Assets/Progression/Scripts/UI/TDHud.cs'
patch(hud, [
    ("            gameOver.SetActive(false);\n        }",
     "            gameOver.SetActive(false);\n"
     "            // new layout: 3D boards, build cards, item cards (the old status line and buttons are hidden)\n"
     "            status.gameObject.SetActive(false);\n"
     "            gameObject.AddComponent<HudLayout>();\n"
     "        }",
     "AddComponent<HudLayout>()"),
    ("                towerButtons[i].gameObject.SetActive(deploy);",
     "                towerButtons[i].gameObject.SetActive(false);   // replaced by BuildCardBar",
     "SetActive(false);   // replaced by BuildCardBar"),
    ("                bool has = deploy && i < game.MachineStock.Count;",
     "                bool has = false;   // replaced by BuildCardBar",
     "bool has = false;   // replaced by BuildCardBar"),
    ("                b.gameObject.SetActive(board);\n                if (!board) continue;",
     "                b.gameObject.SetActive(false);   // replaced by ItemCardBar\n                continue;",
     "replaced by ItemCardBar"),
    ("            status.text = $\"Lives",
     "            if (status.gameObject.activeSelf) status.text = $\"Lives",
     "if (status.gameObject.activeSelf) status.text"),
])

hand = 'Assets/Progression/Scripts/UI/HandView.cs'
patch(hand, [
    ("            template.gameObject.SetActive(false);\n",
     "            template.gameObject.SetActive(false);\n"
     "            // bottom-right layout (2026-11-24): deck pile next to the buttons, narrower fan\n"
     "            deckPosition = new Vector2(838f, 10f);\n"
     "            maxHandWidth = 760f;\n",
     "deckPosition = new Vector2(838f, 10f);"),
    ("            t.color = on ? new Color(0.95f, 0.55f, 0.1f) : new Color(0.2f, 0.2f, 0.22f);",
     "            t.color = on ? new Color(1f, 0.86f, 0.3f) : Color.white;",
     "new Color(1f, 0.86f, 0.3f) : Color.white"),
])

combo = 'Assets/Progression/Scripts/UI/ComboView.cs'
s = open(combo, encoding='utf-8').read()
if 'WorldTarget' not in s:
    s = s.replace("        TextMeshProUGUI text, tipText;\n",
                  "        TextMeshProUGUI text, tipText;\n\n"
                  "        /// <summary>Set by WorldHud: the combos list is drawn on the 3D board instead of the screen panel.</summary>\n"
                  "        public static TMP_Text WorldTarget;\n"
                  "        TMP_Text Shown => WorldTarget != null ? WorldTarget : text;\n"
                  "        static Camera LinkCamera => WorldTarget != null ? Camera.main : null;\n", 1)
    s = s.replace("            if (panel.gameObject.activeSelf != show) panel.gameObject.SetActive(show);\n",
                  "            bool onScreen = show && WorldTarget == null;\n"
                  "            if (panel.gameObject.activeSelf != onScreen) panel.gameObject.SetActive(onScreen);\n", 1)
    s = s.replace("            text.text = Build(cards);\n"
                  "            Vector2 size = text.GetPreferredValues(text.text);\n"
                  "            panel.sizeDelta = new Vector2(size.x + 32f, size.y + 24f);\n"
                  "            text.ForceMeshUpdate();\n",
                  "            Shown.text = Build(cards);\n"
                  "            if (WorldTarget == null)\n"
                  "            {\n"
                  "                Vector2 size = text.GetPreferredValues(text.text);\n"
                  "                panel.sizeDelta = new Vector2(size.x + 32f, size.y + 24f);\n"
                  "            }\n"
                  "            Shown.ForceMeshUpdate();\n", 1)
    s = s.replace("TMP_TextUtilities.FindIntersectingLink(text, mouse.position.ReadValue(), null)",
                  "TMP_TextUtilities.FindIntersectingLink(Shown, mouse.position.ReadValue(), LinkCamera)")
    s = s.replace("TMP_TextUtilities.FindIntersectingLink(text, Mouse.current.position.ReadValue(), null)",
                  "TMP_TextUtilities.FindIntersectingLink(Shown, Mouse.current.position.ReadValue(), LinkCamera)")
    s = s.replace("text.textInfo.linkInfo", "Shown.textInfo.linkInfo")
    for need in ('WorldTarget', 'onScreen', 'Shown.text = Build', 'LinkCamera)', 'Shown.textInfo'):
        if need not in s:
            sys.exit('ComboView patch failed at: ' + need)
    open(combo, 'w', encoding='utf-8').write(s)
    print('patched', combo)
