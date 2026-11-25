# PROGRESS - Game Systems (3 solo Unity games)
Last updated: 2026-11-24 11:45 (Poker TD new HUD is now built into the scene; next: Wong tests/edits it, then shop restyle)
Full history before 2026-11-24: progress/backup/PROGRESS_full_2026-11-24.md

## Goal
Three solo games in one Unity project (GameSystem2): Virtual Pet, Progression (Poker TD), Sandbox (Dam Rush). Aim: Excellent on all three, finished before Christmas 2026. Rubric checklist: 00_Course/Rubric/Rubric_Checklist.md.
Order of work (Wong, 2026-11-12): finish Poker TD -> build Sandbox -> skeletons of all 3 -> write the 3 reports -> polish all 3 -> videos last.

## Current status
- Virtual Pet (wind pet): about 80%, playable. Parked until the polish phase.
- Poker TD: core done (steps 1-5 of 6). Graphics polish in progress: cards DONE; HUD layout v1 DONE (3D boards, build/item cards, buttons), waiting for Wong's test. Left: board + Kenney towers, UI, juice; step 6 (CSV log + strategy graph), Excel model update, content plan, balance.
- Dam Rush: Phases A, B, C coded; flood v2 + defences coded; new HUD approved; random smooth map + water B (Burst, 0.5 m) in the game. Waiting for Wong's playtest and balance.

## Plan for today (2026-11-24, Wong)
1. Poker TD graphics polish (no time limit, "until it looks good"). Polish plan: (1) cards + Balatro feel - DONE; (2) board + towers with the Kenney kit; (3) UI (fonts, Kenney borders/icons, chips x mult count-up); (4) juice (screen shake, hit flash, swirl background). Wong asked for UI (3) next, before (2).
2. Then Dam Rush: playtest + balance.

## Next steps
1. Wong tests the new Poker TD HUD (boards readable? build/item cards, buttons, combos on the board, click a combo hand for an item). Then restyle the shop panel (offers as cards?), game over screen and the hover calc panel - ask Wong first. Layout of the HUD may change later ("plan later", Wong).
1b. Poker TD board + towers: Kenney path/grass tiles, spawn gate, base, decor; towers = Cannon -> weapon-cannon, Gatling -> weapon-turret, Trap -> weapon-catapult on stacked Kenney tower parts; UFO enemies; Kenney ammo.
1c. Poker TD juice: screen shake, hit flash, damage number pop, maybe Balatro swirl background shader.
2. Dam Rush playtest in MainMap: HUD (text size, tile bar height, unclear icons); water (dams/levees hold, pumps, wheels, sweep risk, farm calm-water check, muddy flood look, ice, 5x speed); one full year: food balance (winter need vs forage rate), FROZEN IN banner, hunger death, game over. Map seed is in the Console ("Dam Rush map seed"); type it into MapRandomizer.seed to replay. Water look knobs on M_DR_StylizedWater: Clarity, AbsorbRGB, Caustics, ClearLines.
3. Dam Rush flood test (plan v4 section 10): forecast on spring day 1; Alarm bell, Levee, Floodgate, Sandbags; F = short test flood. Tune sweep risk, pressure wear, gift amounts.
4. Dam Rush: mudbag (early, mud) + sandbag (upgrade).
5. Discuss enemies/weapons/army with Wong (game feels like "a garden in the clouds"); wolves were Phase D stretch.
6. Later: cap top speed at 2x-3x for players. Season rewards: Wong decides.
7. Later: water S (GPU) for a second WebGPU build; pack Assets/Sandbox/WaterCompare as a package (keep the scene).
8. Poker TD step 6: CSV log per run + Excel graph comparing strategies (Excellent criterion 1).
9. Update PokerTD_Model.xlsx with all rule changes; fix Run Sim (balanced strategy always wins).
10. Poker TD content plan (items, machines + rarity, hand upgrades), balance.
11. Virtual Pet: test with the real mouse; add WorkConfig numbers to WindPet_Balance.xlsx.
12. Write the 3 design reports; then polish (Virtual Pet sack shell, Animator, WebGL builds, playtest forms); videos last.

## Done (2026-11-24, Poker TD cards)
- 52 card faces cut from AssetPack/UI/full-deck-poker-playing-cards/22331.jpg by script (anti-aliased rounded corners, 264x400, mipmaps, Trilinear) + Card_Back.png -> Art/Resources/Cards. Kenney TD kit -> Art/Kenney_TD (160 FBX); fonts Super Chips + Action Comics -> Art/Fonts. DOTween installed by Wong.
- Hand (CardView, HandView, CardArt, CardPerspective): Balatro feel after Mix and Jam (2024): smooth follow + swing, idle 3D wobble + bob, hover tilt to the mouse (fake perspective mesh), punch on hover/select, shadow, deal from a deck pile with flip, discard fly-out, sort swap shake, card flies to/from towers. Click a selected card then a tower = place it (others stay selected); drag still works.
- World cards (WorldCard, MarkRow, PTD_CardDissolve.shader, M_CardDissolve.mat): card sprites on towers and above enemies; pop-in, punch on hit/shot, tremble as the burst timer runs out, burst = hard shake + hold + dissolve; grey when stunned. Burst lock 0.6 s (CardMarks.BurstLock) so a card added during a burst cannot join the old hand.
- UI fixes: sort labels readable (grey/orange), Discard moved above the Burst toggle.
- Wong tested: "every thing good now".

## Done (2026-11-24, Poker TD HUD layout v1)
- Wong's plan: game info as 3D world UI; not a Balatro copy. Top wooden board behind the map: ANTE (title) > WAVE > hint line. Right wooden board: ECONOMY (gold), HEALTH (lives), BUILD (towers, machines + stock), HAND (discards, deck), COMBOS (ComboView text, links still clickable). Camera moved to (3.6, 35, -27), pitch 57 (WorldHud fields).
- Bottom: left = tower + machine cards on a black corner fade (JuicyCard feel, faces baked by progress/patches/make_ui_art.py with Kenney renders, price tag $cost / FREE / FULL, tooltip, right-click sells a machine); middle = hand; right = Burst toggle, Sort, Discard, Start Wave + deck pile.
- Items = small cards top-left (tooltip, click to use). All buttons use PolishButton = Wong's UIButton package behaviour (hover punch 5 deg + scale, press shrink, release grow); rewritten because the original imports Microsoft.Unity.VisualStudio.Editor (breaks WebGL); the click sound was not in the package.
- Fonts: TMP assets in Art/Resources/Fonts (Action Comics only has capitals and few symbols; fallback Super Chips; Action Comics draws bigger than its size -> autosize on buttons).
- Files: Scripts/UI/UiKit, PolishButton, JuicyCard, BuildCardBar, ItemCardBar, WorldHud, HudLayout; patch progress/patches/hud_layout_patch.py (TDHud, HandView, ComboView); art Art/Resources/UI, BuildCards. Screens: progress/assets/ptd_ui_v3.png, ptd_ui_wave-1.png, ptd_ui_shop.png.

## Done (2026-11-24, HUD in the scene)
- Wong wants to see and edit all UI in the Editor: new menu "Poker TD/Build New HUD In Scene" (Editor/PokerTDHudBuilder.cs) builds WorldHUD (boards + texts, materials M_TD_WoodDark/WoodDark2/WoodFrame), BuildFade + BuildCards (tower cards), ItemCards (slots), Hand/DeckPile, Sort + Burst buttons, styles every button (Visual child + PolishButton), shared outline materials in Art/Resources/Fonts, switches off Status + Tower0-2, saves the scene. Ran 2026-11-24, scene saved.
- At Play the scripts use the scene objects (HudLayout.builtInScene = true; HandView.SortButton / BuildDeckPile and TDHud burst button reuse objects by name). Runtime-only: machine cards, item cards, tooltips. Tower card positions come from BuildCardBar origin/step. Running the menu again rebuilds those parts (hand edits lost). Run it again after "Build Prototype Scene".
- Play test: sort, burst toggle, tower card select all work; 0 errors. Screen: progress/assets/ptd_scene_play.png.

## Done (2026-11-24, Wong's own layout)
- Wong placed the world UI by hand: no wooden boards; texts lie flat on the table (rot X 90): "LEVEL n" + WAVE top-left of the map, COMBOS top-right, ECONOMY/HEALTH/BUILD/HAND on the right; Hint deleted; camera (0.6, 32.23, -14.85), pitch 65. Scene saved.
- Code follows it: WorldHud texts are all optional (null-safe), levelWord = "LEVEL", Build() makes this flat layout, defaults = Wong's camera. "Build New HUD In Scene" no longer touches an existing WorldHUD; new menu "Poker TD/Rebuild World HUD (resets the 3D texts)" with a confirm dialog. Wood materials no longer used (M_TD_Wood*.mat can be deleted).
- Open: the HUD canvas was switched off in the saved scene (no hand/buttons in Play) - ask Wong.

## Done (2026-11-24, zones + 3D items + new card faces)
- Wong made two HUD zones: TowerArea and CardArea. Both got a soft black background (UI/ZoneFade.png, alpha 0.55). HandView.zone / BuildCardBar.zone fit the cards into them (card size from the zone height, price tags inside).
- Items are now 3D cards on Wong's item mat (WorldHUD/.../ItemArea, sprite UI/ItemZone.png: dark mat, dashed line, 4 dashed slots): new ItemTable.cs on WorldHUD (raycast hover/click, pop-in, idle bob, lift + tilt on hover, grey when blocked, burn away when used, tooltip). The UI ItemCardBar is switched off.
- Build/machine/item faces redrawn in the playing-card style (white face, corner letters + icon top-left and turned bottom-right, model, name + TOWER/MACHINE/ITEM): progress/patches/make_ui_art_v2.py.
- TDHud works without the old Status / Tower0-2 (Wong put them in "Delete if it save"); old machine/item buttons are no longer made (tdhud_cleanup_patch.py).
- Camera: hold and drag the table (CameraDragPan.cs on Main Camera; the grabbed point stays under the pointer; 8 px threshold; limit +-8 x / +-6 z; presses on the HUD never drag). Board clicks now count on release without a drag (CameraDragPan.Clicked in BuildManager and ItemTable). Edge panning was tried and dropped (Wong: not good); CameraEdgePan.cs moved to _to_delete/scripts.
- Tower cards now as tall as the hand cards (HandView.cardHeightInZone) on the same centre line; price tag on the card's lower edge. Hand fan flatter (fanDrop 2.5) and both curves centred on the zone line (middle up, ends down), so the rows look level.
- Note: at the current camera the item mat (x 20.5, z -6.5) is mostly off screen behind the buttons; Wong to move it.

## Key decisions (still in force)
- Poker TD look (2026-11-24, Wong): card feel = Mix and Jam "Recreating Balatro's Game Feel" (YouTube, MIT repo mixandjam/balatro-feel), DOTween; assets from AssetPack (Kenney CC0, Freepik card sheet 22331, fonts incl. personal-use ones - OK, school project only). Towers: Cannon=cannon, Gatling=turret, Trap=catapult.
- Poker TD: Balatro + Kingdom Rush references; damage = chips x mult; rings around buff machines (ring = buff order); hand of 8; Flush/Straight need 5 cards; start tower limit 5; machine/item shop like Balatro jokers.
- Dam Rush concept: the player is the LEADER of a beaver family; orders on a pointy-top hex grid (radius 2 m, Patel 2013); beavers work by instinct. Survive endless seasons (4 x 10 days, day = 240 s); lose when the whole family is dead. Food is its own resource; no fish (beavers do not eat fish). Family cap 30.
- Dam Rush flood: must hurt but always have an answer (levee, sandbags, floodgate, alarm bell the leader rings; no automatic alarm). Flood lasts about a week, leaves duckweed/mud/driftwood.
- Dam Rush industry: 2 labs paid with food + research time; machines need a water wheel + a beaver; Hay Day style orders, automation later.
- Water: game uses B (Burst, WebGL 2 safe). S (GPU) is the best look -> later WebGPU bonus build. Methods: virtual pipes (Mei et al., 2007), Manning friction (de Almeida et al., 2012).
- HUD style: Sandcastle (Bubblebird Studio) + hex bee builder references; approved 2026-11-14.
- Virtual Pet: keep Wong's hand-made lighting (do NOT run "Wind Pet/Build Lab In Scene").
- One .asmdef per game.

## Didn't work (do not repeat)
- Card PNG filter on Point ("None") = jagged cards; keep Trilinear + mipmaps.
- UI copies of a template that built its own child layers in Awake get the layers twice (empty white Face showed as a white fringe): remove copied layers in Build.
- X/Y rotation on a Screen Space Overlay canvas only squashes the card (no perspective): use CardPerspective (vertex perspective).
- device_commit_files often writes an OLD copy (even to a new file name): always check md5; if wrong, apply the edit on the device with python.
- Compiling while Wong is in Play mode wipes the session: check EditorApplication.isPlaying and ask Wong first.
- Deleting files from the device shell: no permission; use _to_delete/.
- device_commit_files sometimes writes an older copy: check md5; else edit on the device with python.
- Patch scripts must be safe to run twice (check if the edit already exists).
- Toggling Burst options in Play mode makes Burst recompile (10x slower for a while).
- Water: too many fixed steps per slow frame = spiral (keep 25 steps/s, 0.2 s fast step).
- Dam Rush as a builder/empire game with predators and crafting: off-concept. One-hex dams: water flows around.
- Poker TD: % health buffs, x1.5 per hand level, early burst for High Card - rejected.

## Open questions
- Flood: 6/12 swept away in one wave too much for year 1? Sandbags without sand (year-1 flood comes before the sifter)?
- 5x speed ~24 fps (was ~33): do the water streaks cost GPU time?
- Food storage holds 40 (60 after B5) but B3/B4 cost 60-70 food: OK or pay bit by bit?
- Pump on high dry ground sends water downhill: keep or only pump into hexes that touch water?
- Poker TD: keep early burst? Two machines with the same ring number: order?
- Edit-mode NullReference in CameraRig.LateUpdate after a recompile (harmless; guard in polish).
- Real Game Systems deadline still unknown.

## Assets
- assets/cards_sliced_preview.png, ptd_cards_v2b.png, ptd_worldcards_towers.png, ptd_worldcards_wave2.png - (no text: view the original) Poker TD cards before/after polish
- assets/PokerTD_Model.xlsx -> md/PokerTD_Model.xlsx.md - Poker TD economy and damage model.
- assets/PokerTD_Reference.xlsx -> md/PokerTD_Reference.xlsx.md - Balatro + Kingdom Rush analysis, APA.
- assets/WindPet_Balance.xlsx -> md/WindPet_Balance.xlsx.md - Virtual Pet bars.
- assets/Research_Beaver_AI_and_Water.md -> md/Research_Beaver_AI_and_Water.md.md - beaver behaviour + water methods, APA.
- assets/Sandbox_Plan_v2.md, v3.md, v4.md - Dam Rush plans (v4 current: floods + defences in section 10).
- assets/*.png - (no text: view the original) screenshots: ui_v2 (HUD), map_random (maps), B_main (water B), wc* (water compare), phaseC/fix (older UI), Sack_Preview_v3.

## Other key files
- Assets/Progression/ - Poker TD (namespace PokerTD); Data/TDConfig.asset; Editor/PokerTDBuilder.cs; Art/, Materials/, Prefabs/, Scenes/.
- Poker TD cards: Scripts/UI/CardView.cs, HandView.cs, CardArt.cs, CardPerspective.cs; Scripts/Combat/WorldCard.cs, MarkRow.cs, CardMarks.cs; Shaders/PTD_CardDissolve.shader; Art/Resources/Cards + M_CardDissolve.mat. Patches: progress/patches/cards_world_patch.py, cards_click_patch.py. "Before" shots: Assets/Screenshots/ptd_before_*.png.
- AssetPack (C:/Users/ADMIN/OneDrive/Desktop/AssetPack): Model/kenney_tower-defense-kit, UI/ (Kenney UI packs, card sheet), Font/.
- Assets/Sandbox/ - Dam Rush (namespace DamRush). Scenes/MainMap.unity ("Dam Rush/Build Main Map" rebuilds the WHOLE scene).
- Assets/Sandbox/Scripts/Colony/ - ColonyHud.cs (HUD), FloodSystem.cs, Industry.cs, BuildOrders*.cs, ColonyManager*.cs, Beaver*.cs, Seasons.cs. UI/HudIcons.cs (painted icons; PNGs in Resources/HudIcons/ replace them).
- Assets/Sandbox/Scripts/Water/WaterGrid.cs, Map/MapRandomizer.cs, Shaders/DR_StylizedWater.shader, Data/MainMapConfig.asset.
- Assets/Sandbox/WaterCompare/ + Scenes/WaterCompare.unity - water demo (keep).
- 03_Sandbox/1_Plan/Sandbox_Plan_v4.md - current Dam Rush plan.
- Assets/VirtualPet/ - Virtual Pet (namespace WindPet).
- progress/backup/ - old versions of scripts and the full PROGRESS history.
