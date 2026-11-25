"""Poker TD 3D shop (2026-11-24, Wong): offers rolled per zone (machines, items, upgrades) like the
stations in Unrailed instead of one mixed Balatro row. Run from the project root. Safe to run twice."""
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

S = 'Assets/Progression/Scripts/'
patch(S + 'Core/TDConfig.cs', [
    ("        public int shopOffers = 4;\n",
     "        public int shopOffers = 4;\n        [Tooltip(\"3D shop (Wong, 2026-11-24): offers per zone. Upgrades = hand levels, slots and unlocks (by the weights below).\")]\n        public int shopMachineOffers = 3, shopItemOffers = 3, shopUpgradeOffers = 3;\n",
     "shopMachineOffers"),
])
patch(S + 'Game/TDGame.cs', [
    ("""            for (int i = 0; i < config.shopOffers; i++)
            {
                float unlockW = locked.Count > 0 && !Offers.Exists(o => o.kind == OfferKind.Unlock) ? config.unlockWeight : 0f;
                float[] w = { config.machineWeight, config.handLevelWeight, config.itemWeight, config.towerSlotWeight, config.machineSlotWeight, unlockW };""",
     """            // one zone each in the 3D shop: machines, items, then upgrades (hand levels, slots, unlocks by weight)
            for (int i = 0; i < config.shopMachineOffers; i++)
                Offers.Add(new ShopOffer { kind = OfferKind.Machine, machine = config.buffs[UnityEngine.Random.Range(0, config.buffs.Length)] });
            for (int i = 0; i < config.shopItemOffers; i++)
                Offers.Add(new ShopOffer { kind = OfferKind.Item, item = config.items[UnityEngine.Random.Range(0, config.items.Length)] });
            for (int i = 0; i < config.shopUpgradeOffers; i++)
            {
                float unlockW = locked.Count > 0 && !Offers.Exists(o => o.kind == OfferKind.Unlock) ? config.unlockWeight : 0f;
                float[] w = { 0f, open.Count > 0 ? config.handLevelWeight : 0f, 0f, config.towerSlotWeight, config.machineSlotWeight, unlockW };""",
     "one zone each in the 3D shop"),
])
print('done')
patch(S + 'Combat/WorldCard.cs', [
    ("        /// <summary>Burns away without the shake",
     "        /// <summary>Burns in from nothing (the reverse of DissolveOut).</summary>\n        public void DissolveIn(float time = 0.45f, float delay = 0f)\n        {\n            Reset();\n            dissolve = 1f;\n            Apply();\n            DOTween.To(() => dissolve, v => { dissolve = v; Apply(); }, 0f, time).SetDelay(delay).SetEase(Ease.OutQuad).SetTarget(this).SetLink(gameObject);\n        }\n\n        /// <summary>Burns away without the shake",
     "public void DissolveIn"),
])
patch(S + 'UI/ShopView.cs', [
    ("            var game = TDGame.Instance;\n            bool show = game.Current == TDGame.Phase.Shop && !BoardTransition.Instance.Busy;\n",
     "            var game = TDGame.Instance;\n            if (Shop3D.Instance != null) { if (panel.activeSelf) panel.SetActive(false); return; }   // the 3D shop replaces this panel\n            bool show = game.Current == TDGame.Phase.Shop && !BoardTransition.Instance.Busy;\n",
     "the 3D shop replaces this panel"),
])

# ---------- Game Over, basic plan (Wong): dark screen, GAME OVER, level reached, RESTART button ----------
patch(S + 'UI/TDHud.cs', [
    ("""            if (game.Current == TDGame.Phase.GameOver && !gameOver.activeSelf)
            {
                gameOver.SetActive(true);
                gameOverText.text = $"GAME OVER\\n<size=50%>Reached ante {game.Ante}, wave {game.WaveInAnte}\\nPress R to restart</size>";
            }
            if (gameOver.activeSelf && kb != null && kb.rKey.wasPressedThisFrame)
            {
                Enemy.All.Clear(); Tower.All.Clear(); BuffMachine.ResetAll();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().name : "PokerTD");
            }""",
     """            if (game.Current == TDGame.Phase.GameOver && !gameOver.activeSelf) ShowGameOver(game);
            if (gameOver.activeSelf && kb != null && kb.rKey.wasPressedThisFrame) Restart();""",
     "ShowGameOver(game);"),
    ("        /// <summary>Hovering a tower: its burst",
     """        /// <summary>Game over (basic plan, Wong 2026-11-24): the dark screen fades in, GAME OVER drops in,
        /// the level and wave reached, and a RESTART button (R works too).</summary>
        void ShowGameOver(TDGame game)
        {
            gameOver.SetActive(true);
            gameOver.transform.SetAsLastSibling();
            var info = gameOver.transform.Find("Info")?.GetComponent<TMP_Text>();
            if (info != null) { gameOverText.text = "GAME OVER"; info.text = $"YOU REACHED LEVEL {game.Ante}  ·  WAVE {game.WaveInAnte}\\n<size=70%><color=#BBA88F>Press R or click Restart</color></size>"; }
            else gameOverText.text = $"GAME OVER\\n<size=50%>Reached level {game.Ante}, wave {game.WaveInAnte}\\nPress R to restart</size>";
            var restart = gameOver.transform.Find("Restart")?.GetComponent<Button>();
            if (restart != null) { restart.onClick.RemoveAllListeners(); restart.onClick.AddListener(Restart); }
            var group = gameOver.GetComponent<CanvasGroup>();
            if (group == null) group = gameOver.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            DOTween.To(() => group.alpha, a => group.alpha = a, 1f, 0.6f).SetUpdate(true).SetLink(gameOver);
            var title = gameOverText.transform;
            title.localScale = Vector3.one * 2.2f;
            title.DOScale(1f, 0.45f).SetDelay(0.15f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameOver);
            Juice.Shake(0.5f);
        }

        void Restart()
        {
            Enemy.All.Clear(); Tower.All.Clear(); BuffMachine.ResetAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().name : "PokerTD");
        }

        /// <summary>Hovering a tower: its burst""",
     "void ShowGameOver(TDGame game)"),
])
s = open(S + 'UI/TDHud.cs', encoding='utf-8').read()
if 'using DG.Tweening;' not in s:
    open(S + 'UI/TDHud.cs', 'w', encoding='utf-8').write('using DG.Tweening;\n' + s)
