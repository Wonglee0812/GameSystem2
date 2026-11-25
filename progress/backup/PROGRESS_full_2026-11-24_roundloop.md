# PROGRESS - Game Systems (3 solo Unity games)
Last updated: 2026-11-24 20:40 (Brick Panic: map, building, people, 3 disasters, brick effects done)
Full history: backup/PROGRESS_full_2026-11-24_brickpanic.md (all details of today), older: backup/PROGRESS_full_2026-11-24*.md

## Goal
Three solo games in one Unity project (GameSystem2): Virtual Pet, Progression (Poker TD), Sandbox (Brick Panic). Aim: Excellent. Deadline about 2026-12-10 (teacher, 2026-11-24). Rubric checklist: 00_Course/Rubric/Rubric_Checklist.md.

## Current status
- Virtual Pet: finished as it is (teacher, 2026-11-24). No sack shell: in the video, explain the base shape (one circle + two triangles).
- Poker TD: look and feel done; tooltip font fixed (2026-11-24). Left: step 6 (CSV log + Excel graph), Excel model update, content plan, balance - Wong has not chosen yet which ones.
- Brick Panic (new Sandbox, replaces Dam Rush): playable test build in Scenes/BrickPanic_Main.unity. Random floating island + volcano, building (4 materials x 8 shapes), people with panic AI, earthquake / tsunami / volcano, wear, brick effects. Missing: round loop + economy, forecast, real UI, balance, playtests.

## Next steps
1. Brick Panic round loop + economy (plan sections 2-3): RoundManager replaces RoundTester. Forecast (disaster + side; Tsunami.PickSide()) -> build (no timer, Start button) -> disaster (physics on via PhysicsActivator.Activate) -> count survivors -> reward 40 + 8 per extra survivor -> next round on the same map; goal 5 survivors +1 per round; run ends when survivors < goal. Disasters stronger per level (quake peakAcceleration/wearStrength, tsunami layers/speed, volcano bombsPerSecond/bigChance). After a round: freeze standing bricks again, clean rubble.
2. Brick Panic real UI built into the scene (Wong must see/edit it): hotbar (material/shape/people), money, forecast, survivors/goal, round result, game over. Kenney UI from AssetPack.
3. Balance (Excel model like Poker TD): volcano kills few people (raise bombs per level); big tsunami kills a lot (11 -> 4 in one test); level-1 quake wearStrength 0.5.
4. Special blocks (plan 4.3): anchor, glue, buoy (water tank stretch).
5. Playtest 1 + 2 (rubric: prototype-test-improve), screenshot key for player builds, WebGL build + itch.io.
6. Poker TD: Wong picks what is still needed (step 6 CSV + graph, Excel update, content plan, balance).
7. Write the 3 reports (Poker TD juice sources: Jonasson & Purho 2012, Nijman 2013, Kelly 2014, Hicks et al. 2018, Eiserloh 2016); videos last.

## Done - Brick Panic (2026-11-24, details in backup/PROGRESS_full_2026-11-24_brickpanic.md)
- Plan v1 written and updated as built (03_Sandbox/1_Plan/BrickPanic_Plan_v1.md, APA sources).
- Physics spike: 853 bricks + 1230 joints at 110-145 fps; activation 40 ms. FPS drops to 11-50 when ~800 bricks collapse at once -> keep 500-600 bricks, remove rubble.
- Scale: 1 stud = 0.5 m, brick 0.6 m, plate 0.2 m, person 1.8 m. Materials (durability s / joint N per stud): plastic 10/250, wood 20/500, stone 35/1200, steel 60/3000 - placeholder numbers.
- Wear: joints lose strength by intensity x (1 + load) / durability; DamageBlock(block, seconds) for direct hits; Ignite/Burn for plastic (1 s) and wood (4 s).
- Map: 48x48 Perlin island, sand/grass/rock, hanging rock, volcano with lava crater; trees/bushes/rocks/huts made of bricks; seed in the Console.
- Building: grid preview, 1-4 material, Z/X shape, R rotate, click place, short right click sell 50%, middle click move free, Ctrl+Z undo, Tab people (5 coins, anywhere).
- People: physics steering, Idle -> Panic (spreads, calms together), flee bricks/hazards, herd, random traits, step jump, toy tumble, faint on head hits, death causes (hit, head hit, crushed, fall, fell off, lava, explosion, fire).
- Disasters: Earthquake (peak g + lean + wear), Tsunami (180 big wave bricks, 5 layers = 6 m, 3 rows, 5 m/s, power halves per hit, fades below 8%), Volcano (brick smoke + sparks, 1.5-2.5x bombs at half gravity with smoke trail, big bombs explode: push, kill radius 1.3 m, fire spot 6 s).
- BrickFx: all effects made of small Lego bricks (pool of 700). Test keys in RoundTester: P quake, T tsunami, V volcano, F5 new map, F6 test crowd.
- Last tests (14 people): quake 10-13 alive; small tsunami 11 alive; big tsunami 11 -> 4 alive; volcano 11 alive (3 dead).

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
- Brick Panic core rule (Wong, 2026-11-24): every disaster can destroy every material; a stronger material only buys time. Losses always happen; the skill is losing little and still reaching the goal.
- Brick Panic: floating island like Poker TD + free fly camera; people placed anywhere; volcano always on the map; rounds on the same map; disaster shown before building; map pieces move free / sell 50%; bonus per extra survivor; all effects made of Lego bricks (BrickFx), no normal particles.
- Brick Panic tech: people move with physics steering, not NavMesh (the island moves); toy tumble instead of a bone ragdoll; only stacked bricks join (FixedJoint, strength x overlapping studs); wear does NOT carry over between rounds (assumption, one switch later).
- Dam Rush is dropped (teacher, 2026-11-24). Its old decisions are in backup/PROGRESS_full_2026-11-24_brickpanic.md.
- Poker TD look (2026-11-24, Wong): Balatro card feel but not a Balatro copy; info as 3D world UI on the table; all UI must exist in the scene so Wong can see and edit it. Assets from AssetPack (Kenney CC0, Freepik card sheet 22331, fonts incl. personal-use ones - OK, school project only). Towers: Cannon=round tower + cannon, Gatling=square tower + turret, Trap=spike plate (the catapult looked like a cannon).
- Poker TD: keep the early burst, no toggle button (Wong, 2026-11-24).
- Buff machines can be moved: right-click returns them to the hand; only the hand card can be sold (Wong, 2026-11-24).
- Shop = 3D zones like Unrailed, not a Balatro row; no UI shop panel (Wong, 2026-11-24).
- Tower placement teaches the rule: a tower is only a card on the tile until it gets a poker card (Wong, 2026-11-24).
- Dissolve is the signature effect of Poker TD: use it for things appearing/disappearing (Wong, 2026-11-24).
- Poker TD: Balatro + Kingdom Rush references; damage = chips x mult; rings around buff machines (ring = buff order); hand of 8; Flush/Straight need 5 cards; start tower limit 5; machine/item shop like Balatro jokers.
- Virtual Pet: keep Wong's hand-made lighting (do NOT run "Wind Pet/Build Lab In Scene").
- One .asmdef per game.
- Code rule (Wong, 2026-11-24): every public / Inspector field gets a [Tooltip] that says what it does, the unit and the default value, so Wong can tune it without reading the code.

## Didn't work (do not repeat)
- Brick Panic: Kenney character clips animate the object the Animator sits on -> Animator MUST be on Model/Root, never on the Rigidbody root (teleport to 0,0,0, x100 scale, instant death).
- Brick Panic: quake set in metres (0.35 m at 2.2 Hz = 7 g) destroyed everything -> strength is peak acceleration in g. Joints break on the first solver push -> 0.5 s grace time.
- Brick Panic tsunami: wave bricks hitting each other = spray explosion; stuck at coast steps; flying into the sky -> own layer BrickPanicWave (ignore each other), climb when blocked, vertical damping.
- Brick Panic: panic spreading both ways never calms down -> others only pull panic up to their own level; decay when the disaster is over. Kenney 2x2 brick is 0.99 m -> scale ground bricks 1/0.99 (else seams).
- Super Chips font had broken kerning pairs for '+' and '%' (text overlapped, e.g. '+%CHIPS', '+25%'). Fixed 2026-11-24: removed 134 pairs in Super Chips SDF.asset (backup: backup/font_2026-11-24/). If the overlap comes back after new glyphs are added to the dynamic atlas, remove the '+'/'%' pairs again.
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
- Brick Panic: does wear carry over to the next round? (now: no)
- Poker TD: two machines with the same ring number: which goes first (now: the one built first)? Click sound: later (Wong).
- Edit-mode NullReference in CameraRig.LateUpdate after a recompile (harmless; guard in polish).
- Block.JointsBroken can count a joint twice (stat only, fix in the round system).

## Assets
- assets/BrickPanic_Plan_v1.md -> md/BrickPanic_Plan_v1.md.md - Brick Panic plan: concept, loop, economy, blocks, disasters as built, people AI, map, physics, rubric check, schedule, APA references.
- assets/bp_*.png - (no text: view the original) Brick Panic screenshots: spike (7g/05g/10g/wear), map2/build1/map_quake, people_idle3/quake1/quake2, tsunami_wall3 + tsunami_big, volcano1/volcano2/volcano_explode.
- assets/tip_fix_machines.png - (no text: view the original) all 4 machine tooltips after the font fix
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
- assets/Sandbox_Plan_v2-v4.md, Research_Beaver_AI_and_Water.md (+ md), Dam Rush *.png - dropped Dam Rush material (keep for reference only).

## Other key files
- Assets/BrickPanic/ - Brick Panic (namespace BrickPanic, own asmdef + Editor asmdef). Scene: Scenes/BrickPanic_Main.unity (menu "Brick Panic/Build Main Scene" rebuilds the WHOLE scene; data assets are kept). Test scene: BrickPanic_Spike.unity.
- Brick Panic scripts: Data/ (BrickGrid, MaterialDef, ShapeDef), Build/ (Block, BlockFactory, BuildController), Physics/ (PhysicsActivator: joints, grace, wear, DamageBlock; KillZone), Map/ (MapGenerator, EnvironmentDresser), People/Person, Disasters/ (Disaster+Hazard, DisasterState, Earthquake, Tsunami, WaveBlock, Eruption, LavaBomb), Fx/ (BrickFx, FireSpot), Camera/FlyCamera, Debug/ (RoundTester = temporary loop + OnGUI HUD, SpikeSpawner, SpikeStats).
- Brick Panic data: Data/Materials (MAT_Plastic/Wood/Stone/Steel + M_BP_* materials), Data/Shapes (8 SHP_), Data/AC_Person.controller, Prefabs/Person.prefab (menu "Brick Panic/Build Person Prefab"). Art/Bricks (8 Kenney FBX, scale 6.25), Art/Characters (Kenney character + idle/run/jump, scale 0.5, 4 skins). Layer 11 = BrickPanicWave.
- 03_Sandbox/1_Plan/BrickPanic_Plan_v1.md - current Sandbox plan.
- Assets/Progression/ - Poker TD (namespace PokerTD); Data/TDConfig.asset; Editor/PokerTDBuilder.cs (rebuilds the WHOLE scene - then run "Build New HUD In Scene" again), Editor/PokerTDHudBuilder.cs; Scenes/PokerTD.unity.
- Poker TD UI: Scripts/UI/ CardView, HandView, CardArt, CardPerspective, JuicyCard, BuildCardBar, ItemTable, WorldHud, HeartBar, HudLayout, UiKit, PolishButton, ComboView, TDHud, Shop3D (ShopView off); Scripts/Game/ CameraDragPan, BoardTransition, GridOverlay, PTDArt, BuildManager, BuildNode; Scripts/Combat/ WorldCard, MarkRow, CardMarks; Scripts/Juice/ Juice, JuiceParts.
- Poker TD shaders: PTD_CardDissolve, PTD_ModelDissolve, PTD_InkBackground (Swirl unused). Editor menus: Apply Kenney Art, Build Juice In Scene, Build 3D Shop (all safe to run twice; Kenney Art rebuilds the board art).
- Poker TD art: Art/Resources/Cards, BuildCards, UI (+Icons), Fonts; Art/Kenney_TD (160 FBX); Art/Fonts (TTF). Kenney renders for the faces: _to_delete/icons (made in Unity).
- AssetPack (C:/Users/ADMIN/OneDrive/Desktop/AssetPack): Model/kenney_tower-defense-kit, kenney_brick-kit, kenney_animated-characters-protagonists, UI/ (Kenney UI packs, card sheet, UIButton.unitypackage), Font/.
- Assets/Sandbox/ - Dam Rush (DROPPED, keep files, do not work on it).
- Assets/VirtualPet/ - Virtual Pet (namespace WindPet).
- progress/backup/ - old versions of scripts and the full PROGRESS history.
