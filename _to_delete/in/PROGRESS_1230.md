# PROGRESS - Game Systems (3 solo Unity games)
Last updated: 2026-11-24 12:30 (Poker TD cards + HUD polish finished, Wong: "everything really perfect"; next: Poker TD shop/game over or board + towers, then Dam Rush)
Full history before 2026-11-24: progress/backup/PROGRESS_full_2026-11-24.md

## Goal
Three solo games in one Unity project (GameSystem2): Virtual Pet, Progression (Poker TD), Sandbox (Dam Rush). Aim: Excellent on all three, finished before Christmas 2026. Rubric checklist: 00_Course/Rubric/Rubric_Checklist.md.
Order of work (Wong, 2026-11-12): finish Poker TD -> build Sandbox -> skeletons of all 3 -> write the 3 reports -> polish all 3 -> videos last.

## Current status
- Virtual Pet (wind pet): about 80%, playable. Parked until the polish phase. Wong showed it to his teacher on 2026-11-24.
- Poker TD: core done (steps 1-5 of 6). Graphics polish: cards + HUD DONE and approved (2026-11-24). Still old style: shop panel, game over screen, hover calc panel; board/towers/enemies still graybox. Left after that: step 6 (CSV log + graph), Excel model update, content plan, balance.
- Dam Rush: Phases A, B, C coded; flood v2 + defences coded; new HUD approved; random smooth map + water B (Burst, 0.5 m) in the game. Waiting for Wong's playtest and balance.

## Next steps
1. Ask Wong what is next for Poker TD: (a) shop panel restyle (offers as cards like Balatro?), game over screen, hover calc panel; (b) board + towers with the Kenney kit (path/grass tiles, spawn gate, base, decor; Cannon -> weapon-cannon, Gatling -> weapon-turret, Trap -> weapon-catapult on stacked Kenney tower parts; UFO enemies; Kenney ammo); (c) juice (screen shake, hit flash, damage number pop, maybe a swirl background). Also: Wong places the ItemArea mat (now partly behind the bottom-right buttons) and tests the hold-and-drag camera.
2. Dam Rush playtest in MainMap: HUD (text size, tile bar height, unclear icons); water (dams/levees hold, pumps, wheels, sweep risk, farm calm-water check, muddy flood look, ice, 5x speed); one full year: food balance (winter need vs forage rate), FROZEN IN banner, hunger death, game over. Map seed is in the Console ("Dam Rush map seed"); type it into MapRandomizer.seed to replay. Water look knobs on M_DR_StylizedWater: Clarity, AbsorbRGB, Caustics, ClearLines.
3. Dam Rush flood test (plan v4 section 10): forecast on spring day 1; Alarm bell, Levee, Floodgate, Sandbags; F = short test flood. Tune sweep risk, pressure wear, gift amounts.
4. Dam Rush: mudbag (early, mud) + sandbag (upgrade).
5. Discuss enemies/weapons/army with Wong (game feels like "a garden in the clouds"); wolves were Phase D stretch.
6. Later: cap Dam Rush top speed at 2x-3x for players. Season rewards: Wong decides.
7. Later: water S (GPU) for a second WebGPU build; pack Assets/Sandbox/WaterCompare as a package (keep the scene).
8. Poker TD step 6: CSV log per run + Excel graph comparing strategies (Excellent criterion 1).
9. Update PokerTD_Model.xlsx with all rule changes; fix Run Sim (balanced strategy always wins).
10. Poker TD content plan (items, machines + rarity, hand upgrades), balance.
11. Virtual Pet: test with the real mouse; add WorkConfig numbers to WindPet_Balance.xlsx.
12. Write the 3 design reports; then polish (Virtual Pet sack shell, Animator, WebGL builds, playtest forms); videos last.

## Done (2026-11-24, Poker TD polish) - current state
- Cards: 52 faces cut by script from AssetPack card sheet 22331.jpg (264x400, anti-aliased corners, mipmaps, Trilinear) + Card_Back -> Art/Resources/Cards. Hand = Balatro feel after Mix and Jam (2024) with DOTween: follow + swing, idle 3D wobble + bob, hover tilt to the mouse (CardPerspective fake perspective), punch, shadow, deal from the deck with a flip, discard fly-out, sort shake, cards fly to/from towers. Click a selected card then a tower = place it; drag still works.
- World cards (WorldCard, MarkRow, PTD_CardDissolve shader, M_CardDissolve): card sprites on towers and above enemies; pop-in, punch, tremble before the burst, burst = hard shake + hold + dissolve; grey when stunned; burst lock 0.6 s (CardMarks.BurstLock).
- HUD (all in the scene, edited by Wong; menu "Poker TD/Build New HUD In Scene" builds the parts, "Rebuild World HUD" resets the 3D texts):
  - World UI = 3D TextMeshPro lying flat on the table (WorldHUD): "LEVEL n" + WAVE top-left, COMBOS top-right, ECONOMY/HEALTH/BUILD/HAND on the right. Camera (0.6, 32.23, -14.85), pitch 65.
  - Bottom: Wong's zones TowerArea (tower + machine cards) and CardArea (hand) on a soft black fade (UI/ZoneFade). Cards fit the zone height; tower and hand cards same height and same centred curve (HandView.cardHeightInZone, fanDrop 2.5). Deck + buttons bottom right.
  - Tower/machine/item faces in the playing-card style (white face, corner letters + icon, Kenney model render, name + TOWER/MACHINE/ITEM): progress/patches/make_ui_art_v2.py.
  - Items = 3D cards on Wong's item mat (ItemArea, UI/ItemZone: dark mat, dashed line, 4 slots): ItemTable.cs (hover lift + tilt, tooltip, click to use, burn away when used, grey when blocked).
  - Buttons: PolishButton = Wong's UIButton package behaviour (rewritten: the original imports an editor-only namespace that breaks WebGL; no click sound in the package). Fonts: Action Comics (capitals only, draws big -> autosize) + Super Chips, TMP assets + shared outline materials in Art/Resources/Fonts.
  - Camera: hold and drag the table (CameraDragPan; limit +-8 x / +-6 z). Board clicks count on release without a drag (CameraDragPan.Clicked).
  - TDHud works without the old Status / Tower0-2 (Wong keeps them in "Delete if it save").
- Patches (all safe to run twice): progress/patches/cards_world_patch.py, cards_click_patch.py, hud_layout_patch.py, scene_hud_patch.py, zones_patch.py, tdhud_cleanup_patch.py, dragpan_patch.py. Old script versions in progress/backup/.

## Key decisions (still in force)
- Poker TD look (2026-11-24, Wong): Balatro card feel but not a Balatro copy; info as 3D world UI on the table; all UI must exist in the scene so Wong can see and edit it. Assets from AssetPack (Kenney CC0, Freepik card sheet 22331, fonts incl. personal-use ones - OK, school project only). Towers: Cannon=cannon, Gatling=turret, Trap=catapult.
- Poker TD: Balatro + Kingdom Rush references; damage = chips x mult; rings around buff machines (ring = buff order); hand of 8; Flush/Straight need 5 cards; start tower limit 5; machine/item shop like Balatro jokers.
- Dam Rush concept: the player is the LEADER of a beaver family; orders on a pointy-top hex grid (radius 2 m, Patel 2013); beavers work by instinct. Survive endless seasons (4 x 10 days, day = 240 s); lose when the whole family is dead. Food is its own resource; no fish (beavers do not eat fish). Family cap 30.
- Dam Rush flood: must hurt but always have an answer (levee, sandbags, floodgate, alarm bell the leader rings; no automatic alarm). Flood lasts about a week, leaves duckweed/mud/driftwood.
- Dam Rush industry: 2 labs paid with food + research time; machines need a water wheel + a beaver; Hay Day style orders, automation later.
- Water: game uses B (Burst, WebGL 2 safe). S (GPU) is the best look -> later WebGPU bonus build. Methods: virtual pipes (Mei et al., 2007), Manning friction (de Almeida et al., 2012).
- Dam Rush HUD style: Sandcastle (Bubblebird Studio) + hex bee builder references; approved 2026-11-14.
- Virtual Pet: keep Wong's hand-made lighting (do NOT run "Wind Pet/Build Lab In Scene").
- One .asmdef per game.

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
- Poker TD: keep early burst? Two machines with the same ring number: order? Click sound for buttons (Wong to send one).
- Edit-mode NullReference in CameraRig.LateUpdate after a recompile (harmless; guard in polish).
- Real Game Systems deadline still unknown.

## Assets
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
- Poker TD UI: Scripts/UI/ CardView, HandView, CardArt, CardPerspective, JuicyCard, BuildCardBar, ItemCardBar (off), ItemTable, WorldHud, HudLayout, UiKit, PolishButton, ComboView, TDHud; Scripts/Game/CameraDragPan; Scripts/Combat/ WorldCard, MarkRow, CardMarks; Shaders/PTD_CardDissolve.shader.
- Poker TD art: Art/Resources/Cards, BuildCards, UI (+Icons), Fonts; Art/Kenney_TD (160 FBX); Art/Fonts (TTF). Kenney renders for the faces: _to_delete/icons (made in Unity).
- AssetPack (C:/Users/ADMIN/OneDrive/Desktop/AssetPack): Model/kenney_tower-defense-kit, UI/ (Kenney UI packs, card sheet, UIButton.unitypackage), Font/.
- Assets/Sandbox/ - Dam Rush (namespace DamRush). Scenes/MainMap.unity ("Dam Rush/Build Main Map" rebuilds the WHOLE scene).
- Assets/Sandbox/Scripts/Colony/ - ColonyHud.cs (HUD), FloodSystem.cs, Industry.cs, BuildOrders*.cs, ColonyManager*.cs, Beaver*.cs, Seasons.cs. UI/HudIcons.cs.
- Assets/Sandbox/Scripts/Water/WaterGrid.cs, Map/MapRandomizer.cs, Shaders/DR_StylizedWater.shader, Data/MainMapConfig.asset.
- Assets/Sandbox/WaterCompare/ + Scenes/WaterCompare.unity - water demo (keep).
- 03_Sandbox/1_Plan/Sandbox_Plan_v4.md - current Dam Rush plan.
- Assets/VirtualPet/ - Virtual Pet (namespace WindPet).
- progress/backup/ - old versions of scripts and the full PROGRESS history.
