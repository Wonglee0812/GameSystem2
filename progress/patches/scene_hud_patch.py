"""Poker TD: let HandView and TDHud use HUD objects that were built into the scene
(menu Poker TD/Build New HUD In Scene) instead of making them at runtime.
Run from the project root. Safe to run twice."""
import sys

def patch(path, edits):
    s = open(path, encoding='utf-8').read()
    for old, new, marker in edits:
        if marker in s:
            continue
        if marker == '__never__' and old not in s:
            continue   # a removal that was already done
        if old not in s:
            sys.exit(f'NOT FOUND in {path}: {old[:70]!r}')
        s = s.replace(old, new, 1)
    open(path, 'w', encoding='utf-8').write(s)
    print('patched', path)

hand = 'Assets/Progression/Scripts/UI/HandView.cs'
patch(hand, [
    ("        public float maxHandWidth = 980f;", "        public float maxHandWidth = 760f;", "maxHandWidth = 760f;"),
    ("        public Vector2 deckPosition = new Vector2(-640f, -10f);", "        public Vector2 deckPosition = new Vector2(838f, 10f);", "deckPosition = new Vector2(838f, 10f);"),
    ("            // bottom-right layout (2026-11-24): deck pile next to the buttons, narrower fan\n"
     "            deckPosition = new Vector2(838f, 10f);\n"
     "            maxHandWidth = 760f;\n", "", "__never__"),
    ("            // the \"Burst at best hand\" toggle sits right above Start Wave, so Discard goes one row higher\n"
     "            var drt0 = (RectTransform)discardButton.transform;\n"
     "            drt0.anchoredPosition = new Vector2(drt0.anchoredPosition.x, 214f);\n", "", "__never__"),
    ("        Button SortButton(string label, float x, UnityEngine.Events.UnityAction action)\n        {\n",
     "        Button SortButton(string label, float x, UnityEngine.Events.UnityAction action)\n        {\n"
     "            // built into the scene (Poker TD/Build New HUD In Scene): only wire it up\n"
     "            var found = discardButton.transform.parent.Find(label);\n"
     "            if (found != null)\n"
     "            {\n"
     "                var fb = found.GetComponent<Button>();\n"
     "                fb.onClick.RemoveAllListeners();\n"
     "                fb.onClick.AddListener(action);\n"
     "                return fb;\n"
     "            }\n",
     "only wire it up"),
    ("        void BuildDeckPile()\n        {\n",
     "        public void BuildDeckPile()\n        {\n"
     "            var existing = area.Find(\"DeckPile\") as RectTransform;\n"
     "            if (existing != null)\n"
     "            {\n"
     "                deckPile = existing;\n"
     "                deckCount = existing.GetComponentInChildren<TMP_Text>(true);\n"
     "                return;\n"
     "            }\n",
     "public void BuildDeckPile()"),
])

# the two lines removed above have no marker; check they are gone
s = open(hand, encoding='utf-8').read()
for gone in ('deckPosition = new Vector2(838f, 10f);\n            maxHandWidth = 760f;', 'drt0.anchoredPosition'):
    if gone in s:
        sys.exit('still in HandView: ' + gone)

hud = 'Assets/Progression/Scripts/UI/TDHud.cs'
patch(hud, [
    ("            gameObject.AddComponent<HudLayout>();\n",
     "            if (GetComponent<HudLayout>() == null) gameObject.AddComponent<HudLayout>();\n",
     "GetComponent<HudLayout>() == null"),
    ("            earlyBurst = Instantiate(startWave, startWave.transform.parent);\n",
     "            var existing = startWave.transform.parent.Find(\"EarlyBurstToggle\");   // built into the scene?\n"
     "            earlyBurst = existing != null ? existing.GetComponent<Button>() : Instantiate(startWave, startWave.transform.parent);\n",
     "built into the scene?"),
    ("            earlyBurst.interactable = true;\n            var rt = (RectTransform)earlyBurst.transform;\n",
     "            earlyBurst.interactable = true;\n            if (existing != null) return;   // keep the position and style from the scene\n"
     "            var rt = (RectTransform)earlyBurst.transform;\n",
     "keep the position and style from the scene"),
])
