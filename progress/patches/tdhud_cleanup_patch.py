"""Poker TD: TDHud no longer needs the old Status line and Tower0-2 buttons (the new HUD replaced them),
so Wong can delete them. Old machine and item buttons are not made any more.
Run from the project root. Safe to run twice."""
import sys

def patch(path, edits):
    s = open(path, encoding='utf-8').read()
    for old, new, marker in edits:
        if marker in s:
            continue
        if marker == '__gone__' and old not in s:
            continue   # a removal that was already done
        if old not in s:
            sys.exit(f'NOT FOUND in {path}: {old[:70]!r}')
        s = s.replace(old, new, 1)
    open(path, 'w', encoding='utf-8').write(s)
    print('patched', path)

patch('Assets/Progression/Scripts/UI/TDHud.cs', [
    ("            for (int i = 0; i < towerButtons.Length; i++)\n            {\n                int index = i;\n",
     "            // old tower buttons: optional (the new HUD uses BuildCardBar; Wong may delete them)\n"
     "            for (int i = 0; towerButtons != null && i < towerButtons.Length; i++)\n            {\n"
     "                if (towerButtons[i] == null) continue;\n                int index = i;\n",
     "old tower buttons: optional"),
    ("            var first = (RectTransform)towerButtons[0].transform;\n            for (int i = 0; i < game.MachineLimit; i++) AddMachineButton(i);\n",
     "            // (old machine buttons are not made any more: BuildCardBar shows bought machines)\n",
     "old machine buttons are not made any more"),
    ("            for (int i = 0; i < game.config.itemSlots; i++) AddItemButton(i);\n",
     "            // (old item buttons are not made any more: ItemTable shows the items on the table)\n",
     "old item buttons are not made any more"),
    ("            status.gameObject.SetActive(false);\n",
     "            if (status != null) status.gameObject.SetActive(false);\n",
     "if (status != null) status.gameObject.SetActive(false);"),
    ("            if (status.gameObject.activeSelf) status.text",
     "            if (status != null && status.gameObject.activeSelf) status.text",
     "status != null && status.gameObject.activeSelf"),
    ("            for (int i = 0; i < towerButtons.Length; i++)\n            {\n                towerButtons[i].gameObject.SetActive(false);   // replaced by BuildCardBar\n",
     "            for (int i = 0; towerButtons != null && i < towerButtons.Length; i++)\n            {\n"
     "                if (towerButtons[i] == null) continue;\n"
     "                towerButtons[i].gameObject.SetActive(false);   // replaced by BuildCardBar\n",
     "if (towerButtons[i] == null) continue;\n                towerButtons[i].gameObject.SetActive(false);"),
    ("            while (machineButtons.Count < Mathf.Min(game.MachineStock.Count, 9 - towers)) AddMachineButton(machineButtons.Count);\n",
     "",
     "__gone__"),
    ("            UpdateItems();\n", "", "__gone__"),
])
