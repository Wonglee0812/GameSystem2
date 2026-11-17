# PROGRESS - Game Systems (3 solo Unity games)
Last updated: 2026-11-13 (Sandbox: plan v2 approved, Phase A half done - connection lost mid-edit)

## Goal
Three solo games in one Unity project (GameSystem2): Virtual Pet, Progression, Sandbox. Aim: Excellent on all three, finished before Christmas 2026. Rubric checklist: 00_Course/Rubric/Rubric_Checklist.md.
Order of work (Wong, 2026-11-12): finish Poker TD -> build Sandbox -> skeletons of all 3 done -> write the 3 reports -> polish all 3 -> videos last.

## Current status
- Virtual Pet (wind pet): about 80%, playable. Parked until the polish phase.
- Progression (Poker TD): core done (steps 1-5 of 6). Left: step 6 (CSV log + strategy graph), Excel model update, content plan, polish, theme.
- Sandbox "Dam Rush": playtest-2 fixes done and tested by Wong ("ok for now"). Plan v2 approved (03_Sandbox/1_Plan/Sandbox_Plan_v2.md). Phase A is HALF DONE: some files are edited, others not, so **Unity does not compile right now**. Do not press Play before step 1 below.

## Next steps
1. Finish Phase A (Sandbox). The 2 patch scripts are in progress/patches/ (copied there by Wong). From the project root run:
   `python3 progress/patches/phaseA_colony.py .` then `python3 progress/patches/phaseA_patch.py .`
   Both are safe to run twice (they skip what is already there). Then: compile, read the console, fix errors.
2. Phase A, still to do after the patches: run menu "Dam Rush/Build Main Map" (rebuilds MainMap at 128 x 96 m; hand changes in the scene are lost, the builder now sets the stylised water, colours and Seasons). Then measure the water step time (was ~5 ms at 96 x 72; estimate ~9 ms), playtest one full year at 5x, balance food (winter need vs forage rate), check ice + "frozen in" + game over.
3. Phase B: Dig (-0.35 m per order, max -1.5 m), Fell tree (tree removed, more wood at once), Dam Mk2 (stronger, also a base; buildings lost when it breaks, 50% back), Spillway, Base Mk2 (pumps water from downstream: +0.5 m on the hexes around it, buildings on it need less depth), food farm (water lilies, duckweed; costs mud).
4. Phase C: Material lab + Building lab (tech tree paid with food), machines (wood splitter -> branches, wood press, stone crusher -> gravel, sand sifter mud -> sand, cement mixer), water wheel (faster flow = faster production), each machine needs 1 beaver; Hay Day style orders (amount) or automation; data-driven recipes/machines.
5. Phase D (stretch): wolves that ambush beavers far from water.
6. Season rewards: Wong decides later.
7. Sandbox playtest cycles + screenshots of player builds; player-experience statement is in Plan v2, refine it after each cycle.
8. Poker TD step 6: CSV log per run + Excel graph comparing strategies (Excellent criterion 1).
9. Update PokerTD_Model.xlsx with all rule changes; fix Run Sim (balanced strategy always wins).
10. Poker TD content plan (items, machines + rarity, hand upgrades), balance, polish.
11. Virtual Pet: test with the real mouse; add WorkConfig numbers to WindPet_Balance.xlsx.
12. Write the 3 design reports; then polish (Virtual Pet sack shell, Animator, WebGL builds, playtest forms); videos last.

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
Not done (in the patch scripts):
- phaseA_colony.py -> ColonyManager: foragers group, starveDays, litterFood, Awake adds Seasons, LodgeWaterDepth, LodgeFrozenIn, ReachableNow/NearestFood, Die + GameOver, PickForageSpot. (The edit was cut off; check whether it landed - the script detects it.)
- phaseA_patch.py -> Beaver (eat from food storage, tree bark = half meal, forage, health + death, winter: stay in lodge, eat only under the ice), GameSpeed 5x, HUD (season line, all stock chips, goal box, storage type buttons, Foragers row, health bar, game over + Play again, FROZEN IN banner), ice in DR_StylizedWater.shader, MainMapBuilder 128 x 96 (points x 4/3) + Seasons component.

## Done
- 2026-11-13 Sandbox plan v2 approved by Wong (see Decisions 2026-11-13 "plan v2").
- 2026-11-13 Sandbox playtest 2 fixes (tested, Wong: ok): world bars above sites (build %, repair, rebuild, dam condition, nursery "next kits" or why it waits); repair tool 6/R + "fix"/"Repair all"; no alarm on dam break (auto flag + idle help); 50% drops as piles, builders take from piles, gatherers carry piles home; wood balance; economy counters (Events > debug); stylised water shader + M_DR_StylizedWater.mat.
- 2026-11-13 Sandbox playtest 1 fixes: water banks + depth-scaled pipes, dam drag tool, hover tooltip per hex, top bar stock only, GameSpeed, adults carry 2, priority flag, two-step remove; buildings (base, nursery, storage), kits grow up, tabbed HUD; research notes (03_Sandbox/2_Design/Research_Beaver_AI_and_Water.md).
- 2026-11-13 Sandbox base: water lab, main map, hex grid, colony phases 1-3. Cycle 1 "builder" version rejected (off-concept).
- 2026-11-10 to 2026-11-13 Poker TD: reference + model sheets, steps 1-5 (towers, deck, bursts, antes, buff machines in rings, combos, shop + items). Wong tested the shop: works well.
- 2026-11-10 Virtual Pet: electricity job, pickup, pachinko shop, bills, contracts, loan, clock, digestion; sack preview v3 (not approved).

## Decisions
- 2026-11-13 plan v2 (Sandbox), Wong:
  - Main goal: survive the seasons, endless. 4 seasons x 10 game days (day = 240 s, 40 min per season at 1x); 5x speed for playtests. Spring = floods (stronger every year), summer = build + food, autumn = store food, winter = survive the ice. Lose when the whole family is dead. Season rewards: later.
  - Food is its own resource (wood is no longer food). Foragers group. No fish traps: beavers do not eat fish (Beavers in England, n.d.); food farm grows water plants instead.
  - Storages: one building "Storage", the leader picks the type (option b).
  - Dam Mk1 = only holds water; Dam Mk2 = stronger + a base (buildings lost when it breaks, 50% back); Spillway; Base Mk1 = as now; Base Mk2 = pumps water from DOWNSTREAM (+0.5 m around it, water is moved, not created).
  - Two labs (materials, buildings), paid with food. Machines need a water wheel AND one beaver; faster flow = faster production; Hay Day style orders or automation.
  - Sand: made from mud by a sand sifter (Wong: a machine that makes/filters sand from mud).
  - Map 128 x 96 m.
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
- Sandbox as a builder game / empire builder with predators and crafting - off-concept or too big.
- One-hex dam to raise the water - water flows around it; dam lines from bank to bank are needed.
- PhysX joint breakForce on stacked blocks - joints snap under their own weight.
- Deleting files from the device shell - no permission; use _to_delete.
- device_commit_files sometimes writes an older copy - check md5 on the device.
- 2026-11-13 the connection to Wong's computer (and other connectors) failed with 503 errors in the middle of Phase A; "Temporarily unable to authenticate" in the app. Server-side, not Wong's PC.
- Poker TD: % health buffs, x1.5 per hand level, early burst for High Card - rejected.

## Open questions
- Season rewards (Wong decides later).
- Food balance: forage rate vs winter need - check in the first full-year playtest.
- Water step cost on 128 x 96, and at 5x speed (the water may not keep up).
- Poker TD: keep early burst? Final shop list in the content plan. Two machines with the same ring number: order?
- Real Game Systems deadline still unknown.

## Assets
- assets/PokerTD_Model.xlsx -> md/PokerTD_Model.xlsx.md - Poker TD economy and damage model (values only).
- assets/PokerTD_Reference.xlsx -> md/PokerTD_Reference.xlsx.md - Balatro + Kingdom Rush analysis with APA sources.
- assets/WindPet_Balance.xlsx -> md/WindPet_Balance.xlsx.md - Virtual Pet bars (no WorkConfig numbers yet).
- assets/Sack_Preview_v3.png - (no text: view the original) sack shell preview.
- assets/Research_Beaver_AI_and_Water.md -> md/Research_Beaver_AI_and_Water.md.md - beaver behaviour + water methods, APA.
- assets/Sandbox_Plan_v2.md - (not converted yet; plain markdown) plan v2: seasons, food, categories x variations, phases, APA. Copy from 03_Sandbox/1_Plan.

## Other key files
- progress/patches/phaseA_colony.py, phaseA_patch.py - the rest of Phase A (run from the project root).
- 03_Sandbox/1_Plan/Sandbox_Plan_v2.md - plan v2.
- Assets/Sandbox/ - Dam Rush (namespace DamRush). Scenes/MainMap.unity (menu "Dam Rush/Build Main Map" rebuilds the WHOLE scene), Scenes/WaterLab.unity.
- Assets/Sandbox/Scripts/Colony: ColonyManager, Beaver, BuildOrders (orders, repair, drops, piles of storages), Site/DamSite/BuildingSite, MaterialPile (piles + ISupply), ItemInfo, Seasons, DayNight, ColonyHud. Water: WaterGrid, WaterRipples, FlowStreaks (off). Map: MapController (floods). Game: CameraRig, GameSpeed.
- Assets/Sandbox/Shaders/DR_StylizedWater.shader + Materials/M_DR_StylizedWater.mat - water look.
- Assets/Sandbox/Data/MainMapConfig.asset - water + map numbers.
- Assets/Progression/ - Poker TD (namespace PokerTD); Data/TDConfig.asset; Editor/PokerTDBuilder.cs.
- Assets/VirtualPet/ - Virtual Pet (namespace WindPet); do NOT run "Wind Pet/Build Lab In Scene".
- 01_VirtualPet/2_Design/, 02_Progression/2_Design/Model/ - originals of the assets.
