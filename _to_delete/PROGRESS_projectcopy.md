# PROGRESS - Game Systems (3 solo Unity games)
Last updated: 2026-11-25 04:10 (Brick Panic: core done, 5 disasters, ready for gameplay tuning)
Full history: backup/PROGRESS_full_2026-11-25_disasters.md (all details of 2026-11-24/25), older: backup/PROGRESS_full_2026-11-2*.md
(This is a copy of GameSystem2/progress/PROGRESS.md on Wong's computer; the file on the computer is the master.)

## Goal
Three solo games in one Unity project (GameSystem2): Virtual Pet, Progression (Poker TD), Sandbox (Brick Panic). Aim: Excellent. Deadline about 2026-12-10 (teacher, 2026-11-24). Rubric checklist: 00_Course/Rubric/Rubric_Checklist.md.

## Current status
- Virtual Pet: finished as it is (teacher, 2026-11-24). In the video, explain the base shape (one circle + two triangles).
- Poker TD: core done, paused (Wong, 2026-11-24). Left: step 6 (CSV log + Excel graph), Excel model update, content plan, balance - Wong picks later.
- Brick Panic: core done and approved by Wong (2026-11-25): endless round loop, popup-shop HUD, build/erase tools with line build, toy palette, juice, health + shelter rule, Lego-stiff joints, 5 disasters (quake, tsunami, volcano, tornado, lightning) with effects. Next: Wong's gameplay tuning plan, then plan doc, playtests, build.

## Next steps
1. Wong plays and lists gameplay tuning (he will plan it). Known points: lightning may be soft at L1 (aimAtExposed / personDamage); volcano hurts buildings less since the wear bug fix (hitDamage / explosionRadius); is reward 40 + 8 per extra too much with cheap bricks?
2. Update the plan (03_Sandbox/1_Plan/BrickPanic_Plan_v1.md) to the game as built: round loop, UI flow, tools, prices, person types, health + shelter rule, Lego joints, 5 disasters, juice. Sources: Adams 2014 (dominant strategy), Salen & Zimmerman 2004 (meaningful play), Jonasson & Purho 2012, Eiserloh 2016.
3. Special blocks (plan 4.3): anchor, glue, buoy (optional, Wong decides).
4. Playtest 1 + 2 (juice ON / OFF via Systems > BPJuice.juiceOn), WebGL build + itch.io.
5. Poker TD: Wong picks what is still needed.
6. Write the 3 reports; videos last.

## Done - Brick Panic (2026-11-24/25, short; details in the full history)
- Round loop: forecast -> build -> disaster -> settle (min 8 s, until still, max 12 s) -> count -> reward 40 + 8 per extra or game over; goal +1 per round; wear and standing bricks carry over; rubble breaks apart.
- HUD (uGUI in the scene, Wong edited it by hand): popup shop (SHOP/B -> material or person -> size), tool chip, forecast card (+ side arrow for tsunami/tornado, one hint line), stats pills, live counter, result card (ROUND X + SURVIVED!), game over, help "?", tool panel (V build / C erase).
- Building: click = 1 brick, drag = line (max 40, stacks); erase drag = area (half price back). Prices per 2x4: plastic 0.5, wood 1.5, stone 3, steel 5; money with 1 decimal. Person (random kid/adult/elder/athlete).
- People: stand still in build phase; health (adult 100, kid/elder 70, athlete 130), bars, 50% heal between rounds; emotes.
- Physics: ConfigurableJoints, locked + projection, 20 solver iterations; no breaking by PhysX force, only wear and impacts; bricks clip to the island like a baseplate. Roofs and bridges hold like Lego.
- Disasters: quake (island-wide wear, falling rocks on people with no roof); tsunami (balanced, unchanged); volcano (bombs from the crater, half aimed at exposed people, no island-wide wear); tornado (mass test, crosses from a forecast side); lightning (highest point, steel = lightning rod).
- Look/juice: toy palette, ink sky, island build-in, brick break signature effect, shake, hit stop, slow-mo, sky tint, wear cracks, tornado cloud cap + wind + dirt, storm cloud + forked bolt + burn marks + screen flash + thunder.

## Key decisions (still in force)
- Brick Panic core rule: every disaster can destroy every material; a stronger material only buys time. Endless; wear carries over.
- "Build nothing" must never be the best plan (dominant strategy, Adams 2014): shelter rule for quake, aimed bombs for volcano, aimed strikes for lightning.
- Each disaster tests a different build property (meaningful play): quake = roofs + joints, tsunami = walls + side, volcano = roofs + distance + no-burn material, tornado = mass, lightning = material + height.
- Do NOT run "Brick Panic/Build HUD In Scene" (overwrites Wong's hand edits). Use "Apply Look (Palette + Sky)" and "Add Tool Panel (keeps your HUD edits)"; both are safe to run twice.
- Tsunami = per-brick drive, gravity always on, spray at the edge (Wong: most balanced, do not change).
- Quake cracks removed (QuakeFissures.cs kept, not in the scene). No sky drops for the volcano.
- Brick Panic: floating island 64 cells + free fly camera; volcano always on the map; all effects made of Lego bricks (BrickFx); physics steering, not NavMesh.
- Dam Rush is dropped (teacher, 2026-11-24).
- Poker TD (Wong, 2026-11-24): Balatro feel but not a copy; info as 3D world UI; all UI in the scene; dissolve = signature effect.
- Virtual Pet: keep Wong's hand-made lighting (do NOT run "Wind Pet/Build Lab In Scene").
- One .asmdef per game. Code rule: every public / Inspector field gets a [Tooltip] with what it does, the unit and the default.

## Didn't work (do not repeat)
- Brick Panic joints: FixedJoint chains sag and break under gravity; force-based breaking with projection on (PhysX reports ~14x too big forces). Use ConfigurableJoint + wear/impact breaking.
- Volcano: sky-dropped ash (Wong: bombs must come from the crater); bombs bigger than the crater blew up at launch (start above the rim, ignore each other); forgetting WearScale = 0 wore the whole island.
- Brick Panic UI: SpriteRenderer is invisible here (use a quad); Quaternion == hides tiny rotations (track shake with a bool); RenderMeshInstanced needs worldBounds; new sprites need spriteImportMode Single; DOTween UI shortcuts are not visible to asmdefs (use BPTween).
- Tsunami: dam break (too weak), one start speed (stuck on ground steps), buoyancy (bricks float in the air).
- Animator MUST be on Model/Root, never on the Rigidbody root. Kenney 2x2 brick is 0.99 m (scale 1/0.99).
- Super Chips font: broken kerning for '+' and '%' (removed 134 pairs, backup: backup/font_2026-11-24/).
- Poker TD: camera edge panning; wooden 3D signboards; UI built only at runtime; X/Y rotation on an Overlay canvas.
- device_commit_files can write an OLD copy: use a new staged name and check md5, or edit on the device with python.
- Compiling while Wong is in Play mode wipes the session: check isPlaying; save Wong's scene before compiling. Deleting from the device shell is blocked: use _to_delete/.

## Open questions
- Poker TD: two machines with the same ring number - which goes first? Click sound: later (Wong).
- Block.JointsBroken can count a joint twice (stat only).

## Assets
- assets/bp_tornado.png, bp_lightning.png, bp_volcano_v2.png - (no text: view the original) new disasters and the fixed volcano.
- assets/bp_sag_before.png, bp_sag_after.png - (no text: view the original) Lego-stiffness test before / after.
- assets/bp_balance_quake.png, bp_balance_volcano.png - (no text: view the original) health bars, shelter rule tests.
- assets/bp_bpui2_2.png, bp_bpui2_3.png, bp_bpui2_5.png, bp_bpui_5.png, bp_bpui_7.png - (no text: view the original) HUD: build view, shop popup, size popup, game over, result.
- assets/bp_bpin_5.png, bp_bpjuice_5.png, bp_bpjuice_6.png - (no text: view the original) island build-in, quake with emotes and cracks.
- assets/BrickPanic_Plan_v1.md -> md/BrickPanic_Plan_v1.md.md - Brick Panic plan v1 (OUT OF DATE: see Next steps 2).
- assets/bp_*.png (older), Poker TD pngs (tip_*, radar1, trans_*, ring_*, s_*, r2-r5_*, juice_*, ptd_*) - (no text: view the original) earlier steps.
- assets/PokerTD_Model.xlsx -> md/PokerTD_Model.xlsx.md - Poker TD economy and damage model.
- assets/PokerTD_Reference.xlsx -> md/PokerTD_Reference.xlsx.md - Balatro + Kingdom Rush analysis, APA.
- assets/WindPet_Balance.xlsx -> md/WindPet_Balance.xlsx.md - Virtual Pet bars.
- assets/Sandbox_Plan_v2-v4.md, Research_Beaver_AI_and_Water.md (+ md) - dropped Dam Rush (reference only).

## Other key files
- Assets/BrickPanic/ (namespace BrickPanic, own asmdef + Editor asmdef). Scene: Scenes/BrickPanic_Main.unity. Menus: "Apply Look (Palette + Sky)" (palette, prices, sky, juice/disaster components), "Add Tool Panel", "Build HUD In Scene" (DON'T), "Build Main Scene" (rebuilds everything, DON'T unless asked).
- Scripts: Game/RoundManager; Build/ (Block, BlockFactory, BuildController); Physics/PhysicsActivator; People/Person; Disasters/ (Earthquake, Tsunami + WaveBlock, Eruption + LavaBomb, Tornado, LightningStorm, ShelterDebris + FallingDebris, QuakeFissures unused); Fx/ (BrickFx, BPJuice, MapBuildIn, PersonEmotes, PersonHealthBars, WearCracks, TsunamiArrow); UI/ (BrickPanicHud, BPCard, BPButton, BPTooltip, BPHoverTip, BPTween, BPTheme); Debug/SagTest.
- Tuning lives on Systems (PhysicsActivator, RoundManager, BuildController, each disaster, BPJuice) and on Data/Materials (MAT_*), Prefabs/Person.prefab.
- Art: Art/UI (+ Icons, Emotes, Renders) made by progress/patches/make_bp_ui_art.py; Shaders/BP_InkSky.
- Assets/Progression/ - Poker TD; Assets/VirtualPet/ - Virtual Pet; Assets/Sandbox/ - Dam Rush (dropped).
- progress/backup/ - old script versions (tsunami_, roundloop_, ui_, balance_2026-11-2*) and the full PROGRESS history.
