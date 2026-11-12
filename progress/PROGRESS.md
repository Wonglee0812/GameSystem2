# PROGRESS - Game Systems (3 solo Unity games)
Last updated: 2026-11-12 (end of session)

## Goal
Three solo games in one Unity project (GameSystem2): Virtual Pet, Progression, Sandbox. Aim: Excellent on all three, finished before Christmas 2026. Rubric checklist: 00_Course/Rubric/Rubric_Checklist.md.

## Current status
- Virtual Pet (wind pet): about 80%, playable and fun. Parked until the polish phase.
- Progression (Poker TD): graybox prototype, steps 1-4 of 6 done. Wong playtested on 2026-11-12: the core loop is stable. What is left is polish and the side features (rest of the shop, logging, theme).
- Sandbox: not started.

## Next steps
1. Poker TD step 5, rest of the shop turn: hand levels, extra tower slots, extra machine slots, 4 one-use item slots, unlock Four of a Kind / Straight Flush. (Buff machine offers are already in the shop.)
2. Update PokerTD_Model.xlsx with the rule changes of 2026-11-12: hand of 8 cards, 4-card Flush/Straight, duplicate card does not reset the timer, round-robin aim, cannon radius 2.6, sell back 50%. Then re-run Run Sim.
3. Poker TD step 6: CSV log per run + Excel graph that compares strategies (Excellent criterion 1).
4. Answer the open ring questions below (ring 1 inner?, rings = tower or machine limit?, ring width).
5. Poker TD polish: theme/story (Excellent criterion 3), final machine list and rarity tiers, art pass (no pixel art), sound.
6. Wong tests with the real mouse in Virtual Pet: printer red button, click/hold pickup, pet into the turbine.
7. Add all new WorkConfig numbers to WindPet_Balance.xlsx (offered, not done yet).
8. Polish phase, Virtual Pet: sack shell in Unity, Animator + hand-keyed clips (Wong keys), WebGL build, video, playtest forms.
9. Sandbox game: not started.

## Done
- 2026-11-10 Virtual Pet: electricity job complete (turbine, bills, stamp + ink, pachinko slot, lights, dryer on power).
- 2026-11-10 Virtual Pet: click = take item, hold = drag; tools stay in the slot while dragging props; desk tidied (8 props moved to shelves); rotor spins on the axle; bills 1-5 kWh, bigger bill and printer.
- 2026-11-10 Virtual Pet: shop on the pachinko screen (turbine levels 1-5, 6 potions), bills scale with level, big orders, printer red button with charges, loan safety net, desk slab + prop recovery.
- 2026-11-10 Virtual Pet: game clock, contracts with deadline and fines (3 fines -> next takes 80% of money), potion prices follow turbine level, digestion energy regen (fedRegen), turbine output raised (0.08 kWh/s, x1.8 per level).
- 2026-11-10 Virtual Pet: sack shell preview built from Blender primitives (Sack_Preview_v3.png). Not yet approved or imported.
- 2026-11-10 Poker TD: reference analysis (PokerTD_Reference.xlsx) and economy/damage model (PokerTD_Model.xlsx).
- 2026-11-10 Poker TD step 1-2: scene, path, 3 towers, card marks, hand evaluation (ace-low straight fixed), bursts, 4 enemy types, antes, game over, dashed range rings.
- 2026-11-10 Poker TD step 3: 52-card deck, deal one card per tower, 3 discards, drag card onto tower, tower limit 4, board sinks into a placeholder shop and rises again.
- 2026-11-10 Poker TD: each hit also deals the card's value (2-10, J/Q/K 10, A 11).
- 2026-11-12 Assembly definitions: WindPet, WindPet.Editor, PokerTD, PokerTD.Editor (.asmdef). Compiles with no errors.
- 2026-11-12 Poker TD step 4: BuffMachine + RingGuide scripts, 4 placeholder machines (+50 chips, +25% chips, +4 mult, x1.5 mult) on keys 4-7, machine limit 5, rings drawn with order numbers, hover panel on towers (example hand Pair). Test: Pair lvl 5 gives 2250 in order and 1662.5 reversed, same as sheet Burst.
- 2026-11-12 Fixes after Wong's test: machines are bought in the shop (random offers, like jokers) and placed free in Deploy, no build buttons; sell placed machine by right-click (sellBack 0.5); ring bands filled green->red, raised above the tiles; cannon blast radius 1.6 -> 2.6 + flash; Flush/Straight need 4 cards (TDConfig.flushStraightCards).
- 2026-11-12 Balance fixes after Wong lost every run: hand = 8 cards (+2 per tower slot above 8; no extra draw when building); a duplicate card no longer restarts the burst timer; towers aim first at enemies without their card. All three are TDConfig options.
- 2026-11-12 Round-robin aim (towers go back to the enemy they shot longest ago when all carry their card); right-click sells towers too (card back to hand); Sort: Rank / Sort: Suit buttons; COMBOS panel top-right (like TFT traits: hands the tower cards can make, have/need, chips x mult at current level, locked hands).
- 2026-11-12 PokerTD_Model.xlsx: sheet Burst and Inputs rows 25-26 changed from "priority slots with smaller circles" to rings.

## Decisions
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
- Flush and Straight count with 4 cards - why: each tower holds 1 card and a card marks once, so with 4 towers an enemy never gets 5 different cards (Flush never happened). Like Balatro's Four Fingers joker. Full House still needs 5 cards, so it needs tower limit 5+.
- Use .asmdef per game - why: faster recompiles, games stay separate (Wong approved).
- Virtual Pet scene: Wong added environment, lighting and post-processing by hand. Keep them; do not rebuild the scene with the builder menu.

## Didn't work
- % of enemy health buffs - why: they make the difficulty curve pointless (Wong rejected them).
- x1.5 damage per hand level - why: Wong found it too easy; replaced by fixed % of base chips.
- "Later slot = smaller circle" for buff machines - why: a misreading; Wong meant rings around the machine (see Decisions).
- Deleting files from the device shell - why: no delete permission; test files are moved to _to_delete instead.

## Open questions
- Ring 1 = inner ring (closest to the machine)? Assumed; toggle TDConfig.ringOneIsInner.
- Number of rings = tower limit (Wong's first message) or buff machine limit (his example)? Code uses tower limit.
- Two machines give the same ring number to one tower: who goes first? Assumed the machine built first.
- Keep 4-card Flush/Straight, or start with tower limit 5 instead?
- Ring width 2.5 units (1.25 tiles) makes the rings large on the 14x9 map; check in playtest.
- Hand size and balance now live in TDConfig only; the Excel model is behind (see Next steps 2).
- Sack shell: Wong will change Sack_Preview_v3.png.
- Which buff machines to keep, and rarity tiers? (later; 4 placeholders in TDConfig.buffs now)
- Poker TD theme/story (needed for Excellent criterion 3): not chosen yet (later).
- Run Sim shows the balanced strategy always wins: needs a fix (do it with Next steps 2).

## Assets
- assets/PokerTD_Model.xlsx -> md/PokerTD_Model.xlsx.md - Poker TD economy and damage model: Inputs, Hands, Enemies, Burst (priority-slot calculator), Run Sim (4 strategies, 24 waves). Burst uses rings since 2026-11-12. Values only, no formulas or charts in the md.
- assets/PokerTD_Reference.xlsx -> md/PokerTD_Reference.xlsx.md - Balatro hands and Kingdom Rush towers/enemies/waves analysis, Takeaways with APA sources.
- assets/WindPet_Balance.xlsx -> md/WindPet_Balance.xlsx.md - Virtual Pet bars: Parameters, Bands, Rules, Simulation. Does not yet hold the WorkConfig numbers (shop, loan, contracts).
- assets/Sack_Preview_v3.png - (no text: view the original) sack shell preview render.

## Other key files
- Assets/VirtualPet/ - Virtual Pet (namespace WindPet); Data/WorkConfig.asset, PetConfig = tuning numbers
- Assets/VirtualPet/Editor/LabSceneBuilder.cs, WorkSetup.cs - menu "Wind Pet/Build Lab In Scene" rebuilds the scene (do NOT run: it would lose Wong's lighting/environment work)
- Assets/Progression/ - Poker TD (namespace PokerTD); Scenes/PokerTD.unity; Data/TDConfig.asset
- Assets/Progression/Scripts/Combat/BuffMachine.cs, RingGuide.cs, DamageCalc.cs, Blast.cs - step 4 (rings, buff order, hover text, cannon flash); UI/ShopView.cs - machine offers
- Assets/Progression/Editor/PokerTDBuilder.cs - menu "Poker TD/Build Prototype Scene"
- 01_VirtualPet/2_Design/, 02_Progression/2_Design/Model/ - originals of the assets above
- Assets/Screenshots, _to_delete - test leftovers, safe to delete
