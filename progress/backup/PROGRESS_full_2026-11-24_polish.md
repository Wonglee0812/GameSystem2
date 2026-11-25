# PROGRESS - Game Systems (3 solo Unity games)
Last updated: 2026-11-24 17:00 (radar rings, hidden floors in transition, safe shop builder)
Full history before 2026-11-24: progress/backup/PROGRESS_full_2026-11-24.md

## Goal
Three solo games in one Unity project (GameSystem2): Virtual Pet, Progression (Poker TD), Sandbox (Dam Rush). Aim: Excellent on all three, finished before Christmas 2026. Rubric checklist: 00_Course/Rubric/Rubric_Checklist.md.
Order of work (Wong, 2026-11-12): finish Poker TD -> build Sandbox -> skeletons of all 3 -> write the 3 reports -> polish all 3 -> videos last.

## Current status
- Virtual Pet (wind pet): about 80%, playable. Parked until the polish phase. Wong showed it to his teacher on 2026-11-24.
- Poker TD: core done (steps 1-5 of 6). Graphics polish: cards + HUD DONE and approved (2026-11-24). 3D models (Kenney) DONE 2026-11-24, not yet checked by Wong. Still old style: shop panel, game over screen, hover calc panel. Left after that: step 6 (CSV log + graph), Excel model update, content plan, balance.
- Dam Rush: Phases A, B, C coded; flood v2 + defences coded; new HUD approved; random smooth map + water B (Burst, 0.5 m) in the game. Waiting for Wong's playtest and balance.

## Next steps
1. Wong play-tests the 3D shop + transition + Game Over and gives changes. Then Poker TD step 6 (CSV log + graph) or Dam Rush.
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

## Done (2026-11-24, Poker TD Kenney 3D)
- Menu "Poker TD/Apply Kenney Art" (Editor/PokerTDKenneyBuilder.cs, safe to run twice): Kenney tiles under Board/KenneyArt (grass, path straight/corner/end picked from the path shape), small decor on 22% of grass tiles (BuildNode.deco, hidden while built on), spawn hole tile outside the board + Waypoint_Spawn (enemies come out of it), castle at the path end, shop floor dressed (dirt yard, trees, fences, crystal bases). Old cubes keep the colliders (renderer off, all cubes now top at y 0.2). ShopFloor is inactive in edit mode (it overlapped the board).
- Runtime art: Scripts/Game/PTDArt.cs + Art/Resources/PTDArt.asset. Cannon = round tower + cannon, Gatling = square tower + turret, Trap = low base + catapult (path), machines = round base + crystals in the buff colour, enemies = UFO a/b/c/d (Normal/Speed/Tank/Boom) that bob and spin, ammo = cannonball / bullet. Graybox stays as fallback if the asset is missing.
- Towers and machines are children of Board (BuildManager already did this; see correction below).
- Grid lines on the board (Scripts/Game/GridOverlay.cs on Board/KenneyArt/Grid, made by the menu; brighter in Deploy, faint in a wave). Mouse-wheel zoom towards the pointer in CameraDragPan (zoomIn 16 m, zoomOut 4 m, pan room grows when zoomed in). Wong chose lines on top over separate 1x1 blocks.
- Trap = spike trap (Wong chose it over the catapult, which looked like a cannon): Kenney spawn-round plate + 3x3 spikes made in code (Tower.BuildSpikes, M_TD_Spike.mat), spikes pop up on each pulse. Trap card face re-rendered from _to_delete/icons/trap-spikes.png (make_ui_art_v2.py). Grid made stronger (width 0.07, alpha 0.45 deploy / 0.15 wave).
- Patch: progress/patches/kenney_art_patch.py (Tower, Enemy, CardMarks, BuffMachine, Projectile). BuildNode.cs rewritten (art + deco). Old versions: progress/backup/kenney_2026-11-24/.

## Done (2026-11-24, Poker TD juice)
- Scripts/Juice/Juice.cs (one Juice object, all switches, Inspector) + JuiceParts.cs (BurstScore, HitNumber, Coin, Flasher). Menu "Poker TD/Build Juice In Scene" (Editor/PokerTDJuiceBuilder.cs, safe twice) makes the Juice object, HUD/JuiceBanner, HUD/JuiceVignette (UI/Vignette.png) and SwirlBackground (Shaders/PTD_Swirl.shader, M_PTD_Swirl.mat).
- Effects: trauma camera shake (rotation only, Eiserloh 2016), hit stop on Pair+ (max 0.1 s, cooldown 0.25 s), Balatro-style burst score (chips x mult -> damage, bigger for better hands), suit-colour confetti, small hit numbers, UFO white flash + squash, weapon recoil + muzzle sparks, cannonball arc + landing dust, trap dust ring, UFO explosion + coins flying to GOLD, build drop with bounce + dust, castle shake + red vignette + LIVES flash, banners WAVE n / WAVE CLEARED! / LEVEL n, swirl background.
- Hooks: progress/patches/juice_patch.py (Tower, BuffMachine, Enemy, CardMarks, Projectile). Old versions: progress/backup/juice_2026-11-24/.
- Correction: towers were already parented to Board by BuildManager; the "floating towers" seen earlier came from my test (Tower.Build called directly). The extra SetParent in Tower/BuffMachine.Build is harmless.
- Sources for the report: Jonasson & Purho (2012), Nijman (2013), Kelly (2014), Hicks et al. (2018), Eiserloh (2016).

## Done (2026-11-24, juice round 2 - Wong's playtest notes)
- Dissolve is the shared effect: Shaders/PTD_ModelDissolve.shader (same noise + orange edge as the card dissolve) + Art/Resources/M_ModelDissolve.mat; ModelDissolve in Juice/JuiceParts.cs. Dead UFOs: a copy rises, spins and burns away (+ orange embers). Building: the build card flies from the bottom-left to the tile and burns away while the tower/machine burns in.
- Background: Shaders/PTD_InkBackground.shader = code port of Wong's SplatoonGraph (Game-Design repo, OpenScene) on InkBackground quad (M_PTD_Ink.mat, _Darken 0.9 so the table text stays readable; Wong can set the colours).
- Shots: bigger ammo, trails, stronger muzzle flash; cannon landing = flash + fireball + sparks + smoke + dust.
- Camera glides back to the start view when a wave ends (CameraDragPan.ResetView).
- "Burst at best hand" button removed (early burst stays on via config).
- World HUD: GOLD shows the number only; lives = HeartBar (UI/HeartBar.cs, 5 per row, 1 heart = 2 lives, half hearts; heart sprites UI/Icons/Heart_*.png); COMBOS list in 2 columns above the map (ComboView world mode).
- Item mat lower (ItemZone.png 768x400), now inside the map height.

## Done (2026-11-24, juice round 3)
- Ink background recoloured to the game palette (M_PTD_Ink: dark brown + maroon ink, _Darken 0).
- Dark zones behind tower cards and hand (HUD TowerArea/CardArea) fade out outside Deploy (TDHud.FadeZones).
- One tooltip for the whole game: UiKit.ShowTip (build-card style) now also for combos and the tower hover (old CalcPanel and combo tooltip unused). UiKit.HideTip ignores a hide in the same frame as a show.
- Slower: UFO burn (Juice.deathBurnTime 1.1 s) and hit flash (Juice.hitFlashTime 0.12 s).
- Discard button shows only "Discard". EarlyBurstToggle deleted from the scene and its code removed.

## Done (2026-11-24, round 4)
- A built tower first shows only its card lying flat on the tile (pulsing, hint "ADD A POKER CARD"); the model stays hidden. The first poker card on it burns the card away and the tower burns in (Juice.Built with waitFor, Juice.TowerArmed, Tower.Pending, WorldCard.Billboard). Machines still burn in at once.
- Tower card sits higher (+0.95 above the model top) and 0.9 behind the tower (Tower.cardBack), so it no longer covers the weapon from the tilted camera.
- One selection at a time: picking a tower/machine card clears the selected poker cards; picking a poker card clears the tower/machine card (BuildManager.Select/Deselect, HandView.Click).
- Patch file progress/patches/juice_patch.py now covers rounds 1, 2 and 4 (round 3 edits were made directly; old copies in progress/backup/juice_2026-11-24/).

## Done (2026-11-24, 3D shop + transition + Game Over)
- Transition (Game/BoardTransition.cs rewritten): tiles fall one by one from the centre out (towers/machines/decor with them), then the other floor's tiles rise centre-first with a small pop (like Unrailed). Grid: erased from the centre out going to the shop, drawn from the outside in coming back (GridOverlay.holeRadius, one quad per tile edge). Knobs on BoardTransition: depth, spread, pieceTime, overlap, gridTime, pop.
- 3D shop (UI/Shop3D.cs + menu "Poker TD/Build 3D Shop", Editor/PokerTDShopBuilder.cs): zones on mats like the item mat (UI/ShopZone.png): MACHINES (square Kenney pedestals, machine model on top), ITEMS and UPGRADES (cards lying flat), 3D REROLL / CONTINUE buttons (UI/Button3D.png), SHOP title. Hover = shared tooltip + lift, click = buy (dissolve away, "-$x"), deny = shake. Offers keep their slot; reroll burns old ones out and new ones in. Old ShopPanel UI switched off.
- Shop offers now per zone: TDConfig.shopMachineOffers / shopItemOffers / shopUpgradeOffers = 3 each (upgrades = hand level, slots, unlock by weight). shopOffers is unused now.
- Upgrade card faces: BuildCards/Upgrade_Hand, Upgrade_TowerSlot, Upgrade_MachineSlot, Upgrade_Unlock (progress/patches/make_shop_art.py).
- Game Over (basic plan): dark overlay fades in, GAME OVER drops in, "YOU REACHED LEVEL x · WAVE y", RESTART button (copy of Start Wave) + R key (TDHud.ShowGameOver / Restart).
- Patch: progress/patches/shop3d_patch.py. Old versions: progress/backup/shop_2026-11-24/.

## Done (2026-11-24, machine rings on tiles + move machines)
- Wong aligned the shop texts by hand. "Poker TD/Build 3D Shop" is now safe: it only adds missing parts and keeps everything in the scene (checked: no change after running it). Game Over is also kept once built.
- Buff machine rings: no more circles. Each tile in a ring gets a see-through colour square + the ring number, lit ring by ring from the machine outwards (scan). Shown when placing a machine, hovering a machine, or hovering a tower it reaches (BuildManager.UpdateRings, BuildNode.SetRing, ringScanStep 0.06 s).
- Right-click a placed machine = back to the hand (stock, free to place again) with a dissolve (BuildManager.ReturnMachine); selling only from the hand card (right-click, as before). Tooltip on placed machines.
- Old files: progress/backup/rings_2026-11-24/.
- Polish 2026-11-24: machine/tower labels on the board use the game font (Super Chips + outline). Ring tiles are almost invisible (BuildManager.ringIdle 0.12) and a radar wave goes from the machine to the edge (ringWaveTime 1.1 s, ringWavePause 0.35 s, ringWaveWidth 2.2). Transition: falling tiles hide at 45% of their fall (BoardTransition.hideAt), rising tiles appear only when they start to move, shop zones land after the floor (zoneLag 0.6), tower range rings hidden while the floor moves. Old files: progress/backup/polish_2026-11-24/.

## Key decisions (still in force)
- Poker TD look (2026-11-24, Wong): Balatro card feel but not a Balatro copy; info as 3D world UI on the table; all UI must exist in the scene so Wong can see and edit it. Assets from AssetPack (Kenney CC0, Freepik card sheet 22331, fonts incl. personal-use ones - OK, school project only). Towers: Cannon=cannon, Gatling=turret, Trap=catapult.
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
- Poker TD: Wong said "OK if we lock early burst for High Card" - confirm: should High Card be unable to early burst? The shop panel's dark background stayed on screen with GAME OVER (Wong's screenshot, 2026-11-24) - fix in the shop/game over restyle. Two machines with the same ring number: order? Click sound: later (Wong).
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
