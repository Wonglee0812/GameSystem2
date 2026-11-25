# PROGRESS - Game Systems (3 solo Unity games)
Last updated: 2026-11-24 21:00 (Brick Panic: tsunami rework + endless round loop done)
Full history: backup/PROGRESS_full_2026-11-24_roundloop.md (state before this update), older: backup/PROGRESS_full_2026-11-24*.md

## Goal
Three solo games in one Unity project (GameSystem2): Virtual Pet, Progression (Poker TD), Sandbox (Brick Panic). Aim: Excellent. Deadline about 2026-12-10 (teacher, 2026-11-24). Rubric checklist: 00_Course/Rubric/Rubric_Checklist.md.

## Current status
- Virtual Pet: finished as it is (teacher, 2026-11-24). No sack shell: in the video, explain the base shape (one circle + two triangles).
- Poker TD: core done, paused for now (Wong, 2026-11-24). Left: step 6 (CSV log + Excel graph), Excel model update, content plan, balance - Wong picks later.
- Brick Panic: playable endless loop in Scenes/BrickPanic_Main.unity (forecast -> build -> disaster -> settle -> count -> reward or game over). Tested OK by Wong. HUD is still a temporary OnGUI. Missing: real UI, balance, special blocks, playtests.

## Next steps
1. Brick Panic real UI built into the scene (Wong must see/edit it); then set RoundManager.showDebugHud off. Parts: hotbar (material/shape/people), money, round/goal/alive, forecast + START button, round result + CONTINUE, game over + RESTART, joint wear. Kenney UI from AssetPack. RoundManager already exposes Current, Round, Goal, ForecastText, LastSurvivors/LastDead/LastReward/LastRubble, StartDisaster(), Continue(), Restart(), NewMap().
2. Update the plan (03_Sandbox/1_Plan/BrickPanic_Plan_v1.md): section 5 tsunami as built now, section 8 wear carries over, round loop as built.
3. Balance (Excel model like Poker TD): tsunami is strong (round 1 knocked over almost all map pieces: 79 rubble); per-level steps; settleSeconds (some people fall off after they were counted); volcano kills few; level-1 quake wearStrength 0.5.
4. Special blocks (plan 4.3): anchor, glue, buoy (water tank stretch).
5. Playtest 1 + 2 (rubric: prototype-test-improve), screenshot key for player builds, WebGL build + itch.io.
6. Poker TD: Wong picks what is still needed (step 6 CSV + graph, Excel update, content plan, balance).
7. Write the 3 reports (Poker TD juice sources: Jonasson & Purho 2012, Nijman 2013, Kelly 2014, Hicks et al. 2018, Eiserloh 2016); videos last.

## Done - Brick Panic round loop (2026-11-24)
- RoundManager (Scripts/Game/RoundManager.cs, on Systems): Build (no timer, START / Enter; F5 new map only before round 1) -> Disaster -> Settle 3 s -> count. Survivors >= goal: reward 40 + 8 per extra survivor, CONTINUE (goal +1). Else GAME OVER, RESTART (R) reloads the scene and resets static counters.
- Forecast: random disaster, never the same twice in a row; tsunami side picked at forecast (Tsunami.Begin(side)).
- After a round: dead bodies removed; bricks moved > 0.4 m or turned > 12 deg = rubble, removed with a smoke puff; standing bricks frozen (kinematic); joints and wear kept.
- PhysicsActivator: Activate only joins pairs with at least one new brick (old joints keep wear, broken joints never rejoin), grace time for all joints, Freeze(), Forget() on brick destroy (else a joint pins the neighbour to the world), AverageWear. BuildController.BeginBuildPhase / EndBuildPhase.
- Disaster.SetLevel(level), level = round, "Per level" fields in the Inspector: quake peak +20%, wear +25%, +2 s; tsunami speed +10%, +1 layer per 2 levels, Power kept +0.02; volcano bombs +25%, big chance +0.05.
- Test: R1 tsunami 13/5 alive, +104; R2 volcano 10/6, +72, 38 bricks frozen, wear 50% kept; R3 quake 2/7 = game over; Restart OK. New brick on an old wall = exactly 1 new joint.
- RoundTester disabled (debug keys only). BrickPanic_Main added to the Build Profile scene list. BrickPanicMainBuilder adds RoundManager.

## Done - Brick Panic tsunami rework (2026-11-24)
- Wave brick = own drive (Power) like v1 + gravity always on (no buoyancy). Pushes only when there is island below (raycast). Over the edge, Power < 5% or stuck 2.5 s -> breaks into 8 small BrickFx spray bricks.
- Smooth invisible WaveGround MeshCollider (layer 13 BrickPanicWaveGround, only wave bricks touch it, exists only during a tsunami); wave bricks ignore the map ground (layer 12 BrickPanicGround). All raycasts/overlaps use BrickGrid.QueryMask (no ~0).
- Spawn on an invisible shelf (removed after 3 s) with thin lane side walls; wave bricks collide with each other.
- Defaults: speed 7, push 25, mass 160, powerAfterHit 0.7, minPower 0.05, 4 layers x 3 rows (144 bricks). Test: 100/144 reach the edge. Debug counters WaveBlock.EndEdge/EndPower/EndStuck. DisasterState.WearScale added (tsunami wearStrength 1).

## Done - Brick Panic base (2026-11-24, details in backup/PROGRESS_full_2026-11-24_brickpanic.md)
- Plan v1 (03_Sandbox/1_Plan/BrickPanic_Plan_v1.md, APA). Physics spike: keep 500-600 bricks. Scale: 1 stud = 0.5 m, brick 0.6 m, person 1.8 m. Materials (durability s / joint N per stud): plastic 10/250, wood 20/500, stone 35/1200, steel 60/3000 (placeholders).
- Map: 48x48 Perlin floating island, volcano, map pieces made of bricks. Building: grid preview, 1-4 material, Z/X shape, R rotate, place/sell 50%/move/undo, Tab people (5 coins).
- People: physics steering, panic spreads and calms, traits, toy tumble, faint, death causes. Disasters: quake (peak g + lean + wear), tsunami, volcano (bombs, explosions, fire). BrickFx: all effects made of small bricks.

## Done - Poker TD look and feel (2026-11-24, details in backup/PROGRESS_full_2026-11-24_polish.md)
- Balatro-feel cards + world HUD on the table, Kenney 3D art, juice system (Juice.cs), dissolve as signature effect, machine rings + radar wave, board transition, 3D shop zones, Game Over. Patch scripts in progress/patches/ (safe to run twice).

## Key decisions (still in force)
- Brick Panic is endless; wear carries over between rounds (Wong, 2026-11-24).
- Tsunami = per-brick drive that drops on every hit, gravity always on, breaks into spray at the edge (Wong, 2026-11-24). Pure physics versions were too weak (see Didn't work).
- Brick Panic core rule (Wong, 2026-11-24): every disaster can destroy every material; a stronger material only buys time.
- Brick Panic: floating island + free fly camera; people placed anywhere; volcano always on the map; rounds on the same map; disaster shown before building; map pieces move free / sell 50%; bonus per extra survivor; all effects made of Lego bricks (BrickFx).
- Brick Panic tech: physics steering, not NavMesh; toy tumble instead of a bone ragdoll; only stacked bricks join (FixedJoint, strength x overlapping studs).
- Dam Rush is dropped (teacher, 2026-11-24).
- Poker TD (Wong, 2026-11-24): Balatro feel but not a copy; info as 3D world UI; all UI must exist in the scene so Wong can see and edit it; assets from AssetPack (school project only); keep early burst; machines can be moved, sold only from the hand; 3D shop zones; tower is a card until it gets a poker card; dissolve = signature effect.
- Virtual Pet: keep Wong's hand-made lighting (do NOT run "Wind Pet/Build Lab In Scene").
- One .asmdef per game.
- Code rule (Wong, 2026-11-24): every public / Inspector field gets a [Tooltip] with what it does, the unit and the default value.

## Didn't work (do not repeat)
- Tsunami as dam break (gravity only): looks like pouring water, too weak. One start speed only: momentum is gone after ~10 m because the 0.6 m ground steps act like walls (fixed by WaveGround). Buoyancy (gravity cancelled): bricks float in the air after leaving the island.
- Brick Panic: Animator MUST be on Model/Root, never on the Rigidbody root. Quake strength is peak acceleration in g, not metres; joints need 0.5 s grace. Panic spreading both ways never calms down. Kenney 2x2 brick is 0.99 m -> scale ground bricks 1/0.99.
- Super Chips font: broken kerning pairs for '+' and '%' (removed 134 pairs, backup: backup/font_2026-11-24/). Remove again if the overlap comes back.
- Poker TD: camera edge panning; wooden 3D signboards; UI built only at runtime; card PNG on Point filter; copied UI templates get layers twice; X/Y rotation on an Overlay canvas (use CardPerspective); % health buffs, x1.5 per hand level, early burst for High Card.
- device_commit_files often writes an OLD copy (caches by staged file name): use a new staged file name each time and check md5; else edit on the device with python.
- Compiling while Wong is in Play mode wipes the session: check EditorApplication.isPlaying and ask Wong first. Save Wong's unsaved scene changes before compiling.
- Deleting files from the device shell: no permission; use _to_delete/.
- Patch scripts must be safe to run twice. Toggling Burst options in Play mode = slow recompile. Water: keep 25 steps/s, 0.2 s fast step.
- Dam Rush as a builder/empire game: off-concept.

## Open questions
- Brick Panic: people can fall off after they were counted as survivors (longer settleSeconds?).
- Poker TD: two machines with the same ring number: which goes first (now: the one built first)? Click sound: later (Wong).
- Edit-mode NullReference in CameraRig.LateUpdate after a recompile (harmless; guard in polish).
- Block.JointsBroken can count a joint twice (stat only).

## Assets
- assets/BrickPanic_Plan_v1.md -> md/BrickPanic_Plan_v1.md.md - Brick Panic plan (not yet updated for the tsunami rework and the round loop).
- assets/bp_tsunami_drive.png, bp_round_hud.png, bp_round_result.png, bp_gameover.png - (no text: view the original) new tsunami sweeping, round HUD, round result, game over (2026-11-24).
- assets/bp_*.png (older) - (no text: view the original) Brick Panic spike, map, building, people, old tsunami, volcano screenshots.
- assets/tip_fix_machines.png, radar1.png, trans_hide3.png, ring_tiles7.png, s_*.png, r2-r5_*.png, juice_*.png, ptd_*.png, cards_sliced_preview.png - (no text: view the original) Poker TD polish steps.
- assets/PokerTD_Model.xlsx -> md/PokerTD_Model.xlsx.md - Poker TD economy and damage model.
- assets/PokerTD_Reference.xlsx -> md/PokerTD_Reference.xlsx.md - Balatro + Kingdom Rush analysis, APA.
- assets/WindPet_Balance.xlsx -> md/WindPet_Balance.xlsx.md - Virtual Pet bars.
- assets/Sandbox_Plan_v2-v4.md, Research_Beaver_AI_and_Water.md (+ md), Dam Rush *.png - dropped Dam Rush material (reference only).

## Other key files
- Assets/BrickPanic/ - Brick Panic (namespace BrickPanic, own asmdef + Editor asmdef). Scene: Scenes/BrickPanic_Main.unity (menu "Brick Panic/Build Main Scene" rebuilds the WHOLE scene; data assets are kept). Test scene: BrickPanic_Spike.unity.
- Brick Panic scripts: Game/RoundManager; Data/ (BrickGrid incl. layer names + QueryMask, MaterialDef, ShapeDef); Build/ (Block, BlockFactory, BuildController); Physics/ (PhysicsActivator, KillZone); Map/ (MapGenerator, EnvironmentDresser); People/Person; Disasters/ (Disaster+Hazard, DisasterState, Earthquake, Tsunami, WaveBlock, Eruption, LavaBomb); Fx/ (BrickFx, FireSpot); Camera/FlyCamera; Debug/ (RoundTester = disabled debug keys, SpikeSpawner, SpikeStats).
- Brick Panic data: Data/Materials, Data/Shapes (8 SHP_), Data/AC_Person.controller, Prefabs/Person.prefab. Art/Bricks (8 Kenney FBX), Art/Characters. Layers: 11 BrickPanicWave, 12 BrickPanicGround, 13 BrickPanicWaveGround.
- Assets/Progression/ - Poker TD (namespace PokerTD); Scenes/PokerTD.unity; Editor builders (PokerTDBuilder rebuilds the WHOLE scene - then run "Build New HUD In Scene" again). Menus: Apply Kenney Art, Build Juice In Scene, Build 3D Shop (safe to run twice).
- AssetPack (C:/Users/ADMIN/OneDrive/Desktop/AssetPack): Kenney tower-defense, brick-kit, characters, UI packs, card sheet, fonts.
- Assets/Sandbox/ - Dam Rush (DROPPED, keep files). Assets/VirtualPet/ - Virtual Pet (namespace WindPet).
- Assets/Screenshots/ - test screenshots from Claude (can go to _to_delete/).
- progress/backup/ - old script versions (tsunami_2026-11-24/, roundloop_2026-11-24/) and the full PROGRESS history.
