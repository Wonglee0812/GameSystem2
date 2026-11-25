"""Poker TD: board clicks on release (CameraDragPan.Clicked) so a camera drag never builds;
flatter hand fan centred on the tower-card line. Run from the project root. Safe to run twice."""
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

bm = 'Assets/Progression/Scripts/Game/BuildManager.cs'
s = open(bm, encoding='utf-8').read()
if 'CameraDragPan.Clicked' not in s:
    n = s.count('pointer.press.wasPressedThisFrame')
    if n != 3:
        sys.exit(f'BuildManager: expected 3 presses, found {n}')
    s = s.replace('pointer.press.wasPressedThisFrame', 'CameraDragPan.Clicked')
    s = s.replace('        void Update()\n', '        // left clicks on the board count on release without a drag (CameraDragPan), so dragging the camera never builds\n        void Update()\n', 1)
    open(bm, 'w', encoding='utf-8').write(s)
    print('patched', bm)

patch('Assets/Progression/Scripts/UI/ItemTable.cs', [
    ("            if (pointer.press.wasPressedThisFrame)\n",
     "            if (CameraDragPan.Clicked)   // on release, not at the start of a camera drag\n",
     "if (CameraDragPan.Clicked)"),
])

patch('Assets/Progression/Scripts/UI/HandView.cs', [
    ("        public float fanDrop = 4f;", "        public float fanDrop = 2.5f;", "fanDrop = 2.5f;"),
    ("                v.restPosition = new Vector2(mid * step, -mid * mid * fanDrop + (selected ? selectedLift : 0f));",
     "                // the curve is centred on the zone line: middle cards a bit up, outer cards a bit down\n"
     "                float edge = (n - 1) / 2f;\n"
     "                v.restPosition = new Vector2(mid * step, -mid * mid * fanDrop + edge * edge * fanDrop * 0.5f + (selected ? selectedLift : 0f));",
     "edge * edge * fanDrop * 0.5f"),
])

patch('Assets/Progression/Scripts/UI/BuildCardBar.cs', [
    ("                card.restPosition = origin + new Vector2(i * s, -mid * mid * 3f);",
     "                float edge = (n - 1) / 2f;   // same centred curve as the hand\n"
     "                card.restPosition = origin + new Vector2(i * s, -mid * mid * 2.5f + edge * edge * 2.5f * 0.5f);",
     "edge * edge * 2.5f * 0.5f"),
])
