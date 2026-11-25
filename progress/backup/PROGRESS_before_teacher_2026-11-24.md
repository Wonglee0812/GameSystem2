# PROGRESS - Game Systems (3 solo Unity games)
Last updated: 2026-11-24 17:10 (Poker TD visual + juice polish FINISHED, approved by Wong)
Full history: progress/backup/PROGRESS_full_2026-11-24.md (before polish) and backup/PROGRESS_full_2026-11-24_polish.md (polish day, all details)

## Goal
Three solo games in one Unity project (GameSystem2): Virtual Pet, Progression (Poker TD), Sandbox (Dam Rush). Aim: Excellent on all three, finished before Christmas 2026. Rubric checklist: 00_Course/Rubric/Rubric_Checklist.md.
Order of work (Wong, 2026-11-12): finish Poker TD -> build Sandbox -> skeletons of all 3 -> write the 3 reports -> polish all 3 -> videos last.

## Current status
- Virtual Pet (wind pet): about 80%, playable. Parked until the polish phase. Wong showed it to his teacher on 2026-11-24.
- Poker TD: gameplay steps 1-5 done; look and feel DONE (2026-11-24, Wong: "dự án này xong"): Kenney 3D board, juice, 3D shop, transition, Game Over. Left: step 6 (CSV log + graph), Excel model update, content plan, balance.
- Dam Rush: Phases A, B, C coded; flood v2 + defences coded; new HUD approved; random smooth map + water B (Burst, 0.5 m). Waiting for Wong's playtest and balance.

## Next steps
1. Ask Wong what is next: Poker TD step 6 (CSV log per run + Excel graph comparing strategies, Excellent criterion 1) or the Dam Rush playtest.
2. Dam Rush playtest in MainMap: HUD (text size, tile bar height, unclear icons); water (dams/levees hold, pumps, wheels, sweep risk, farm calm-water check, muddy flood look, ice, 5x speed); one full year: food balance, FROZEN IN banner, hunger death, game over. Map seed in the Console ("Dam Rush map seed") -> MapRandomizer.seed to replay. Water knobs on M_DR_StylizedWater: Clarity, AbsorbRGB, Caustics, ClearLines.
3. Dam Rush flood test (plan v4 section 10): forecast on spring day 1; Alarm bell, Levee, Floodgate, Sandbags; F = short test flood. Tune sweep risk, pressure wear, gift amounts.
4. Dam Rush: mudbag (early, mud) + sandbag (upgrade).
5. Discuss enemies/weapons/army for Dam Rush with Wong ("a garden in the clouds"); wolves were Phase D stretch.
6. Later: cap Dam Rush top speed at 2x-3x. Season rewards: Wong decides.
7. Later: water S (GPU) for a WebGPU build; pack Assets/Sandbox/WaterCompare as a package (keep the scene).
8. Update PokerTD_Model.xlsx with all rule changes (incl. 3D shop: 3 offers per zone); fix Run Sim (balanced strategy always wins).
9. Poker TD content plan (items, machines + rarity, hand upgrades), balance.
10. Virtual Pet: test with the real mouse; add WorkConfig numbers to WindPet_Balance.xlsx.
11. Write the 3 design reports (Poker TD juice section: sources Jonasson & Purho 2012, Nijman 2013, Kelly 2014, Hicks et al. 2018, Eiserloh 2016; juice ON/OFF test with Juice.juiceOn); then polish (Virtual Pet sack shell, Animator, WebGL builds, playtest forms); videos last.

## Done - Poker TD look and feel (2026-11-24, summary; details in backup/PROGRESS_full_2026-11-24_polish.md)
- Cards + HUD: Balatro-feel hand (DOTween), world cards with dissolve (PTD_CardDissolve), world HUD flat on the table, build/item cards, item mat (smaller), hearts for lives (UI/HeartBar.cs, 1 heart = 2 lives), gold number only, COMBOS in 2 columns, one shared tooltip (UiKit.ShowTip) for everything, Discard without count, early-burst button removed.
- Kenney 3D (menu "Poker TD/Apply Kenney Art"): tiles + path pieces + decor + grid lines (GridOverlay), spawn hole, castle, Cannon/Gatling towers, spike trap (spikes in code, pop on pulse), machines = base + crystals, UFO enemies, ammo. Runtime art in Art/Resources/PTDArt.asset. Camera: drag + wheel zoom, glides back after a wave.
- Juice (Scripts/Juice/Juice.cs, one object with a switch per effect + master switch; menu "Poker TD/Build Juice In Scene"): trauma shake, hit stop, Balatro burst score, confetti, hit numbers, hit flash, recoil, muzzle flash, trails, cannon arc + explosion, death dissolve + coins, castle hit + red vignette, banners, ink background (port of Wong's SplatoonGraph, M_PTD_Ink).
- Dissolve = signature effect (PTD_ModelDissolve + ModelDissolve): towers wait as a card on the tile until the first poker card, then burn in; machines burn in; dead UFOs burn away.
- One selection at a time (tower/machine card vs poker cards). Tower cards sit above and behind the model (Tower.cardBack).
- Machine rings: shown on the tiles (colour + number), almost invisible with a radar wave from the machine (BuildManager ringIdle / ringWave*). Right-click a placed machine = back to the hand (move it); sell only from the hand card.
- Transition (BoardTransition): tiles fall from the centre out and hide, new floor rises centre-first (hidden until it moves), shop zones land last, grid erased centre-out / drawn outside-in. Knobs explained in the Inspector tooltips.
- 3D shop (Shop3D + menu "Poker TD/Build 3D Shop", safe: only adds missing parts, keeps Wong's layout): MACHINES pedestals, ITEMS and UPGRADES cards on mats, 3D REROLL/CONTINUE; hover lift + tooltip, click buy with dissolve. 3 offers per zone (TDConfig.shopMachineOffers/ItemOffers/UpgradeOffers).
- Game Over: dark overlay, GAME OVER drop-in, level + wave reached, RESTART button + R.
- Patch scripts (safe to run twice): progress/patches/kenney_art_patch.py, juice_patch.py, shop3d_patch.py, make_shop_art.py, make_ui_art_v2.py (+ older ones). Old script copies: progress/backup/*_2026-11-24/.

## Key decisions (still in force)
- Poker TD look (2026-11-24, Wong): Balatro card feel but not a Balatro copy; info as 3D world UI on the table; all UI must exist in the scene so Wong can see and edit it. Assets from AssetPack (Kenney CC0, Freepik card sheet 22331, fonts incl. personal-use ones - OK, school project only). Towers: Cannon=round tower + cannon, Gatling=square tower + turret, Trap=spike plate (the catapult looked like a cannon).
- Poker TD: keep the early burst, no toggle button (Wong, 2026-11-24).
- Buff machines can be moved: right-click returns them to the hand; only the hand card can be sold (Wong, 2026-11-24).
- Shop = 3D zones like Unrailed, not a Balatro row; no UI shop panel (Wong, 2026-11-24).
- Tower placement teaches the rule: a tower is only a card on the tile until it gets a poker card (Wong, 2026-11-24).
- Dissolve is the signature effect of Poker TD: use it for things appearing/disappearing (Wong, 2026-11-24).
- Poker TD: Balatro + Kingdom Rush references; damage = chips x mult; rings around buff machines (ring = buff order); hand of 8; Flush/Straight need 5 cards; start tower limit 5; machine/item shop like Balatro jokers.
- Dam Rush concept: the player is the LEADER of a beaver family; orders on a pointy-top hex grid (radius 2 m, Patel 2013); beavers work by instinct. Survive endless seasons (4 x 10 days, day = 240 s); lose when the whole family is dead. Food is its own resource; no fish (beavers do not eat fish). Family cap 30.
- Dam Rush flood: must hurt but always have an answer (levee, sandbags, floodgate, alarm bell the leader rings; no automatic alarm). Flood lasts about a week, leaves duckweed/mud/driftwood.
- Dam Rush industry: 2 labs paid with food + research time; machines need a water wheel + a beaver; Hay Day style orders, automation later.
- Water: game uses B (Burst, WebGL 2 safe). S (GPU) is the best look -> later WebGPU bonus build. Methods: virtual pipes (Mei et al., 2007), Manning friction (de Almeida et al., 2012).
- Dam Rush HUD style: Sandcastle (Bubblebird Studio) + hex bee builder references; approved 2026-11-14.
- Virtual Pet: keep Wong's hand-made lighting (do NOT run "Wind Pet/Build Lab In Scene").
- One .asmdef per game.
- Code rule (Wong, 2026-11-24): every public / Inspector field gets a [Tooltip] that says what it does, the unit and the default value, so Wong can tune it without reading the code.

## Didn't work (do not repeat)
- Camera edge panning in Poker TD (Wong: not good) -> hold and drag instead.
- Wooden 3D signboards for the info (Wong replaced them with flat text on the table).
- UI built only at runtime (Wong could not see/edit it) -> build into the scene.
- Card PNG filter on Point ("None") = jagged cards; keep Trilinear + mipmaps.
- Copies of a UI template that builds child layers in Awake get the layers twice (white fringe): remove copied layers.
- X/Y rotation on a Screen Space Overlay canvas only squashes (no perspective): use CardPerspective.
- device_commit_files often writes an OLD copy (it caches by staged file name): use a new staged file name each time and check md5; else edit on the device with python.
- Compiling while Wong is in Play mode wipes the session: check EditorApplication.isPlaying and ask Wong first. Save Wong's unsaved scene changes before compiling.
- Deleting files from the device shell: no permission; use _to_delete/.
- Patch scripts must be safe to run twice.
- Toggling Burst options in Play mode makes Burst recompile (10x slower for a while).
- Water: too many fixed steps per slow frame = spiral (keep 25 steps/s, 0.2 s fast step).
- Dam Rush as a builder/empire game with predators and crafting: off-concept. One-hex dams: water flows around.
- Poker TD: % health buffs, x1.5 per hand level, early burst for High Card - rejected.

## Open questions
- Flood: 6/12 swept away in one wave too much for year 1? Sandbags without sand (year-1 flood comes before the sifter)?
- 5x speed ~24 fps (was ~33): do the water streaks cost GPU time?
- Food storage holds 40 (60 after B5) but B3/B4 cost 60-70 food: OK or pay bit by bit?
- Pump on high dry ground sends water downhill: keep or only pump into hexes that touch water?
- Poker TD: two machines with the same ring number: which goes first (now: the one built first)? Click sound for buttons: later (Wong).
- Edit-mode NullReference in CameraRig.LateUpdate after a recompile (harmless; guard in polish).
- Real Game Systems deadline still unknown (Wong will tell later).

## Assets
- assets/radar1.png, trans_hide3.png - (no text: view the original) radar ring wave; transition with the old floor hidden
- assets/ring_tiles7.png - (no text: view the original) machine rings on the tiles
- assets/s_trans1.png, s_back1.png, s_shop_buy.png, s_shop_reroll.png, s_gameover4.png - (no text: view the original) transition, 3D shop, Game Over
- assets/r5_close2.png - (no text: view the original) tower cards above/behind the towers
- assets/r4_pending.png, r4_arm.png - (no text: view the original) tower card waiting on the tile, then burning in
- assets/r3_bg.png, r3_deploy.png, r3_wave.png - (no text: view the original) juice round 3
- assets/r2_overview.png, r2_dissolve_test2.png, r2_build.png - (no text: view the original) juice round 2
- assets/juice_banner.png, juice_burst6.png, juice_particles2.png, juice_basehit.png - (no text: view the original) juice tests
- assets/ptd_trap_options.png, ptd_grid_zoom.png, ptd_spike_final.png, ptd_spikes1.png - (no text: view the original) trap choices A-D; grid + zoom; final spike trap
- assets/ptd_kenney_play2.png, ptd_kenney_wave1.png, ptd_kenney_shop2.png, ptd_kenney_back.png - (no text: view the original) Poker TD with Kenney 3D models
- assets/ptd_fan_flat.png, ptd_zones_items.png, ptd_aligned.png, ptd_scene_play.png - (no text: view the original) Poker TD current HUD (zones, items mat, aligned cards)
- assets/cards_sliced_preview.png, ptd_cards_v2b.png, ptd_worldcards_towers.png, ptd_worldcards_wave2.png, ptd_ui_*.png - (no text: view the original) Poker TD polish steps
- assets/PokerTD_Model.xlsx -> md/PokerTD_Model.xlsx.md - Poker TD economy and damage model.
- assets/PokerTD_Reference.xlsx -> md/PokerTD_Reference.xlsx.md - Balatro + Kingdom Rush analysis, APA.
- assets/WindPet_Balance.xlsx -> md/WindPet_Balance.xlsx.md - Virtual Pet bars.
- assets/Research_Beaver_AI_and_Water.md -> md/Research_Beaver_AI_and_Water.md.md - beaver behaviour + water methods, APA.
- assets/Sandbox_Plan_v2.md, v3.md, v4.md - Dam Rush plans (v4 current: floods + defences in section 10).
- assets/*.png (others) - (no text: view the original) Dam Rush screenshots: ui_v2, map_random, B_main, wc*, phaseC/fix, Sack_Preview_v3.

## Other key files
- Assets/Progression/ - Poker TD (namespace PokerTD); Data/TDConfig.asset; Editor/PokerTDBuilder.cs (rebuilds the WHOLE scene - then run "Build New HUD In Scene" again), Editor/PokerTDHudBuilder.cs; Scenes/PokerTD.unity.
- Poker TD UI: Scripts/UI/ CardView, HandView, CardArt, CardPerspective, JuicyCard, BuildCardBar, ItemTable, WorldHud, HeartBar, HudLayout, UiKit, PolishButton, ComboView, TDHud, Shop3D (ShopView off); Scripts/Game/ CameraDragPan, BoardTransition, GridOverlay, PTDArt, BuildManager, BuildNode; Scripts/Combat/ WorldCard, MarkRow, CardMarks; Scripts/Juice/ Juice, JuiceParts.
- Poker TD shaders: PTD_CardDissolve, PTD_ModelDissolve, PTD_InkBackground (Swirl unused). Editor menus: Apply Kenney Art, Build Juice In Scene, Build 3D Shop (all safe to run twice; Kenney Art rebuilds the board art).
- Poker TD art: Art/Resources/Cards, BuildCards, UI (+Icons), Fonts; Art/Kenney_TD (160 FBX); Art/Fonts (TTF). Kenney renders for the faces: _to_delete/icons (made in Unity).
- AssetPack (C:/Users/ADMIN/OneDrive/Desktop/AssetPack): Model/kenney_tower-defense-kit, UI/ (Kenney UI packs, card sheet, UIButton.unitypackage), Font/.
- Assets/Sandbox/ - Dam Rush (namespace DamRush). Scenes/MainMap.unity ("Dam Rush/Build Main Map" rebuilds the WHOLE scene).
- Assets/Sandbox/Scripts/Colony/ - ColonyHud.cs (HUD), FloodSystem.cs, Industry.cs, BuildOrders*.cs, ColonyManager*.cs, Beaver*.cs, Seasons.cs. UI/HudIcons.cs.
- Assets/Sandbox/Scripts/Water/WaterGrid.cs, Map/MapRandomizer.cs, Shaders/DR_StylizedWater.shader, Data/MainMapConfig.asset.
- Assets/Sandbox/WaterCompare/ + Scenes/WaterCompare.unity - water demo (keep).
- 03_Sandbox/1_Plan/Sandbox_Plan_v4.md - current Dam Rush plan.
- Assets/VirtualPet/ - Virtual Pet (namespace WindPet).
- progress/backup/ - old versions of scripts and the full PROGRESS history.
