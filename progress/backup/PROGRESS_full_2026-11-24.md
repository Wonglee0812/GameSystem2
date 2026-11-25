# PROGRESS - Game Systems (3 solo Unity games)
Last updated: 2026-11-14 01:05 (Sandbox: new HUD approved by Wong; random smooth map + water B ready; Wong playtests next)

## Goal
Three solo games in one Unity project (GameSystem2): Virtual Pet, Progression, Sandbox. Aim: Excellent on all three, finished before Christmas 2026. Rubric checklist: 00_Course/Rubric/Rubric_Checklist.md.
Order of work (Wong, 2026-11-12): finish Poker TD -> build Sandbox -> skeletons of all 3 done -> write the 3 reports -> polish all 3 -> videos last.

## Current status
- Virtual Pet (wind pet): about 80%, playable. Parked until the polish phase.
- Progression (Poker TD): core done (steps 1-5 of 6). Left: step 6 (CSV log + strategy graph), Excel model update, content plan, polish, theme.
- Sandbox "Dam Rush": playtest-2 fixes done and tested by Wong ("ok for now"). Plan v2 approved (03_Sandbox/1_Plan/Sandbox_Plan_v2.md). Phase A code is DONE: both patches applied, Unity compiles with 0 errors, MainMap rebuilt at 128 x 96 m, a short Play test ran without errors. 5x speed fixed (~33 fps). Phase B code DONE (Dam Mk2, Spillway, Base Mk2 = pump base, Food farm, Dig, Fell trees). Phase C code DONE (2 labs, tech tree with research time, water wheel, 5 machines, operators, orders + automation, new materials). All compile; each feature checked in Play mode by script. Wong played A+B+C up to autumn year 1 (2026-11-13) and listed problems; fix round 1 is done (see Done). Plan v3: 03_Sandbox/1_Plan/Sandbox_Plan_v3.md.

## Next steps
0. Wong playtests the new random map + water + new HUD in MainMap (HUD check: text size on his screen, height of the tile bar, icons that are hard to read) (MapRandomizer on the Map object: seed 0 = random; the seed is in the Console as "Dam Rush map seed"; type it into MapRandomizer.seed to replay a map). Also (B: 0.5 m cells, Burst, smooth surface, clear water). Check: dams/levees still hold, pumps, wheels, sweep risk, farm calm-water check, flood look (muddy), ice in winter, 5x speed. Look knobs on M_DR_StylizedWater: Clarity, AbsorbRGB, Caustics, ClearLines. Then mudbag + sandbag.
0b. Later: water S (Sandcastle-style, GPU) for a second WebGPU build; pack Assets/Sandbox/WaterCompare as a reusable package (Wong, 2026-11-14: keep the WaterCompare scene, do not delete).
1. Wong tests flood v2 + defences (plan v4 section 10): forecast on spring day 1, a week of waves; build an Alarm bell (Build > Tools) and ring/all-clear it from its pop-up; Levee, Floodgate (open/close in the pop-up), Sandbags (Build > Water, or the dam pop-up; needs M4 + sand). F = short test flood (1 day of waves). Tune: sweep risk (6/12 died in one wave without the bell), pressure wear, gift amounts.
2. Discuss enemies, weapons and an army with Wong (Wong: the game feels like "a garden in the clouds", nothing is dangerous). Wolves were Phase D stretch; this may grow into a bigger system - plan v4.
3. Wong tests fix round 1 (click a building = pop-up; build categories; machines call beavers; 2-row top bar; water look; family cap 30).
4. Phase A: Wong playtests one full year at the fastest working speed; balance food (winter need vs forage rate); check ice, the "FROZEN IN" banner, death by hunger and the game over screen. First Play test (spring, day 2, 2026-11-13): 12 beavers, 2 foragers picking water plants, food 32, no errors; wood storage full (40), so the wood cutters stand idle. One year at 5x takes about 32 real minutes (160 game minutes / 5).
5. Later (after the playtests): cap the top speed at 2x-3x for players (option c) - Wong, 2026-11-13.
6. Phase D (stretch): wolves that ambush beavers far from water.
7. Season rewards: Wong decides later.
8. Sandbox playtest cycles + screenshots of player builds; player-experience statement is in Plan v2, refine it after each cycle.
9. Poker TD step 6: CSV log per run + Excel graph comparing strategies (Excellent criterion 1).
10. Update PokerTD_Model.xlsx with all rule changes; fix Run Sim (balanced strategy always wins).
11. Poker TD content plan (items, machines + rarity, hand upgrades), balance, polish.
12. Virtual Pet: test with the real mouse; add WorkConfig numbers to WindPet_Balance.xlsx.
13. Write the 3 design reports; then polish (Virtual Pet sack shell, Animator, WebGL builds, playtest forms); videos last.

## Phase A - what is done and what is not (2026-11-13)
Done (written to disk, verified):
- Scripts/Colony/ItemInfo.cs (new): item names, colours, StorageTypes (food, wood, mud, stone).
- Beaver.cs: Item enum + Food, Branches, PressedWood, Gravel, Sand, Cement; WorkGroup + Forage.
- BuildingSite.cs: storage holds ONE type (storeType, stock); title "<Type> storage".
- BuildOrders.cs: storageType for the next storage, capacity 40, piles drawn per type, removal drops the stock, starter storage takes (type, amount).
- ColonyManager.cs (first part only): starter storages food 30 / wood 12 / mud 8 / stone 6 on separate bases; Capacity/StoreIn/NearestStorageWithSpace per type.
- Scripts/Colony/Seasons.cs (new): 4 seasons x 10 days, endless years, ice (max 0.5 m, margin 0.2 m), spring floods (2 per spring, x5 in year 1, +35% per year), winter flow x0.5, goal text per season, food needed for winter.
- MapController.cs: seasonFlow, StartFlood(strength, reason).
- 03_Sandbox/1_Plan/Sandbox_Plan_v2.md (English, APA).
Applied 2026-11-13 (patch scripts; both were run twice to check they are safe):
- phaseA_colony.py -> ColonyManager: foragers group, starveDays, litterFood, Awake adds Seasons, LodgeWaterDepth, LodgeFrozenIn, ReachableNow/NearestFood, Die + GameOver, PickForageSpot.
- phaseA_patch.py -> Beaver (eat from food storage, tree bark = half meal, forage, health + death, winter: stay in lodge, eat only under the ice), GameSpeed 5x, HUD (season line, all stock chips, goal box, storage type buttons, Foragers row, health bar, game over + Play again, FROZEN IN banner), ice in DR_StylizedWater.shader, MainMapBuilder 128 x 96 (points x 4/3) + Seasons component.

## Done
- 2026-11-14 HUD restyle after Wong's references (Sandcastle, Bubblebird Studio; a hex bee builder with a dark research card). ColonyHud.cs rewritten, same functions: top left = white pills (icon + amount, hover = name/room/piles); top centre = season, day, clock with sun/moon and water state as white text with shadow, banners and toasts under it; top right = dark rounded speed buttons + family pill + foldable season goal card; bottom centre = build bar (4 category tabs with icons + fold, a dark tray of cream tool tiles with icon, name, cost, hotkey, locked = faded + "research X") and a help card for the chosen tool (storage type picker with icons, repair all); bottom left = round buttons Work, Family, Orders (count), Events, Help (controls + map seed) that open dark cards with a gold outline; building pop-up on the right; HUD tooltips. New HudIcons.cs paints ~35 flat icons in code (SDF shapes with an ink outline); a PNG in Resources/HudIcons/<Name>.png replaces a painted icon. Palette: cream tiles, charcoal cards, gold line, orange accent. Backup: progress/backup/ColonyHud_2026-11-14.cs.txt. Screenshots: progress/assets/ui_v2_1.png, ui_v2_2.png.
- 2026-11-14 Random smooth map: new MapRandomizer.cs (execution order -1000) on the Map object makes a new map each Play: ForestTerrain.Randomize(seed) = main stream as two sine meanders + 3 side streams (quadratic Bezier + wobble, joins spread along the main stream, >= 20 m apart on one edge), points every 2 m; ground rebuilt; 4 water sources moved to the stream heads; forest replanted from the old tree kinds (~640 trees); lodge spot at 47% of the main stream; camera starts there. Smoother ground: no small bumps (bumpHeight 0), wider gentle hills, U-shaped banks over bankWidth 2.5 m, terrain mesh every 0.5 m (GroundHeights.meshSubdivide = 2), mud colour only near streams. Smooth shoreline: dry water points next to water take the neighbours' water level, so the ground cuts the water along its slope; the shader fades at depth 0. MainMap scene got the component (not rebuilt); MainMapBuilder adds it too. Tested 2 seeds (527117, 865194): no errors, ~41-48 fps at 1x. Screenshots: progress/assets/map_random_2.png, map_random_close.png. Backups: progress/backup/ForestTerrain_2026-11-14.cs.txt, GroundHeights_2026-11-14.cs.txt.
- 2026-11-14 Water B in the game: WaterGrid.cs rewritten with the same public API. Own resolution (subdivide = 2 -> 256 x 192 cells of 0.5 m on the 128 x 96 m map; ground from GroundHeights.GroundAt), pipe flows + depths + velocity in Burst IJobParallelFor jobs, surface mesh (heights, colours, foam, normals) in Burst jobs, surface blends between the last two steps. Steps: 25/s at 1x (0.04 s), up to 0.2 s at fast speed. Measured in the Editor: step 3.7 ms (old 1 m CPU: 11 ms for 4x fewer cells), mesh 2 ms, 1x ~54 fps (59 with water paused), 5x ~43 fps. DR_StylizedWater.shader: clear water (refraction, per-colour absorption, muddy in floods via _DR_Flood), caustics, fewer cell lines (_ClearLines). DamRush.asmdef now references Burst, Collections, Mathematics. Backups: progress/backup/WaterGrid_1m_cpu_2026-11-14.cs.txt, DR_StylizedWater_2026-11-14.shader.txt. Screenshot: progress/assets/B_main_clear_day3.png.
- 2026-11-14 Water compare v3: C stutter fixed (GPU state kept for the step before + blended with Alpha in the vertex shader; new base class GpuWaterSim). Lane D (0.125 m) replaced by lane S = "Sandcastle-style": shallow water equations with momentum on a staggered grid, semi-Lagrangian advection (Chentanez & Mueller, 2010; Stam, 1999) in SweWater.compute + SimSwe.cs, 0.25 m like C. Shader: caustics (Worley cell lines), foam on rising wave fronts. Tools: Boat (floats, drifts with the current, tilts, runs aground, wake; Boat.cs; GPU lanes read the water back with AsyncGPUReadback), Block hold-to-spam (1.2 m blocks, only the cells under a block are updated), water level +/-0.15 m (PgUp/PgDn), wave maker = paddle push for 0.35 s every 3 s. Push is now bounded (pulls flow to a target; before it pumped water and raised the reservoir +1 m). 81 fps with 4 lanes, 8 boats, 12 blocks.
- 2026-11-13 Water compare v2: mouse tools (Push drag, Wave click, Block drop; mirror to all lanes), wave train (G), hide panel (H), lane D = GPU 0.125 m cells, clear water in CompareWater.shader (refraction from the URP opaque texture + per-colour absorption, _Clarity 0-1). 4 lanes: 91 fps at 1x, 81 fps at 5x. Source check: no public source says how Sandcastle simulates water on the GPU or its grid size; the 80.lv interview (Weibel) only says custom sim from papers, water and sand at two resolutions. The "whole simulation on the GPU" line came from the Steam page of SandFlow, a different game.
- 2026-11-13: Water compare demo (3 lanes, same valley + breachable wall). Measured on Quadro T2000: 1x 70-90 fps all lanes; 5x + flood 42 fps. Per frame: A sim 0.7-2.3 ms + mesh 1.7 ms; B sim 0.3-1.6 ms + mesh 2 ms; C CPU 0.03-0.07 ms. Look: A blocky 1 m edges; B smooth; C sharpest flow detail.
- 2026-11-13 night, flood v2 + flood defences (new files FloodSystem.cs, BuildOrders.PhaseD.cs; edits in MapController, Seasons, DamSite, BuildingSite, BuildOrders(.PhaseB), Industry, ColonyManager, ColonyHud; plan v4 section 10):
  - Flood: forecast 1 day before (banner, red outlines on buildings that may flood), 0.5 day rise, 6 days of waves (period 1 day, low = 45% of the top), 0.5 day fall; one flood per spring (Seasons.floodForecastDay = 1); no automatic alarm; F = short test flood.
  - Damage: beavers in water >= 0.3 m and >= 1.2 m/s can be swept away (kit 4%/s, yearling 1.5%/s, adult 0.4%/s x speed/1.2); flooded buildings: storage loses stock (food 1/6 s, other 1/20 s), machines/labs/nurseries stop; pressure wear in floods = 0.04/s per m of head above 0.4 x dam height (full pond = high break risk).
  - Answers: Alarm bell (ring / all clear in the pop-up; AlarmActive also when the bell rings; starving beavers still come out), Levee (3 mud, 0.8 m), Floodgate (1.6 m; open = 0.15 m sill, no wear), Sandbags (2 sand via the repair mechanism, +0.3 m, -30% wear, wash out 2 days after the flood; locked behind M4).
  - Gifts: each wave that falls leaves 3 + 0.6 x strength piles (duckweed/food 40%, mud 35%, driftwood/wood 25%) on shallow banks within 35 m of the lodge.
  - Script test: no bell -> 6/12 swept away in one wave, levee broke, food storage flooded, gifts 12 food + 4 wood; bell rung -> all in the lodge, no more deaths. Fixed after the test: flooded state flickered (hysteresis + 1 message per 2 min), bell counted as flooded.
- 2026-11-13 evening, fix round 1 after Wong's playtest (new file BuildOrders.Select.cs; edits in Beaver, Beaver.PhaseC, Industry, BuildingSite, BuildOrders, ColonyManager(.PhaseB), ColonyHud, WaterGrid, DR_StylizedWater.shader):
  - Starving with food in storage: (1) a hungry beaver carrying a load while storages were full never ate -> now it drops the load as a pile and eats; (2) beavers hiding from the flood alarm or sleeping now come out when starving (food < 5).
  - Fell trees on/next to buildings: allowed (tool and cutters no longer skip blocked hexes).
  - Clicks on tall bases/dams picked the hex behind them (no collider) -> every site visual has a BoxCollider + SitePick; right-click/select now hit the building you see. Right-click on base+building removes the building first (tested).
  - Family cap: maxFamily 30 (nurseries stop; HUD shows Family n/30) - Wong's playtest had 71-77 beavers and lag.
  - Machines: no Operators group; a machine with an order calls an idle worker (else a random free one), the beaver loads up to 3 batches per trip, runs them, carries the products home, then goes back. Wheel: 1x at 0.5 m/s (was 1.0), max 3x (was 2x). Test: splitter made 6 batches (12 food) in ~75 s real at 5x.
  - UI: top bar in 2 rows (raw / machine materials), panels moved down; Industry tab removed; click any building = pop-up (info, flag, remove, open base/building on it; storage type change; lab = its own research tree with Research buttons; machine = +1/+5/clear/auto, operator, loaded batches) with a pulsing outline; Build menu in 4 categories (Water, Buildings, Industry with one button per machine, Tools).
  - Water: shader used _Time (runs 5x faster at 5x) -> now _DR_Time = real time from WaterGrid; very fast water capped at 3 m/s for the look; new flow streaks along the current (stronger in fast water, cells fade), look inspired by Sandcastle (Bubblebird Studio). Screenshots: progress/assets/fix_popup_water.png, fix_water_close.png.
- 2026-11-13 Sandbox Phase C code (new files Industry.cs, BuildOrders.PhaseC.cs, Beaver.PhaseC.cs; hooks in BuildOrders, BuildingSite, ColonyManager, Beaver, ColonyHud, Site, DamSite, ItemInfo):
  - Data-driven (Industry component, added to the colony at Awake): MachineDef + RecipeDef, TechNode (food, days, requires), lab/wheel BuildingDefs. Numbers = plan v3 sections 8-9.
  - BuildStage now has pressed wood, gravel, sand, cement (any stage can use them); storages can hold all 8 types (2 rows of type buttons).
  - Research: Industry tab; food paid at start, one node per lab at a time, waits if the lab is gone; B5 sets storage size to 60. Tools locked until their node is done ("locked: M1" on the button).
  - Water wheel on flowing water >= 0.3 m, >= 0.2 m/s; speed factor 0.25x-2x (1 m/s = 1x), 0 when frozen; drives 2 machines on next hexes; paddles turn.
  - Machines on a platform; operator (new group) fetches inputs, loads, runs one batch (seconds / power), carries the product home. Orders +1/+5/clear; auto after M7. Stale operator claims free after 90 s.
  - Test 2026-11-13: lab built, M1 researched in 1 day (food 30 -> 10), wheel 2.0x, splitter made 2 food from 1 wood, stopped when the water slowed (0.11 m/s). Screenshots: progress/assets/phaseC_industry_tab.png, phaseC_build_tab.png.
- 2026-11-13 Sandbox Phase B code (new files BuildOrders.PhaseB.cs, ColonyManager.PhaseB.cs; hooks in BuildOrders, Beaver, ColonyHud, DamSite, BuildingSite, WaterGrid, GroundHeights, HexGrid):
  - Dam Mk2 (7): 4 stages (costs changed in Phase C, see plan v3 section 9), 2.0 m, wears 60% slower, platform for a nursery/storage; the building is lost when the dam loses a stage (50% of it + 50% of a storage's stock as piles). Old dam is now "Dam Mk1".
  - Spillway (8): 2 stone stages, 1.0 m, never wears (wearFactor 0), no leak alarm from overflow.
  - Pump base (9): base that moves water from the lowest wet hex 2-5 hexes away into the free hexes around it, up to +0.5 m above the source, 0.6 m3/s; a nursery on it needs 0.5 m less water at order time; stops when frozen. Test: next-hex depth 0.30 -> 0.62 m.
  - Food farm (0): on calm water 0.15-1.0 m; 8 food/day spring+summer, 4 autumn, 0 winter or dry; holds 12; foragers collect from farms first (half the work time).
  - Dig (Z): each click orders -0.35 m (max 1.5 m); diggers take 3 loads per step, then terrain mesh, collider, water grid and hex heights go down; spare loads go on the bank as piles when storages are full; not on hexes with trees or orders. Test: hex dug 0.35 m.
  - Fell trees (X, drag): marked trees are felled by wood cutters first (3x cut time), 6 wood each, tree removed for good; right-click removes marks/dig orders.
  - Dam look and wear now follow the top stage's main material (sticks wash away fastest, stones slowest).
- 2026-11-13 Fast speed fix (option a): WaterGrid steps less often at speeds above 1x with a longer step (fastSpeedStepBudget = 1, maxStepSeconds = 0.2). 5x: ~2 fps -> ~33 fps; water roughness 0.045 at 5x and at 1x (stable), lodge depth the same.
- 2026-11-13 Sandbox Phase A code: patches applied (food, foragers, health + death, winter in the lodge, frozen in, game over + Play again, season HUD + goal box, storage type buttons, 5x speed, ice in the water shader, 128 x 96 map). 0 compile errors, no shader errors, map rebuilt (1116 hexes, 736 trees).
- 2026-11-13 Sandbox plan v2 approved by Wong (see Decisions 2026-11-13 "plan v2").
- 2026-11-13 Sandbox playtest 2 fixes (tested, Wong: ok): world bars above sites (build %, repair, rebuild, dam condition, nursery "next kits" or why it waits); repair tool 6/R + "fix"/"Repair all"; no alarm on dam break (auto flag + idle help); 50% drops as piles, builders take from piles, gatherers carry piles home; wood balance; economy counters (Events > debug); stylised water shader + M_DR_StylizedWater.mat.
- 2026-11-13 Sandbox playtest 1 fixes: water banks + depth-scaled pipes, dam drag tool, hover tooltip per hex, top bar stock only, GameSpeed, adults carry 2, priority flag, two-step remove; buildings (base, nursery, storage), kits grow up, tabbed HUD; research notes (03_Sandbox/2_Design/Research_Beaver_AI_and_Water.md).
- 2026-11-13 Sandbox base: water lab, main map, hex grid, colony phases 1-3. Cycle 1 "builder" version rejected (off-concept).
- 2026-11-10 to 2026-11-13 Poker TD: reference + model sheets, steps 1-5 (towers, deck, bursts, antes, buff machines in rings, combos, shop + items). Wong tested the shop: works well.
- 2026-11-10 Virtual Pet: electricity job, pickup, pachinko shop, bills, contracts, loan, clock, digestion; sack preview v3 (not approved).

## Decisions
- Water (2026-11-13): web build is the hardest limit. WebGL 2 has no compute shaders; WebGPU is experimental in Unity 6.3 (production in 6.6). So a demo scene compares A = CPU C# 1 m cells + new shader, B = Burst jobs 0.5 m cells, C = GPU compute 0.25 m cells. Wong decides after looking.
- Water (Wong, 2026-11-14): S is the best water ("tuyet voi"); C stutters with boats. Keep the WaterCompare scene and later pack it as a package. For the game now: B only (WebGL 2 safe), S later as a WebGPU version.
- Water direction (Wong, 2026-11-13): A looks worst, C best (still not as good as Sandcastle). Apply B to the game now; keep C; at the end export two builds: WebGL 2 with B water and WebGPU with optimised C water (the WebGPU one is a bonus).
- Sandbags + mudbags (2026-11-13): mudbag = early, uses mud; sandbag = better upgrade. Built after the water work. Sweep rates of flood v2 accepted as they are.
- 2026-11-13 flood v2 (Wong): a flood must hurt but always have an answer; add levee, sandbags, floodgate and an alarm bell the leader must ring (no automatic alarm); without it the flood can sweep away kits, yearlings and (less often) adults; the flood brings duckweed, mud and driftwood; it lasts about a week with constant waves and fast water; a full pond before the flood makes a dam break very likely. Numbers (risks, wear, gifts) = Claude's first version.
- 2026-11-13 fix round 1 (Wong): machines take a beaver from the family by themselves (no Operators group); each lab and machine has its own pop-up when clicked (no Industry tab); build menu split up; top bar in 2 rows; faster wheels/production; flow must be readable (reference: Sandcastle). Family cap 30 = Claude's number, tune in playtest.
- 2026-11-13 Phase C (Wong): recipes as proposed (splitter 1 wood -> 2 food, crusher 1 stone -> 2 gravel, sifter 2 mud -> 1 sand, press 3 wood -> 1 pressed wood, mixer 1 gravel + 1 sand -> 1 cement); times longer (40-100 s, a day is 240 s); research costs food AND takes time; prices higher (20-70 food); splitter right after the wheel (M2) so research can be paid with food; pressed wood + cement also for Base Mk2; cement for Dam Mk2 + Base Mk2; gravel for the spillway; sand for the food farm. Branches count as food.
- Phase B numbers are first guesses (plan v2 gave the rules, not the values): Mk2 wear x0.4 and 2.0 m; spillway 1.0 m; pump 0.6 m3/s, reach 2-5 hexes; farm 8 food/day, max 12; dig 3 loads per 0.35 m; fell 6 wood, 3x time. Tune after Wong's test.
- Fast speed: water steps less often with a longer step above 1x (option a) for testing now; cap the speed at 2x-3x later (option c) - why: Wong, 2026-11-13, tests need 5x now.
- 2026-11-13 plan v2 (Sandbox), Wong:
  - Main goal: survive the seasons, endless. 4 seasons x 10 game days (day = 240 s, 40 min per season at 1x); 5x speed for playtests. Spring = floods (stronger every year), summer = build + food, autumn = store food, winter = survive the ice. Lose when the whole family is dead. Season rewards: later.
  - Food is its own resource (wood is no longer food). Foragers group. No fish traps: beavers do not eat fish (Beavers in England, n.d.); food farm grows water plants instead.
  - Storages: one building "Storage", the leader picks the type (option b).
  - Dam Mk1 = only holds water; Dam Mk2 = stronger + a base (buildings lost when it breaks, 50% back); Spillway; Base Mk1 = as now; Base Mk2 = pumps water from DOWNSTREAM (+0.5 m around it, water is moved, not created).
  - Two labs (materials, buildings), paid with food. Machines need a water wheel AND one beaver; faster flow = faster production; Hay Day style orders or automation.
  - Sand: made from mud by a sand sifter (Wong: a machine that makes/filters sand from mud).
  - Map 128 x 96 m.
  - 2026-11-14 (Wong): HUD should look like Sandcastle and the hex bee builder references (easier to read: colours and layout). New HUD approved by Wong the same night ("UI dep qua roi").
  - 2026-11-14 (Wong): a new random map every game, smooth ground so the water runs smoothly.
- Adults carry 2 items per trip for every job, yearlings 1; hunger 0.3/s, kits eat half - why: wood was always 0 (1 cutter fed only 3 beavers). After: 4 cutters 28 wood/day vs 8 eaten/day.
- Tail-slap alarm only for floods; a dam that loses a stage gets an automatic flag - why: Wong, playtest 2.
- Repair: cost = stage items x 0.5 x wear (min 1); rebuild = full stage - why: repair and rebuild cost almost the same before.
- Washed-away stage or removed order drops 50% as piles (removed storage: all of its stock).
- Water look: shader DamRush/StylizedWater (cellular F2-F1 lines, Worley 1996; two-phase flow map, Vlachos 2010; foam; ripples around swimmers) - why: Wong's reference image.
- Sandbox concept: the player is the LEADER of a beaver family, not the builder; orders on a hex grid (pointy-top, radius 2 m, Patel 2013); beavers work by instinct.
- Dam stages like real dams: base (stones + mud), body (sticks), seal (mud) (Beaver Trust, n.d.).
- Water: heightfield + virtual pipes (Mei et al., 2007), Manning friction (de Almeida et al., 2012), displacement (Kellomaki, 2013), pressure forces (Kerner, 2015).
- Predators dropped earlier for scope; wolves only as Phase D stretch.
- Sandbox reference games: Timberborn (Mechanistry), Totally Accurate Battle Simulator (Landfall), Bee Island (Lemma Arts, 2023); tech tree like Plague Inc.; machine control like Hay Day.
- Poker TD: Balatro + Kingdom Rush references; damage = chips x mult; rings around buff machines (ring = buff order); hand of 8; Flush/Straight need 5 cards, start tower limit 5; early burst at best hand (keep or drop later); machine/item shop like Balatro jokers; core done 2026-11-13.
- Virtual Pet: parked at 80%; Wong keeps his hand-made lighting/environment (do not run the scene builder).
- Use .asmdef per game.

## Didn't work
- Water B with 0.5 m cells at 50 steps/s and fast-speed steps divided by 2: 5x dropped to ~14 real fps (more fixed steps per slow frame = spiral). Fix: 25 steps/s and the old 0.2 s fast-speed step.
- Toggling Burst options from a script in Play mode makes Burst recompile; jobs run without Burst (10x slower) for a while. Restored to the defaults afterwards.
- Wave maker v1 with a splash every 3 s added ~3.7 m3/s (more than the stream inflow), so the reservoir kept rising; now push only.
- Water compare v1: surface mesh sampled ground every frame (16 ms per lane, 22 fps) and foam keyed to slope made lane A all white. Fix: bilinear weights + ground heights cached once (1.7 ms, 70-90 fps), stricter foam.
- Sandbox as a builder game / empire builder with predators and crafting - off-concept or too big.
- One-hex dam to raise the water - water flows around it; dam lines from bank to bank are needed.
- PhysX joint breakForce on stacked blocks - joints snap under their own weight.
- Deleting files from the device shell - no permission; use _to_delete.
- device_commit_files sometimes writes an older copy - check md5 on the device; if it repeats, apply the edit on the device with python instead.
- 2026-11-13 Claude compiled while Wong's game was in Play mode: the domain reload wiped the colony lists (75 beavers on the map, 0 in the code) and the session was lost. Always check EditorApplication.isPlaying before a compile and ask Wong first.
- 2026-11-13 the connection to Wong's computer (and other connectors) failed with 503 errors in the middle of Phase A; "Temporarily unable to authenticate" in the app. Server-side, not Wong's PC.
- First version of the patch scripts was not safe to run twice (edits that keep the old line were inserted again). Fixed: each edit now checks if its result is already in the file.
- 5x game speed with the water stepping at the 1x rate on 128 x 96: ~2 fps (fixed by option a).
- The PROGRESS.md on disk was an older copy (before plan v2); replaced 2026-11-13 with the newer one Wong had.
- Poker TD: % health buffs, x1.5 per hand level, early burst for High Card - rejected.

## Open questions
- Season rewards (Wong decides later).
- Food balance: forage rate vs winter need - check in the first full-year playtest.
- Enemies/army (Next steps 2). Flood: is 6/12 swept away in one wave too much for year 1? Should sandbags be possible without sand (year-1 flood comes before the sand sifter)?
- 5x speed now ~24 fps in the test (was ~33); check if the new water streaks cost GPU time.
- Edit-mode NullReference in CameraRig.LateUpdate -> ForestTerrain after a recompile (old, harmless in Play); guard it in the polish phase.
- Food storage holds 40 (60 after B5), but B3/B4 cost 60-70 food: the player needs 2 food storages. OK, or pay research food bit by bit?
- The material lab, machines and wheel use simple box/cylinder looks; real models in the polish phase. HUD icons are painted in code; Wong may draw real ones in Affinity (same names, Resources/HudIcons/).
- Pump on high dry ground still sends water down the slope (it counts dry cells below the target as needy). Keep (it is physical) or only pump into hexes that already touch water?
- Poker TD: keep early burst? Final shop list in the content plan. Two machines with the same ring number: order?
- Real Game Systems deadline still unknown.

## Assets
- assets/ui_v2_1.png, ui_v2_2.png - (no text: view the original) new HUD: pills, clock, build tray with icon tiles, Work card, building pop-up
- assets/map_random_1.png, map_random_2.png, map_random_close.png - (no text: view the original) random maps (seeds 527117, 865194) and the smooth shoreline
- assets/B_main_5x.png, B_main_clear_day3.png - (no text: view the original) main map with water B (old look at 5x; clear look at noon)
- assets/wc3_laneS.png, wc3_C_vs_S.png, wc2_laneC_clear.png - (no text: view the original) demo v2/v3: clear water, lane S, boats, blocks
- assets/wc_overview2.png, wc_dambreak2.png, wc_laneA.png, wc_laneB.png, wc_laneC.png, wc_flood5x.png - (no text: view the original) water compare demo screenshots
- assets/PokerTD_Model.xlsx -> md/PokerTD_Model.xlsx.md - Poker TD economy and damage model (values only).
- assets/PokerTD_Reference.xlsx -> md/PokerTD_Reference.xlsx.md - Balatro + Kingdom Rush analysis with APA sources.
- assets/WindPet_Balance.xlsx -> md/WindPet_Balance.xlsx.md - Virtual Pet bars (no WorkConfig numbers yet).
- assets/Sack_Preview_v3.png - (no text: view the original) sack shell preview.
- assets/Research_Beaver_AI_and_Water.md -> md/Research_Beaver_AI_and_Water.md.md - beaver behaviour + water methods, APA.
- assets/Sandbox_Plan_v2.md - (not converted yet; plain markdown) plan v2: seasons, food, categories x variations, phases, APA. Copy from 03_Sandbox/1_Plan.
- assets/Sandbox_Plan_v4.md - (plain markdown) plan v4 = v3 + floods v2 and flood defences (section 10).
- assets/Sandbox_Plan_v3.md - (plain markdown) plan v3 = v2 + Phase C recipes, costs, tech tree (section 8) and new Phase B costs (section 9), APA.
- assets/phaseC_industry_tab.png, phaseC_build_tab.png - (no text: view the original) HUD screenshots of Phase C (Industry tab is gone since fix round 1).
- assets/fix_popup_water.png, fix_water_close.png - (no text: view the original) machine pop-up, build categories, 2-row bar, water streaks.

## Other key files
- Assets/Sandbox/Scripts/Colony/ColonyHud.cs (HUD) + Scripts/UI/HudIcons.cs (painted icons; real art goes in Resources/HudIcons/).
- Assets/Sandbox/Scripts/Map/MapRandomizer.cs - random map at Play; ForestTerrain.Randomize makes the streams.
- Assets/Sandbox/Scripts/Water/WaterGrid.cs - water B (Burst, 0.5 m); old 1 m CPU version in progress/backup/.
- Assets/Sandbox/WaterCompare/ (own asmdef DamRush.WaterCompare) + Scenes/WaterCompare.unity - water demo; menu Dam Rush/Build Water Compare Scene rebuilds it. MainMap backup: Scenes/MainMap_backup_2026-11-13.unity.
- progress/patches/phaseA_colony.py, phaseA_patch.py - the rest of Phase A (run from the project root).
- 03_Sandbox/1_Plan/Sandbox_Plan_v4.md - plan v4 (current); v2 and v3 kept.
- Assets/Sandbox/ - Dam Rush (namespace DamRush). Scenes/MainMap.unity (menu "Dam Rush/Build Main Map" rebuilds the WHOLE scene), Scenes/WaterLab.unity.
- Assets/Sandbox/Scripts/Colony: FloodSystem.cs (sweep, flooded buildings, gifts, forecast), BuildOrders.PhaseD.cs (levee, floodgate, sandbags, bell, pressure wear), BuildOrders.Select.cs (click to select, pop-up data, SitePick colliders, storage type change), Industry.cs (Phase C data: machines, recipes, tech tree, wheels, research), BuildOrders.PhaseC.cs, Beaver.PhaseC.cs (operators), BuildOrders.PhaseB.cs + ColonyManager.PhaseB.cs (Phase B types and rules), ColonyManager, Beaver, BuildOrders (orders, repair, drops, piles of storages), Site/DamSite/BuildingSite, MaterialPile (piles + ISupply), ItemInfo, Seasons, DayNight, ColonyHud. Water: WaterGrid, WaterRipples, FlowStreaks (off). Map: MapController (floods). Game: CameraRig, GameSpeed.
- Assets/Sandbox/Shaders/DR_StylizedWater.shader + Materials/M_DR_StylizedWater.mat - water look.
- Assets/Sandbox/Data/MainMapConfig.asset - water + map numbers.
- Assets/Progression/ - Poker TD (namespace PokerTD); Data/TDConfig.asset; Editor/PokerTDBuilder.cs.
- Assets/VirtualPet/ - Virtual Pet (namespace WindPet); do NOT run "Wind Pet/Build Lab In Scene".
- 01_VirtualPet/2_Design/, 02_Progression/2_Design/Model/ - originals of the assets.
- _to_delete/ (project root) - 2 orphan .meta files of moved screenshots; Wong can delete the folder.