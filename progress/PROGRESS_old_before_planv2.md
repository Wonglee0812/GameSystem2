# PROGRESS - Game Systems (3 solo Unity games)
Last updated: 2026-11-13 (Sandbox playtest 2 fixes: progress bars, dam repair, wood balance, no alarm on dam break, stylised water, 50% drops)

## Goal
Three solo games in one Unity project (GameSystem2): Virtual Pet, Progression, Sandbox. Aim: Excellent on all three, finished before Christmas 2026. Rubric checklist: 00_Course/Rubric/Rubric_Checklist.md.
Order of work (Wong, 2026-11-12): finish Poker TD -> build Sandbox -> skeletons of all 3 done -> write the 3 reports -> polish all 3 -> videos last.

## Current status
- Virtual Pet (wind pet): about 80%, playable and fun. Parked until the polish phase.
- Progression (Poker TD): core done (steps 1-5 of 6). Wong tested the shop on 2026-11-13: works well. Left: step 6 (CSV log + strategy graph), Excel model update, content plan (items, cards, machines), visual/UI polish, theme. Parked to start Sandbox.
- Sandbox "Dam Rush": leader of a beaver family (see Decisions). Playable in Assets/Sandbox/Scenes/MainMap.unity: forest map, hex grid, 12 beavers (4 adults, 5 yearlings, 3 kits) that grow up, work groups, dam orders (3 stages), base / nursery / storage buildings, needs, day/night, tail-slap alarm, leak repair, dam wear, tabbed HUD. Playtest fixes of 2026-11-13 are coded and compile; water fix measured in Play mode; the UI fixes are NOT tested in game yet.

## Next steps
1. Sandbox: Wong tests the playtest-2 fixes (see Done 2026-11-13 "playtest 2"): world bars, repair tool 6/R + "fix" / "Repair all", piles after a break, new water look, wood economy (Events > debug > Economy per day). Then: new buildings, clearer obstacles, food system (Wong's next list).
1b. (older, still to confirm) Wong tests the earlier 2026-11-13 fixes in MainMap: hover tooltips (no tool on), speed buttons / Space / - / =, builders carrying 2 (adults), priority flag (key 5 or P, "flag" button), remove = right-click twice (flashes red) and "x" then "sure?", drag the dam tool to paint a dam line, a dam line across the valley raises the water to nursery depth (0.7 m). Then list new changes.
3. Check the water step time at 1x speed (was 4.5 ms; depth-scaled pipes may cost more; seen 9-12 ms while running at 3x). Check in a WebGL build.
4. Sandbox: resources running out (trees now infinite), more buildings if needed; dam labels overlap when hexes are close.
5. Sandbox plan / player-experience statement in 03_Sandbox/1_Plan (English, APA) before playtest 1 - Excellent criterion "player experience defined and refined through cycles". Not written yet.
6. Sandbox playtest cycles 1-3 (prototype -> test -> improve), screenshots of player builds.
7. Poker TD step 6: CSV log per run + Excel graph that compares strategies (Excellent criterion 1). Needed before the report.
8. Update PokerTD_Model.xlsx with all rule changes (hand of 8, 5-card Flush/Straight, start tower limit 5, early burst at best hand, round-robin aim, cannon radius 2.6, sell back 50%, shop prices/weights, reroll, items). Fix Run Sim (balanced strategy always wins).
9. Poker TD content plan (items, buff machines + rarity, hand upgrades), then balance; polish (visuals, UI, sound, theme).
10. Virtual Pet: Wong tests with the real mouse; add WorkConfig numbers to WindPet_Balance.xlsx.
11. Write the 3 design reports (Wong). Tip: write each Sandbox playtest section right after its cycle.
12. Polish phase, Virtual Pet: sack shell in Unity, Animator + hand-keyed clips (Wong keys), WebGL build, playtest forms.
13. Videos for all 3 games (last step).

## Done
- 2026-11-13 Sandbox playtest 2 fixes (coded, compiled, checked in Play mode): world bars above sites (build %, repair, rebuild, dam condition, nursery "next kits" or why it waits); dam repair tool + "fix"/"Repair all"; no alarm on dam break (auto flag + idle help); 50% drops as piles and pickup; wood balance; economy counters (Events > debug); stylised water shader + M_DR_StylizedWater.mat. Measured: break -> no alarm, flag set, piles used by builders; repair 2 mud on a base at 40%.
- 2026-11-10 Virtual Pet (summary): electricity job, click/hold pickup, shop on the pachinko screen, bills, contracts with fines, loan, game clock, digestion regen; sack shell preview (Sack_Preview_v3.png, not approved yet).
- 2026-11-10 to 2026-11-12 Poker TD (summary): reference + model sheets; steps 1-4 (scene, path, 3 towers, deck, discards, bursts, 4 enemies, antes, buff machines in rings, COMBOS panel, sorting, round-robin aim, selling); balance fixes after Wong's tests. .asmdef per game.
- 2026-11-13 Poker TD step 5 shop (offers, reroll, 4 item slots); right-click sells stock machines. Wong tested: works well. Core loop done.
- 2026-11-13 Sandbox Cycle 1 "builder" version (player places blocks, TABS loop) - built, then rejected by Wong as off-concept. Files moved to _to_delete/DamRush_Cycle1.
- 2026-11-13 Sandbox buildings + UI refactor (earlier chat, not logged then): base (stone platform, blocks water), nursery on a base (entrance needs water >= 0.7 m next to it; litters cost wood; kits per nursery), storage on a base (30 of each material; replaces the lodge stock; starter storage near the lodge); kits -> yearlings -> adults over game days; HUD with tabs Build / Work / Family / Events, toasts, banners, flood phases (F).
- 2026-11-13 Sandbox playtest fixes: (a) water: terrain banks kept above the stream bed (water left the bed at x 24-36 into the forest = the "broken" stream); pipe area = flow depth x cell width (SandboxConfig.depthScaledPipes, on for MainMap only). Measured: stream 0.23-0.28 m deep, ~2.7 m/s, no dry gaps (before 0.04-0.15 m, 4-5 m/s, dry gaps). A line of 8 finished dams across the valley at x=30 gave 1.13 m depth after ~4 game minutes, still rising; a single-hex dam lets water flow around it. (b) dam tool can be dragged; tooltip warns when water can flow around. (c) hover tooltip for every hex (site progress + materials + depth; finished: function + depth; free hex/base: depth + what can be built). (d) top bar shows stock only; capacity in the storage tooltip. (e) GameSpeed.cs: pause / 1x / 2x / 3x. (f) builders: adult carries 2, yearling 1. (g) priority flag. (h) two-step remove.
- 2026-11-13 Research notes: beaver behaviour for the AI + water simulation methods (03_Sandbox/2_Design/Research_Beaver_AI_and_Water.md, APA).
- 2026-11-13 Sandbox base (summary): water upgrades (Manning, displacement, pressure forces, m3/s sources, open edge) + Water Lab (wood lift/weight 1.0, wall push 2.9 vs 2.7 kN); main map 96 x 72 m, streams, 324 trees, 621 hexes, 2.5D camera; colony phases 1-3: lodge, A* on hexes, work groups, dam tool in 3 stages (finished dam cut outflow 2.3 -> 0.9 m3/s), day/night 240 s, energy + food, tail slap, leak repair instinct, dam wear, event log.

## Decisions
- Adults carry 2 items per trip for every job (cut, dig, build, piles), yearlings 1 - why: wood was always 0 in playtest 2; measured 1 cutter (~4 wood/day) fed only 3 beavers.
- Hunger 0.45 -> 0.3 per s, kits eat half; a hungry cutter eats one of its own sticks - why: same wood measurement. After the change: 4 cutters 28 wood/day vs 8 eaten/day (12 beavers); storage filled up.
- The tail-slap alarm is only for floods. A dam that loses a stage gets an automatic priority flag and idle beavers help rebuild - why: Wong, playtest 2 (hiding at every break wasted time).
- Repair before a break: tool 6/R; cost = stage items x 0.5 x wear, min 1, main material of the top stage (sticks for the body, mud for seal/base). Rebuilding a lost stage costs the full stage - why: Wong found repair and rebuild cost almost the same.
- A washed-away stage or a removed order drops 50% of its materials as piles (removed storage: 100% of its stock). Builders use piles as a supply; cutters/diggers/idle carry piles to storage - why: Wong.
- Water look: custom URP shader DamRush/StylizedWater (cellular F2-F1 lines, Worley 1996; two-phase flow map, Vlachos 2010; foam from the grid, ripple rings around swimmers). Old white streaks off - why: Wong's reference image (painted turquoise water with white caustic lines).
- Virtual Pet parked at 80% - why: sack model, rig and animation take long; do them in the polish phase.
- Sack: full sack body, fullness inflates it (code), mood moves the ears (blend tree); Claude sets up the Animator, Wong keys the clips - why: rubric wants primitives and hand-made animation.
- Progression game = Poker TD (Wong's old idea, old repo Wonglee0812/Game-Design, Project_2) - why: fits strategy + resources; new art, no pixel art, to stay away from Balatro's look.
- Reference games: Balatro (hand ratios, antes) and Kingdom Rush (TD costs) - why: Excellent criterion 2.
- Damage = chips x mult. Hand level adds a fixed % of base chips (60% High Card down to 20% Straight Flush).
- Enemy health x2 per ante, 3 waves per ante, endless; lose when base lives reach 0.
- Same card (rank + suit) marks an enemy only once; wait for next mark gets shorter (2 / 1.6 / 1.2 / 0.8 s); 5 marks burst at once.
- Burst multiplier comes from the tower that placed the last mark.
- Towers: Cannon (AOE), Gatling (fast single), Trap (zone on the path).
- Two turns like Unrailed 2: shop turn, then battle turn (deal, arrange, fight).
- Prototype first with placeholder numbers, model and balance after playtest data.
- A = 11 damage per hit (card value like Balatro chips) - why: Wong confirmed 2026-11-12.
- Buff machine stands in the centre; the ground around it is split into rings, one ring per tower in the tower limit; towers are placed in the rings. Ring number = the order that machine's buff is applied to that tower (ring 1 first). It does not change buff strength - why: Wong's design; the player chooses the order (+mult before xmult = more damage, like Balatro).
- Hand size 8 like Balatro, +2 cards per tower slot above 8 - why: too few cards to play (Wong).
- Duplicate card does not reset the burst timer, AND towers spread marks to enemies without their card - why: bursts were delayed forever by a gatling hitting the same enemy; Wong asked which fix, Claude advised both.
- Rings, not pie slices - why: Wong prefers rings. Filled colours, inner green to outer red, with numbers - why: Wong found dashed rings hard to read.
- Buff machines work like Balatro jokers: random offers in the shop after each wave, owned up to the machine limit, sold for half cost - why: Wong's design.
- (2026-11-12, replaced the same day) Flush/Straight with 4 cards, because 4 towers could never give 5 different cards. Now 5 cards + start tower limit 5 (see below).
- Use .asmdef per game - why: faster recompiles, games stay separate (Wong approved).
- (replaced 2026-11-13) Sandbox concept as a TABS-like loop where the player builds the dam himself. See the leader concept below.
- Sandbox concept (2026-11-13): the player is the LEADER of a beaver family, not the builder. He orders dams and buildings on a hex grid and sets work groups; the beavers do the work with their own instincts. Goal: build dams to make a living area - why: Wong's original idea; Claude had drifted from it.
- Map: forest valley 96 x 72 m, streams with low gradient (~3-4 %, beavers prefer < 6 %) in wide valleys, several side streams, flood comes from upstream (F key for now, no challenge yet) - why: beaver habitat research; small streams are where beavers need dams.
- Hex grid over the whole map: pointy-top, odd-r rows, radius 2 m (one hex closes the main stream). Maths from Patel (2013), Red Blob Games - why: Wong wants orders and buildings on a grid.
- Colony: 6 beavers (2 adults, 2 yearlings, 2 kits; kits do not work), groups set by count, not per beaver; resources infinite for now; main lodge = 4 hexes on the bank and the central stock (central-place foraging) - why: Wong 2026-11-13.
- Dam on one clicked hex, 3 stages like real dams: base 2 stone + 2 mud (0.6 m, leaky 0.6), body 4 wood (1.4 m, leaky 1.0), seal 3 mud (1.6 m, leaky 0.05) - why: Beaver Trust (n.d.) building order; Wong chose click per hex.
- Phase 3 rules: wood is also food; night work is cheaper; tail slap sends all home; Idle workers plug loud leaks by instinct; water over the top wears dams down - why: research (Bartra Cabre et al., 2020; Wilsson, 1971 cited in Mental Floss).
- Priority order for builders: flagged sites first, then loud leaks, then the oldest order - why: Wong asked for flags; leaks stay above normal orders.
- Remove needs a second right-click (or "x" then "sure?") within 3 s; the site flashes red - why: Wong removed buildings by accident with one right-click.
- Space now pauses the game (not only the water); water pause is a debug button in the Events tab - why: speed control asked by Wong.
- Water realism over the old fixed pipe area for MainMap - why: Wong found the stream weak and dams useless; Water Lab keeps the old model it was checked with.
- Sandbox categories (current): materials (wood, mud, stone), structures (dam, lodge, storage later), work groups (wood, earth, build). Old idea of mixer machines and free building parts is on hold.
- Sandbox rules: shared = physics + water; specific = e.g. mud erodes, wet wood gets heavier, mixes are stronger.
- Sandbox is 3D; water = heightfield grid with virtual pipes (Mei et al., 2007) + Manning friction (de Almeida et al., 2012) + displacement (Kellomaki, 2013); forces on parts from pressure on surface patches (Kerner, 2015) - why: runs on WebGL; the lab numbers match physics. Wong tested the water: OK.
- Sandbox: predators and defence dropped - why: scope too big, and defence overlaps Poker TD.
- Sandbox Excellent targets: chain reactions + 3 prototype-test-improve cycles (design); modular architecture + smooth UX (implementation).
- Sandbox reference games: Timberborn (Mechanistry; beavers, dams, water physics), Totally Accurate Battle Simulator (Landfall; sandbox mode), Bee Island (Lemma Arts, 2023).
- Flush/Straight need 5 cards and the run starts with tower limit 5 - why: Wong prefers a higher start limit over the 4-card rule.
- Early burst: an enemy bursts at once when its marks make the best hand the tower cards can reach (COMBOS best), only Pair or higher; TDConfig.burstAtBestHand + HUD test button - why: Wong's idea, waiting cannot give a higher hand. Keep or drop decided later.
- Hand levels come two ways: random hand upgrade offers in the shop (like Planet cards) and the Hand manual item (player picks the hand) - why: Wong chose both.
- Slots come from random shop offers or permit items; unlocks are rare and expensive shop offers - why: Wong's design.
- Items can be used in Deploy and Wave (each item checks if it makes sense now) - why: Wong chose this.
- Machine limit counts placed machines only; buying is always allowed; stock machines can be sold by right-click - why: Wong.
- Freeze item hits the front enemy automatically, no aiming - why: Wong approved 2026-11-13.
- Poker TD core counts as done; switch to Sandbox now, Poker TD polish + content later - why: Wong, 2026-11-13.
- Virtual Pet scene: Wong added environment, lighting and post-processing by hand. Keep them; do not rebuild the scene with the builder menu.

## Didn't work
- % of enemy health buffs - why: they make the difficulty curve pointless (Wong rejected them).
- x1.5 damage per hand level - why: Wong found it too easy; replaced by fixed % of base chips.
- "Later slot = smaller circle" for buff machines - why: a misreading; Wong meant rings around the machine (see Decisions).
- Sandbox as a full empire builder + predators + unit control + crafting - why: 5 genres in one, too big for the time left.
- Early burst for High Card - why: one gatling with High Card won the game too easily (Wong's test 2026-11-12).
- Deleting files from the device shell - why: no delete permission; test files are moved to _to_delete instead.
- Sandbox as a builder game (player places blocks, TABS loop) - why: off-concept (Wong 2026-11-13).
- PhysX joint breakForce on stacked blocks - why: joints snap under the blocks' own weight; replaced by a custom pull/shear check in Part.cs (only used in the Water Lab now).
- First water lab view: flow arrows red like the push arrows, sliders saved into the asset - why: confusing (Wong); explained, fixes offered (not done yet).
- One-hex dam to raise the water - why: the valley is wide and flat, so water flows around it; a dam line from bank to bank is needed (now possible by dragging).
- device_commit_files sometimes writes an older copy of a changed file - why: upload lag; always check md5 on the device and commit again.

## Open questions
- Keep early burst at best hand, or drop it? Decide after more playtests (button above Start Wave).
- Shop prices, weights and item list are placeholders: final list in the content plan.
- Two machines give the same ring number to one tower: who goes first? Assumed the machine built first.
- Ring width 2.5 units: fine for now; the map will be expanded later.
- Hand size and balance live in TDConfig only; the Excel model is behind (Next steps 6).
- Sack shell: Wong will change Sack_Preview_v3.png.
- Poker TD theme/story: later.
- Real Game Systems deadline still unknown; planning assumes before Christmas 2026.
- Timing: Wong aims to finish the skeletons of all 3 games next week; Claude's estimate for Sandbox was about 3 weeks (3 playtest cycles). Check after Cycle 1.
- Sandbox: which buildings after nursery and storage? When do resources run out? Do gatherers carry 2 as well?
- Sandbox: how do the rubric's "3 categories x 3 variations" and "chain reactions" map onto the leader concept? Write it in the plan (Next steps 4).
- Sandbox performance: water step ~5 ms in the editor at 25 Hz on 96 x 72 cells; check in a WebGL build.

## Assets
- assets/PokerTD_Model.xlsx -> md/PokerTD_Model.xlsx.md - Poker TD economy and damage model: Inputs, Hands, Enemies, Burst (priority-slot calculator), Run Sim (4 strategies, 24 waves). Burst uses rings since 2026-11-12. Values only, no formulas or charts in the md.
- assets/PokerTD_Reference.xlsx -> md/PokerTD_Reference.xlsx.md - Balatro hands and Kingdom Rush towers/enemies/waves analysis, Takeaways with APA sources.
- assets/WindPet_Balance.xlsx -> md/WindPet_Balance.xlsx.md - Virtual Pet bars: Parameters, Bands, Rules, Simulation. Does not yet hold the WorkConfig numbers (shop, loan, contracts).
- assets/Sack_Preview_v3.png - (no text: view the original) sack shell preview render.
- assets/Research_Beaver_AI_and_Water.md -> md/Research_Beaver_AI_and_Water.md.md - beaver family, build triggers, dam building order, foraging, night activity, tail slap; game uses for each; water methods, problems of the first water version, changes and lab results; full APA reference list.

## Other key files
- Assets/Sandbox/Shaders/DR_StylizedWater.shader + Materials/M_DR_StylizedWater.mat - water look (tune on the material); Scripts/Water/WaterRipples.cs - ripple points
- Assets/Sandbox/Scripts/Colony/MaterialPile.cs - piles + ISupply (storage or pile)
- Assets/VirtualPet/ - Virtual Pet (namespace WindPet); Data/WorkConfig.asset, PetConfig = tuning numbers
- Assets/VirtualPet/Editor/LabSceneBuilder.cs, WorkSetup.cs - menu "Wind Pet/Build Lab In Scene" rebuilds the scene (do NOT run: it would lose Wong's lighting/environment work)
- Assets/Progression/ - Poker TD (namespace PokerTD); Scenes/PokerTD.unity; Data/TDConfig.asset
- Assets/Progression/Scripts/Combat/BuffMachine.cs, RingGuide.cs, DamageCalc.cs, Blast.cs - step 4 (rings, buff order, hover text, cannon flash); UI/ShopView.cs - machine offers
- Assets/Progression/Scripts/Core/ShopOffer.cs, Game/TDGame.cs (shop, items, early burst), UI/ShopView.cs, UI/TDHud.cs (item bar, test button), UI/ComboView.cs (tooltip, hand pick); Data/TDConfig.asset header "Shop (step 5)"
- Assets/Progression/Editor/PokerTDBuilder.cs - menu "Poker TD/Build Prototype Scene"
- 03_Sandbox/1_Plan, 2_Design, 3_Playtest, 4_Video, Final - Sandbox folders (2_Design has the research notes)
- Assets/Sandbox/ - Dam Rush (namespace DamRush). Scenes/MainMap.unity (menu "Dam Rush/Build Main Map" rebuilds the WHOLE scene: hand changes in the scene are lost), Scenes/WaterLab.unity (menu "Dam Rush/Build Water Lab")
- Assets/Sandbox/Data/MainMapConfig.asset, WaterLabConfig.asset - water and map numbers (lab sliders write into WaterLabConfig)
- Assets/Sandbox/Scripts/Water (WaterGrid, WaterBody, WaterSource, FlowStreaks), World (GroundHeights, ForestTerrain, LabTerrain, ValleyTerrain, HexGrid), Colony (ColonyManager, Beaver, BeaverLook, HexPathfinder, BuildOrders = orders, flags, remove, HexInfo tooltip text; Site, DamSite, BuildingSite; DayNight, ColonyHud), Game (CameraRig, GameSpeed), Map (MapController), Lab (LabController, DebugLines), Building (Part, PartFactory... used by the lab)
- The terrain is made again at play time (ForestTerrain.Awake), so terrain fixes need no scene rebuild; trees are snapped to the ground in ColonyManager.Start.
- Tuning in the Inspector: Colony object -> Needs, alarmSeconds, cutTime, digTime; BuildOrders -> damStages, wearRate, repairInstinctNoise; DayNight -> dayLength; MapController -> flood numbers
- 01_VirtualPet/2_Design/, 02_Progression/2_Design/Model/ - originals of the assets above
- Assets/Screenshots, _to_delete - test leftovers, safe to delete
