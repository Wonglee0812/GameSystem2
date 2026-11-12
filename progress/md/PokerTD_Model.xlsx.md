## README
| Poker TD - economy and damage model (v0.1) |
| --- |
| NaN |
| What this file does: models the damage of one card burst and a full run (8 antes x 3 waves), so the numbers can be tested before the prototype. |
| Yellow cells with blue numbers are inputs. Change them and every sheet updates. Black = formulas, green = links to another sheet. |
| NaN |
| Sheets |
| Inputs - all tuning numbers in one place (enemy health, antes, gold, prices, buff machines). |
| Hands - the 9 poker hands: base chips, base mult and growth per level (ratios from Balatro, see sources). |
| Enemies - the 4 enemy types. |
| Burst - damage of ONE burst on one enemy, with up to 10 buff machines applied in priority order (+ first, x last). |
| Run Sim - a whole run, wave by wave, for 4 strategies. Shows how many bursts a normal enemy needs. |
| NaN |
| Rules modelled (agreed design, Nov 2026) |
| Damage = Chips x Mult. A hand level adds a fixed % of the hand's base chips (strong hands get a smaller %). Mult comes from the hand and from buff machines. |
| Buff machines are bought like jokers. Each machine has a circle aura and sits in a priority slot 1..N. Slots are applied in order, |
| one step at a time, to chips and to mult. Later slots have smaller circles (ASSUMPTION - check with the designer). |
| Enemy health doubles every ante; one ante = 3 waves (normal, elite, boss). |
| Card rules: the same card (same rank and suit) leaves only one mark on an enemy; the wait for the next mark gets shorter as the hand grows. |
| NaN |
| Known simplifications (Run Sim) |
| Each strategy spends a fixed share of its total gold on levels, extra towers and machines. Prices rise by a fixed factor per purchase. |
| The best hand a player can build depends only on the number of towers (table in Inputs). Each machine counts as its average multiplier. |
| These are placeholders until playtest data replaces them. |
| NaN |
| Sources (APA) |
| Balatro Wiki. (n.d.). Planet cards. Fandom. https://balatrogame.fandom.com/wiki/Planet\_Cards |
| Prima Games. (2024). All poker hands and their chips and mults in Balatro. https://primagames.com/gaming/all-poker-hands-and-their-chips-and-mults-in-balatro |
| saschb2b. (n.d.). Balatro: Blinds and bosses. Game Mechanics. https://saschb2b.github.io/game-mechanics/games/balatro/blinds-and-bosses |

## Inputs
| Inputs | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 |
| --- | --- | --- | --- |
| NaN | NaN | NaN | NaN |
| Enemies | NaN | NaN | NaN |
| Normal enemy health at ante 1 | 250 | Agreed start value. | NaN |
| Health growth per ante (x) | 2 | Agreed: x2 per ante. Balatro grows about x2.7 early and x1.4 late (saschb2b, n.d.). | NaN |
| Waves per ante | 3 | Agreed: 3 (to check in playtests). | NaN |
| Wave 1 of ante (normal) health x | 1 | Balatro small blind x1. | NaN |
| Wave 2 of ante (elite) health x | 1.25 | Placeholder (Balatro big blind is x1.5). | NaN |
| Wave 3 of ante (boss) health x | 1.5 | Placeholder (Balatro boss is x2). | NaN |
| Gold | NaN | NaN | NaN |
| Gold per wave at ante 1 | 4 | Placeholder, Balatro-like small money. | NaN |
| Extra gold per wave per ante | 1 | Placeholder. | NaN |
| Prices (each purchase costs more) | NaN | NaN | NaN |
| Hand level: first price | 3 | Placeholder. | NaN |
| Hand level: price growth (x) | 1.25 | Placeholder. | NaN |
| Extra tower: first price | 5 | Placeholder. | NaN |
| Extra tower: price growth (x) | 1.5 | Placeholder (Kingdom Rush upgrades grow about x1.5). | NaN |
| Buff machine: first price | 6 | Placeholder. | NaN |
| Buff machine: price growth (x) | 1.3 | Placeholder. | NaN |
| Towers and machines | NaN | NaN | NaN |
| Towers at the start | 3 | Placeholder. | NaN |
| Max towers | 8 | Placeholder. | NaN |
| Max buff machines (slots) | 5 | Agreed: 5 to start, more can be bought later. | NaN |
| Average x of one machine (Run Sim) | 1.35 | Placeholder: mix of + machines (~x1.2) and rare x machines (x1.5-x2). | NaN |
| Ring width around a buff machine, tiles | 1.25 | Rings = tower limit; a tower in ring k gets that buff as step k. In Unity: ringWidth 2.5 units (1 tile = 2 units). Placeholder. | NaN |
| Ring 1 (applied first) is the inner ring | Yes | Assumption, not confirmed yet. Toggle in TDConfig: ringOneIsInner. | NaN |
| Run Sim | NaN | NaN | NaN |
| Bursts to kill a normal enemy: still OK | 3 | Above this line the player is falling behind (placeholder). | NaN |
| NaN | NaN | NaN | NaN |
| Best hand reachable by number of towers | NaN | NaN | NaN |
| Towers | Best hand (assumption) | NaN | NaN |
| 1 | High Card | With the one-mark-per-card rule, more towers are needed for bigger hands. Placeholder mapping. | NaN |
| 2 | Pair | NaN | NaN |
| 3 | Three of a Kind | NaN | NaN |
| 4 | Straight | NaN | NaN |
| 5 | Flush | NaN | NaN |
| 6 | Full House | NaN | NaN |
| 7 | Four of a Kind | NaN | NaN |
| 8 | Straight Flush | NaN | NaN |
| NaN | NaN | NaN | NaN |
| Strategies: share of gold spent on | NaN | NaN | NaN |
| Strategy | Hand levels | Extra towers | Buff machines |
| A: Level hands | 0.8 | 0.1 | 0.1 |
| B: More towers | 0.1 | 0.8 | 0.1 |
| C: Buff machines | 0.1 | 0.1 | 0.8 |
| D: Balanced | 0.34 | 0.33 | 0.33 |

## Hands
| Poker hands | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 | Unnamed: 6 | Unnamed: 7 | Unnamed: 8 | Unnamed: 9 | Unnamed: 10 | Unnamed: 11 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Hand | Balatro chips | Balatro mult | Chip scale | Base chips | Base mult | Lv1 damage | Chips +% per level | Lv2 | Lv5 | Lv10 | Lv15 |
| High Card | 5 | 1 | 5 | 25 | 1 | 25 | 0.6 | 40 | 85 | 160 | 235 |
| Pair | 10 | 2 | 5 | 50 | 2 | 100 | 0.5 | 150 | 300 | 550 | 800 |
| Two Pair | 20 | 2 | 5 | 100 | 2 | 200 | 0.45 | 290 | 560 | 1010 | 1460 |
| Three of a Kind | 30 | 3 | 5 | 150 | 3 | 450 | 0.4 | 630 | 1170 | 2070 | 2970 |
| Straight | 30 | 4 | 5 | 150 | 4 | 600 | 0.35 | 810 | 1440 | 2490 | 3540 |
| Flush | 35 | 4 | 5 | 175 | 4 | 700 | 0.3 | 910 | 1540 | 2590 | 3640 |
| Full House | 40 | 4 | 5 | 200 | 4 | 800 | 0.3 | 1040 | 1760 | 2960 | 4160 |
| Four of a Kind | 60 | 7 | 5 | 300 | 7 | 2100 | 0.25 | 2625 | 4200 | 6825 | 9450 |
| Straight Flush | 100 | 8 | 5 | 500 | 8 | 4000 | 0.2 | 4800 | 7200 | 11200 | 15200 |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Balatro chips and mult at level 1 (Prima Games, 2024), chips x5 so a Pair does 100. A level adds a fixed amount: base chips x the %, like Balatro planets add a fixed amount (Balatro Wiki, n.d.). | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| The % per level is our own choice: weak hands grow faster, so leveling a cheap hand stays useful. | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |

## Enemies
| Enemy types | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 |
| --- | --- | --- | --- | --- |
| NaN | NaN | NaN | NaN | NaN |
| Enemy | Health x normal | Speed x normal | Gold x normal | Special |
| Normal | 1 | 1 | 1 | - |
| Tank | 3 | 0.6 | 2 | Slow and tough. |
| Speed | 0.6 | 1.8 | 1 | Runs past towers before 5 marks land. |
| Boom | 1.2 | 0.9 | 1.5 | Explodes on death and stuns nearby towers for a few seconds. |
| NaN | NaN | NaN | NaN | NaN |
| Placeholders. Health x normal is used for a quick check in Run Sim (tank column). | NaN | NaN | NaN | NaN |

## Burst
| One burst: hand + level + buff machines in ring order (ring 1 first) | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 | Unnamed: 6 | Unnamed: 7 | Unnamed: 8 | Unnamed: 9 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Hand | Pair | NaN | Base chips | 150 | NaN | NaN | NaN | NaN | NaN |
| Hand level | 5 | NaN | Base mult | 2 | NaN | NaN | NaN | NaN | NaN |
| Rings (= tower limit) | 5 | NaN | No buffs | 300 | NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Ring (order) | Machine type | Value | Tower inside this ring? | Ring band (tiles from machine) | Chips after | Mult after | NaN | Reverse order: chips | Reverse: mult |
| 1 | +Chips | 50 | Y | 0.00 - 1.25 | 200 | 2 | NaN | 237.5 | 7 |
| 2 | +Mult | 4 | Y | 1.25 - 2.50 | 200 | 6 | NaN | 187.5 | 7 |
| 3 | +%Chips | 0.25 | Y | 2.50 - 3.75 | 250 | 6 | NaN | 187.5 | 3 |
| 4 | +Mult | 2 | N | 3.75 - 5.00 | 250 | 6 | NaN | 150 | 3 |
| 5 | xMult | 1.5 | Y | 5.00 - 6.25 | 250 | 9 | NaN | 150 | 3 |
| 6 | - | 0 | N | no ring | 250 | 9 | NaN | 150 | 2 |
| 7 | - | 0 | N | no ring | 250 | 9 | NaN | 150 | 2 |
| 8 | - | 0 | N | no ring | 250 | 9 | NaN | 150 | 2 |
| 9 | - | 0 | N | no ring | 250 | 9 | NaN | 150 | 2 |
| 10 | - | 0 | N | no ring | 250 | 9 | NaN | 150 | 2 |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Damage (this order) | NaN | NaN | 2250 | NaN | NaN | NaN | NaN | NaN | NaN |
| Damage (reverse order) | NaN | NaN | 1662.5 | NaN | NaN | NaN | NaN | NaN | NaN |
| Order bonus | NaN | NaN | 0.353383 | NaN | NaN | NaN | NaN | NaN | NaN |
| x vs no buffs | NaN | NaN | 7.5 | NaN | NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| +Chips adds flat chips. +%Chips multiplies the chips so far. +Mult adds to mult. xMult multiplies mult. Rings are applied 1 -> N, so + first and x last gives the most damage. | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Each row = one machine; the ring is where the tower stands around that machine. Same ring from two machines: the machine built first goes first. The ring count follows the tower limit. | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |

## Run Sim
| Run simulation: 8 antes x 3 waves, 4 strategies | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 | Unnamed: 6 | Unnamed: 7 | Unnamed: 8 | Unnamed: 9 | Unnamed: 10 | Unnamed: 11 | Unnamed: 12 | Unnamed: 13 | Unnamed: 14 | Unnamed: 15 | Unnamed: 16 | Unnamed: 17 | Unnamed: 18 | Unnamed: 19 | Unnamed: 20 | Unnamed: 21 | Unnamed: 22 | Unnamed: 23 | Unnamed: 24 | Unnamed: 25 | Unnamed: 26 | Unnamed: 27 | Unnamed: 28 | Unnamed: 29 | Unnamed: 30 | Unnamed: 31 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Wave | Ante | Wave in ante | Normal HP | Tank HP | Gold this wave | Gold so far | A Levels | A Towers | A Machines | A Hand | A Damage | A Bursts / normal | B Levels | B Towers | B Machines | B Hand | B Damage | B Bursts / normal | C Levels | C Towers | C Machines | C Hand | C Damage | C Bursts / normal | D Levels | D Towers | D Machines | D Hand | D Damage | D Bursts / normal | OK line |
| 1 | 1 | 1 | 250 | 750 | 4 | 4 | 1 | 3 | 0 | Three of a Kind | 450 | 0.555556 | 1 | 3 | 0 | Three of a Kind | 450 | 0.555556 | 1 | 3 | 0 | Three of a Kind | 450 | 0.555556 | 1 | 3 | 0 | Three of a Kind | 450 | 0.555556 | 3 |
| 2 | 1 | 2 | 312.5 | 937.5 | 4 | 8 | 2 | 3 | 0 | Three of a Kind | 630 | 0.496032 | 1 | 3 | 0 | Three of a Kind | 450 | 0.694444 | 1 | 3 | 0 | Three of a Kind | 450 | 0.694444 | 1 | 3 | 0 | Three of a Kind | 450 | 0.694444 | 3 |
| 3 | 1 | 3 | 375 | 1125 | 4 | 12 | 2 | 3 | 0 | Three of a Kind | 630 | 0.595238 | 1 | 4 | 0 | Straight | 600 | 0.625 | 1 | 3 | 1 | Three of a Kind | 607.5 | 0.617284 | 1 | 3 | 0 | Three of a Kind | 450 | 0.833333 | 3 |
| 4 | 2 | 1 | 500 | 1500 | 5 | 17 | 3 | 3 | 0 | Three of a Kind | 810 | 0.617284 | 1 | 4 | 0 | Straight | 600 | 0.833333 | 1 | 3 | 1 | Three of a Kind | 607.5 | 0.823045 | 2 | 3 | 0 | Three of a Kind | 630 | 0.793651 | 3 |
| 5 | 2 | 2 | 625 | 1875 | 5 | 22 | 4 | 3 | 0 | Three of a Kind | 990 | 0.631313 | 1 | 5 | 0 | Flush | 700 | 0.892857 | 1 | 3 | 1 | Three of a Kind | 607.5 | 1.028807 | 2 | 4 | 0 | Straight | 810 | 0.771605 | 3 |
| 6 | 2 | 3 | 750 | 2250 | 5 | 27 | 5 | 3 | 0 | Three of a Kind | 1170 | 0.641026 | 1 | 5 | 0 | Flush | 700 | 1.071429 | 1 | 3 | 2 | Three of a Kind | 820.125 | 0.914495 | 3 | 4 | 1 | Straight | 1377 | 0.544662 | 3 |
| 7 | 3 | 1 | 1000 | 3000 | 6 | 33 | 5 | 3 | 0 | Three of a Kind | 1170 | 0.854701 | 1 | 5 | 0 | Flush | 700 | 1.428571 | 1 | 3 | 2 | Three of a Kind | 820.125 | 1.219326 | 3 | 4 | 1 | Straight | 1377 | 0.726216 | 3 |
| 8 | 3 | 2 | 1250 | 3750 | 6 | 39 | 6 | 3 | 0 | Three of a Kind | 1350 | 0.925926 | 2 | 6 | 0 | Full House | 1040 | 1.201923 | 2 | 3 | 3 | Three of a Kind | 1550.03625 | 0.806433 | 3 | 4 | 1 | Straight | 1377 | 0.907771 | 3 |
| 9 | 3 | 3 | 1500 | 4500 | 6 | 45 | 6 | 3 | 0 | Three of a Kind | 1350 | 1.111111 | 2 | 6 | 0 | Full House | 1040 | 1.442308 | 2 | 3 | 3 | Three of a Kind | 1550.03625 | 0.967719 | 4 | 5 | 1 | Flush | 1795.5 | 0.835422 | 3 |
| 10 | 4 | 1 | 2000 | 6000 | 7 | 52 | 7 | 3 | 0 | Three of a Kind | 1530 | 1.30719 | 2 | 6 | 0 | Full House | 1040 | 1.923077 | 2 | 3 | 3 | Three of a Kind | 1550.03625 | 1.290292 | 4 | 5 | 2 | Flush | 2423.925 | 0.825108 | 3 |
| 11 | 4 | 2 | 2500 | 7500 | 7 | 59 | 7 | 4 | 0 | Straight | 1860 | 1.344086 | 2 | 7 | 0 | Four of a Kind | 2625 | 0.952381 | 2 | 4 | 4 | Straight | 2690.420062 | 0.929223 | 5 | 5 | 2 | Flush | 2806.65 | 0.890742 | 3 |
| 12 | 4 | 3 | 3000 | 9000 | 7 | 66 | 8 | 4 | 0 | Straight | 2070 | 1.449275 | 2 | 7 | 0 | Four of a Kind | 2625 | 1.142857 | 2 | 4 | 4 | Straight | 2690.420062 | 1.115068 | 5 | 5 | 2 | Flush | 2806.65 | 1.06889 | 3 |
| 13 | 5 | 1 | 4000 | 12000 | 8 | 74 | 8 | 4 | 1 | Straight | 2794.5 | 1.431383 | 2 | 7 | 1 | Four of a Kind | 3543.75 | 1.128748 | 2 | 4 | 4 | Straight | 2690.420062 | 1.486757 | 5 | 5 | 2 | Flush | 2806.65 | 1.425187 | 3 |
| 14 | 5 | 2 | 5000 | 15000 | 8 | 82 | 8 | 4 | 1 | Straight | 2794.5 | 1.789229 | 3 | 7 | 1 | Four of a Kind | 4252.5 | 1.175779 | 3 | 4 | 5 | Straight | 4573.714106 | 1.093203 | 6 | 6 | 3 | Full House | 4920.75 | 1.016105 | 3 |
| 15 | 5 | 3 | 6000 | 18000 | 8 | 90 | 9 | 4 | 1 | Straight | 3078 | 1.949318 | 3 | 7 | 1 | Four of a Kind | 4252.5 | 1.410935 | 3 | 4 | 5 | Straight | 4573.714106 | 1.311844 | 6 | 6 | 3 | Full House | 4920.75 | 1.219326 | 3 |
| 16 | 6 | 1 | 8000 | 24000 | 9 | 99 | 9 | 4 | 1 | Straight | 3078 | 2.59909 | 3 | 8 | 1 | Straight Flush | 7560 | 1.058201 | 3 | 4 | 5 | Straight | 4573.714106 | 1.749126 | 6 | 6 | 3 | Full House | 4920.75 | 1.625768 | 3 |
| 17 | 6 | 2 | 10000 | 30000 | 9 | 108 | 10 | 4 | 1 | Straight | 3361.5 | 2.974862 | 3 | 8 | 1 | Straight Flush | 7560 | 1.322751 | 3 | 4 | 5 | Straight | 4573.714106 | 2.186407 | 6 | 6 | 3 | Full House | 4920.75 | 2.032211 | 3 |
| 18 | 6 | 3 | 12000 | 36000 | 9 | 117 | 10 | 4 | 1 | Straight | 3361.5 | 3.569835 | 3 | 8 | 1 | Straight Flush | 7560 | 1.587302 | 3 | 4 | 5 | Straight | 4573.714106 | 2.623688 | 7 | 6 | 3 | Full House | 5511.24 | 2.177368 | 3 |
| 19 | 7 | 1 | 16000 | 48000 | 10 | 127 | 10 | 4 | 1 | Straight | 3361.5 | 4.75978 | 4 | 8 | 1 | Straight Flush | 8640 | 1.851852 | 4 | 4 | 5 | Straight | 5515.361128 | 2.900989 | 7 | 6 | 4 | Full House | 7440.174 | 2.150487 | 3 |
| 20 | 7 | 2 | 20000 | 60000 | 10 | 137 | 11 | 5 | 1 | Flush | 3780 | 5.291005 | 4 | 8 | 1 | Straight Flush | 8640 | 2.314815 | 4 | 5 | 5 | Flush | 5963.764472 | 3.353586 | 7 | 7 | 4 | Four of a Kind | 17437.907813 | 1.146927 | 3 |
| 21 | 7 | 3 | 24000 | 72000 | 10 | 147 | 11 | 5 | 1 | Flush | 3780 | 6.349206 | 4 | 8 | 1 | Straight Flush | 8640 | 2.777778 | 4 | 5 | 5 | Flush | 5963.764472 | 4.024304 | 8 | 7 | 4 | Four of a Kind | 19181.698594 | 1.251193 | 3 |
| 22 | 8 | 1 | 32000 | 96000 | 11 | 158 | 11 | 5 | 2 | Flush | 5103 | 6.270821 | 4 | 8 | 2 | Straight Flush | 11664 | 2.743484 | 4 | 5 | 5 | Flush | 5963.764472 | 5.365738 | 8 | 7 | 4 | Four of a Kind | 19181.698594 | 1.668257 | 3 |
| 23 | 8 | 2 | 40000 | 120000 | 11 | 169 | 11 | 5 | 2 | Flush | 5103 | 7.838526 | 4 | 8 | 2 | Straight Flush | 11664 | 3.429355 | 4 | 5 | 5 | Flush | 5963.764472 | 6.707173 | 8 | 7 | 4 | Four of a Kind | 19181.698594 | 2.085321 | 3 |
| 24 | 8 | 3 | 48000 | 144000 | 11 | 180 | 12 | 5 | 2 | Flush | 5485.725 | 8.749983 | 4 | 8 | 2 | Straight Flush | 11664 | 4.115226 | 4 | 5 | 5 | Flush | 5963.764472 | 8.048608 | 8 | 7 | 5 | Four of a Kind | 25895.293102 | 1.853619 | 3 |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| OK line | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Bursts / normal = how many card bursts one normal enemy needs. Lower is better. The chart's dashed line is the 'still OK' limit from Inputs. | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Gold so far is spent before each wave. Purchases per category use the strategy's share of all gold earned until then. | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
