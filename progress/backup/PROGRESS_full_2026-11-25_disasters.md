# PROGRESS - Game Systems (3 solo Unity games)
Last updated: 2026-11-25 04:40 (Brick Panic: new disasters Tornado + Lightning storm, with effects)
Full history: backup/PROGRESS_full_2026-11-25_polish.md (state before this update), backup/PROGRESS_full_2026-11-24_roundloop.md, older: backup/PROGRESS_full_2026-11-24*.md

## Goal
Three solo games in one Unity project (GameSystem2): Virtual Pet, Progression (Poker TD), Sandbox (Brick Panic). Aim: Excellent. Deadline about 2026-12-10 (teacher, 2026-11-24). Rubric checklist: 00_Course/Rubric/Rubric_Checklist.md.

## Current status
- Virtual Pet: finished as it is (teacher, 2026-11-24). No sack shell: in the video, explain the base shape (one circle + two triangles).
- Poker TD: core done, paused for now (Wong, 2026-11-24). Left: step 6 (CSV log + Excel graph), Excel model update, content plan, balance - Wong picks later.
- Brick Panic: endless loop + real uGUI HUD (popup shop), toy palette, ink sky, island build-in, brick-break signature effect, juice (shake, hit stop, slow-mo, sky tint, emotes, wear cracks), person types, island 64 cells. Self-tested by Claude; Wong must review. Missing: balance, special blocks, playtests.

## Next steps
1. Wong plays the two new disasters (tornado, lightning) and judges them; lightning may be too soft at L1 (aimAtExposed / personDamage).
2. Wong plays a few runs and judges balance pass 1 (numbers below are first guesses). Watch: is the volcano now too hard (test: 10 of 17 open-ground people dead in 10 s at level 2)? Do shelters feel worth it (quake test: open 3/7 alive, sheltered 4/7, but the test shelter collapsed)? Is reward 40 + 8 per extra too much now that bricks are cheap?
2. Possible follow-ups (not decided): people walk to the nearest shelter when a quake/volcano starts; a "NO ROOF" marker over exposed people in the build phase; health per person type in the tooltip.
3. Update the plan (03_Sandbox/1_Plan/BrickPanic_Plan_v1.md): tsunami, round loop, UI, tools, person types, juice, shelter rule + cracks + health (sources: Adams 2014 dominant strategy; Jonasson & Purho 2012; Eiserloh 2016).
4. Special blocks (plan 4.3): anchor, glue, buoy.
5. Playtest 1 + 2 (juice ON / OFF via BPJuice.juiceOn), WebGL build + itch.io.
6. Poker TD: Wong picks what is still needed (step 6 CSV + graph, Excel update, content plan, balance).
7. Write the 3 reports; videos last.

## Fixed - volcano wear bug (2026-11-25)
- Wong saw joint wear grow during the volcano with no bomb hits. Cause: Eruption set DisasterState.Intensity but never WearScale = 0, so PhysicsActivator wore every joint on the island like an earthquake (plastic ~20% per second at full strength). Fix: Eruption sets WearScale 0 on Begin and 1 at the end / OnDisable (as Tornado, Lightning already do). Test: plastic wall far from the crater, full strength: 0% wear. Volcano now hurts bricks only by hits, explosions, fire and impacts, so it may feel weaker on buildings.

## Done - Brick Panic new disasters (2026-11-25)
- Wong: core is done, now extend. Options given (tornado, flood, lightning, wildfire); Wong chose Tornado + Lightning storm. Each tests a property no other disaster used (meaningful play, Salen & Zimmerman, 2004).
- Tornado (Disasters/Tornado.cs): crosses the island from a forecast side (same arrow as the tsunami) in 26 s, radius 7 m; swirl + pull + lift forces (newton, so MASS decides: plastic flies, steel stays) and local joint wear; people pushed (x1.8) and hurt in the core unless roofed; funnel made of spinning bricks. Test L1: plastic wall 2/6 left, wood/stone/steel 6/6, 8/8 people alive.
- Lightning storm (Disasters/LightningStorm.cs): a strike every 0.8-1.6 s on the highest thing within 6 m of a spot (40% of spots near people with no roof); steel counts 4 m higher and grounds the strike safely (lightning rod); others take wear, wood burns; a direct hit does 75 damage, side flash 25 within 2.5 m; 0.7 s blue glow warns before each strike; rain and sun flash (BPJuice.Flash). Test L1: 9/10 alive, 3 people hurt, steel rod and wood hut stood.
- Effects (Wong: polish the two new ones): tornado - dark brick cloud cap turning on top of the funnel, wind streaks, grass/dirt chips and dust at the base, chips when a brick is torn loose. Lightning - storm cloud of big dark bricks over the island (comes down, glows from inside near each strike), forked bolt with a re-strike flicker, glowing burn mark that cools and fades, screen flash (BPJuice.ScreenFlash, white Image made on the HUD at runtime), thunder rumble 0.35 s after a strike, darker sky (x2.6 tint).
- Both are in the RoundManager pool (5 disasters, never the same twice). HUD: icons + colours in BP_UITheme; forecast hints "HEAVY BRICKS STAY!" / "STEEL DRAWS LIGHTNING!"; sky tints. Added by "Apply Look" (the HUD is not rebuilt).

## Done - Brick Panic volcano fix (2026-11-25)
- Wong: the volcano must throw bombs from the crater (no drops from the sky). ShelterDebris now only works for the earthquake. Eruption.aimAtExposed (0.5): half the bombs are aimed at people with no roof (where they will be when it lands, 0.8 m miss); the rest land at random.
- Bug found: most bombs blew up in the crater (big 3.2 m bombs touched the 4 m crater walls and each other at launch). Fixed: bombs ignore each other and start above the rim. Bombs in the air at once: 4 -> 19.
- Small bombs now blast too (0.55 x radius, no fire, never kill at once); explosions do up to 70 damage (explosionDamage) instead of only a push.
- Test: volcano L1, 12 people in the open: 6 dead (lava), the others hurt.

## Done - Brick Panic Lego stiffness (2026-11-25)
- Wong: quake cracks removed (falling rocks are enough). QuakeFissures.cs stays in the project, not in the scene.
- Problem (Wong): a roof without a middle pillar sagged and fell under gravity alone; real Lego holds with 50% overlap. SagTest (Scripts/Debug/SagTest.cs: 7 m plastic cantilever + 8 m wood bridge, no disaster): before = both fell, 14 joints broken, 3.4 m drop.
- Cause: FixedJoint chains are soft in PhysX's iterative solver, and the gravity load passed the joint break force. With projection on, PhysX reports far too big forces in stacked walls (14000 N on a still plastic cantilever), so force-based breaking cannot be used.
- Fix (PhysicsActivator): ConfigurableJoint, all motion locked, projection on (0.01 m / 0.5 deg); 20 solver + 4 velocity iterations; joints no longer break from PhysX forces (useForceBreaking off) - only by wear (x2 wearScale, as the 1-3x load speed-up is gone) and by impacts (Block.OnCollisionEnter -> Impact: 0.02 s wear per N s above 3 m/s); bricks on the island clip to it like a baseplate (anchorToGround, joint to the island body, wears too). After: cantilever 0.48 m tip drop, bridge 0.02 m, 0 joints broken.
- Check: quake L1 still breaks things (81 joints, plastic cantilever lost, wood bridge stood); tsunami still wipes the area (all 130 bricks in its path).

## Done - Brick Panic balance pass 1 (2026-11-25, Wong's list)
- Prices per 2x4 brick: plastic 0.5, wood 1.5, stone 3, steel 5 (pricePerStud = that / 8, set by "Apply Look"). Money and prices show one decimal (BrickPanicHud.Money). Sell refund rounds to 0.1.
- People stand still in the build phase (Person.Calm, set by RoundManager in Build / Result / Game over).
- Settle: wait at least 8 s and until no brick moves faster than 0.8 m/s and nobody is tumbling, at most 12 s (a person was hit after the count before).
- Health: Person.Damage / Heal, adult 100, kid and elder 70, athlete 130. Brick hits: (impulse - 20) x 0.5 damage, head x2; landings above 5 m/s: 20 per m/s. Survivors heal 50% of missing HP after a round (Wong chose partial heal). PersonHealthBars: bar over every person in a disaster or when hurt.
- Shelter rule (Wong chose A): ShelterDebris drops real falling bricks on people with no roof during the quake (rocks, 35 dmg, 0.6 per s at full strength). (Volcano sky drops removed later: see volcano fix.) Reason: "build nothing" was a dominant strategy (Adams, 2014). Roofed people huddle and fear still bricks above them much less. Forecast line says "BUILD ROOFS!" for quake and volcano.
- (Removed later the same day) Ground hazard (Wong chose B): QuakeFissures opened 4 (+1 per level) zig-zag cracks in open ground at 35% quake strength; standing on one: 35 damage/s and falls; bricks on it wear 3 s/s; cracks close with a brick break.
- Volcano: 4.5 bombs/s (was 2), big chance 0.5 (0.35), small 1.8 (1.5), big 3.2 (2.5), explosion radius 3.6 (3).
- Tests: quake L1 with 7 open + 7 sheltered: 7 of 14 dead (before: 1 of 14). Volcano L2 with 17 people in the open: game over.

## Done - Brick Panic tools (2026-11-25)
- Wong approved the UI, popups and transitions. Result card: ROUND X big heading + SURVIVED! smaller heading; banner COUNTING (no dots). Wong edited sizes/positions by hand in the scene: do NOT run "Build HUD In Scene" (it overwrites them); use "Brick Panic/Add Tool Panel (keeps your HUD edits)" style menus.
- Tool panel middle right (dark panel, hammer / eraser squares, chosen = green, keys V / C, hover tooltips). BuildController.Tool: BUILD click = one brick, drag = a line along X or Z (max 40, stacks on what is under it, bricks drop in one after the other); ERASE click = one brick/person, drag = rectangle on the ground plane, everything inside removed (max 250, half price back, people full price). Undo takes back a whole line.
- Tsunami forecast arrow (Fx/TsunamiArrow on Systems): white chevrons over the coast on the side the wave comes from, flowing in the wave direction; only in the build phase.

## Done - Brick Panic polish (2026-11-24/25, Wong's list)
- UI (uGUI in the scene, menu "Brick Panic/Build HUD In Scene", safe to run twice): top left round + goal; top right money / alive / joint wear pills; forecast card (icon, name, one line) + START + one red "NEED X MORE PEOPLE" line; bottom left SHOP button (B) + current tool chip; SHOP pops up 4 materials + person, a material pops up its 8 sizes (in that material's colour), a size closes it (Wong 2026-11-25: popups, not every card on screen); "?" pops up the controls; live counter during a disaster; result card counts up; game over drops in. Every label auto-sizes inside its own box with margins. Tooltips on all cards (price, durability, joint strength, weight, floats/burns).
- UI parts: Scripts/UI/BrickPanicHud, BPCard, BPButton, BPTooltip, BPTween (DOTween UI module is not visible to the asmdef), BPTheme (Data/BP_UITheme.asset: fonts + 9-slices from Poker TD, icons). Art: Art/UI (made by progress/patches/make_bp_ui_art.py), Art/UI/Renders (brick pictures rendered by the builder).
- Person types (random on placing, weights 25/40/20/15 in BuildController): kid, adult, elder, athlete (Person.SetType). Label pops over the new person.
- Look (menu "Brick Panic/Apply Look (Palette + Sky)"): plastic red, wood yellow, stone blue, steel dark metal; brighter ground; brown tree trunks; ink sky shader Shaders/BP_InkSky (3D noise, wraps around the fly camera); trilight ambient.
- Island 64 cells (was 48), volcano radius 8.5, map-piece chances x0.75; camera start (0, 42, -62). MapBuildIn: island columns fly up centre-first (GPU instanced), then map pieces drop in; building blocked meanwhile.
- Brick break = signature effect (BrickFx.Break: flash, chunks, chips, dust ring): rubble breaks one by one (highest first), sell, undo, burn. BrickFx.Snap on joint breaks and hard hits (max 8 per frame). Placed bricks drop in with a squash + dust.
- Juice (Fx/BPJuice on Systems, master switch juiceOn): trauma shake (Eiserloh 2016), hit stop on big collapses, slow-mo when survivors drop below the goal and when a disaster ends (fixedDeltaTime scales too), sky + sun tint per disaster. PersonEmotes (quads; SpriteRenderer does not draw here), WearCracks (crack texture, cutoff = wear).

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
- Brick Panic UI: SpriteRenderer is invisible in this URP 3D setup (use a quad + M_BP_Emote). Unity's Quaternion == treats tiny rotations as equal: track shake with a bool, not lastRot != identity (camera drifted). RenderMeshInstanced needs RenderParams.worldBounds set, else nothing draws. New sprite textures need spriteImportMode = Single or LoadAssetAtPath<Sprite> is null. DOTween UI shortcuts (DOAnchorPos, DOFade) are not visible to asmdefs: use BPTween.
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
- assets/bp_tornado.png, bp_lightning.png - (no text: view the original) new disasters in play.
- assets/bp_volcano_v2.png - (no text: view the original) volcano with bombs thrown from the crater (19 in the air).
- assets/bp_sag_before.png, bp_sag_after.png - (no text: view the original) SagTest before (collapsed) and after (Lego-stiff) the joint fix.
- assets/bp_balance_quake.png, bp_balance_volcano.png - (no text: view the original) balance pass 1 tests (health bars, falling rocks, volcano).
- assets/bp_bpui2_2.png, bp_bpui2_3.png, bp_bpui2_5.png - (no text: view the original) new HUD: build view, shop popup, size popup (2026-11-25).
- assets/bp_bpin_5.png, bp_bpjuice_5.png, bp_bpjuice_6.png, bp_bpui_7.png, bp_bpui_5.png - (no text: view the original) island build-in, quake with emotes and cracks, round 2 build view, result card, game over.
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
- progress/backup/ - old script versions (tsunami_2026-11-24/, roundloop_2026-11-24/, ui_2026-11-24/) and the full PROGRESS history.
