## README
| Poker TD - reference game analysis |
| --- |
| NaN |
| Purpose: starting values for the Poker TD economy, taken from two existing games (rubric: Excellent criterion 2). |
| Balatro (LocalThunk, 2024): ratios between poker hands and how much a hand grows per upgrade level. |
| Kingdom Rush (Ironhide Game Studio, 2011): tower costs, upgrade cost growth, enemy health vs gold bounty, wave growth. |
| Blue numbers = values copied from a source (see the source line under each table). Black = formulas. |
| Sheet 'Takeaways' turns the findings into proposed starting numbers for our game. These are a first guess, to be tuned in the model and in playtests. |
| NaN |
| Sources (APA): |
| Balatro Wiki. (n.d.). Planet cards. Fandom. https://balatrogame.fandom.com/wiki/Planet\_Cards |
| Prima Games. (2024). All poker hands and their chips and mults in Balatro. https://primagames.com/gaming/all-poker-hands-and-their-chips-and-mults-in-balatro |
| Kingdom Rush Wiki. (n.d.). Category: Ranged towers. Fandom. https://kingdomrushtd.fandom.com/wiki/Category:Ranged\_Towers |
| Kingdom Rush Wiki. (n.d.). Lives. Fandom. https://kingdomrushtd.fandom.com/wiki/Lives |
| Wikily. (n.d.). Kingdom Rush: Enemies (Goblin, Orc, Wulf, Shaman, Dark Knight, Ogre). https://wikily.gg/kingdom-rush/enemies |
| Wikily. (n.d.). Kingdom Rush: Southport. https://wikily.gg/kingdom-rush/stages/southport |

## Balatro Hands
| Balatro: poker hands (level 1) and growth per level | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 | Unnamed: 6 | Unnamed: 7 | Unnamed: 8 | Unnamed: 9 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Hand | Base chips | Base mult | Score L1 (chips x mult) | x Pair | +Chips / level | +Mult / level | Score L2 | Score L3 | Growth L1->L2 |
| High Card | 5 | 1 | 5 | 0.25 | 10 | 1 | 30 | 75 | 5 |
| Pair | 10 | 2 | 20 | 1 | 15 | 1 | 75 | 160 | 2.75 |
| Two Pair | 20 | 2 | 40 | 2 | 20 | 1 | 120 | 240 | 2 |
| Three of a Kind | 30 | 3 | 90 | 4.5 | 20 | 2 | 250 | 490 | 1.777778 |
| Straight | 30 | 4 | 120 | 6 | 30 | 3 | 420 | 900 | 2.5 |
| Flush | 35 | 4 | 140 | 7 | 15 | 2 | 300 | 520 | 1.142857 |
| Full House | 40 | 4 | 160 | 8 | 25 | 2 | 390 | 720 | 1.4375 |
| Four of a Kind | 60 | 7 | 420 | 21 | 30 | 3 | 900 | 1560 | 1.142857 |
| Straight Flush | 100 | 8 | 800 | 40 | 40 | 4 | 1680 | 2880 | 1.1 |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Source: Prima Games (2024) for base chips and mult; Balatro Wiki (n.d.) for planet card level-ups. Card ranks also add chips in Balatro; not included here. | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Finding: at level 1 a Flush is worth 7x a Pair and a Straight Flush 40x. One level up adds +275% to a Pair but only +110% to a Straight Flush, so upgrades help cheap hands most. | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |

## KR Towers
| Kingdom Rush: Archer tower line (cost, damage, speed) | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 | Unnamed: 6 | Unnamed: 7 | Unnamed: 8 | Unnamed: 9 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Level | Cost (gold) | Total spent | Cost x previous | Damage min | Damage max | Avg damage | Attack interval (s) | DPS | DPS per gold spent |
| 1 Archer Tower | 70 | 70 | NaN | 4 | 6 | 5 | 0.8 | 6.25 | 0.089286 |
| 2 Marksmen | 110 | 180 | 1.571429 | 7 | 11 | 9 | 0.6 | 15 | 0.083333 |
| 3 Sharpshooter | 160 | 340 | 1.454545 | 10 | 16 | 13 | 0.5 | 26 | 0.076471 |
| 4a Rangers Hideout | 230 | 570 | 1.4375 | 13 | 19 | 16 | 0.4 | 40 | 0.070175 |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Average cost growth | NaN | NaN | 1.487825 | NaN | NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Source: Kingdom Rush Wiki (n.d.), Category: Ranged towers. Selling returns 60% of gold spent after the first wave (Wikily, n.d.). | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Finding: each upgrade costs about 1.5x the previous one, and DPS per gold drops a little at every level, so upgrading is a choice, not a free win. | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |

## KR Enemies
| Kingdom Rush: enemy health vs gold bounty (Normal difficulty) | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 | Unnamed: 6 |
| --- | --- | --- | --- | --- | --- | --- |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Enemy | Health | Gold bounty | Gold per 100 HP | Lives taken | Speed | Armor |
| Goblin | 20 | 3 | 15 | 1 | 46.1 | NaN |
| Wulf | 35 | 5 | 14.285714 | 1 | 96 | NaN |
| Orc | 80 | 9 | 11.25 | 1 | 30.7 | Low (30% physical) |
| Shaman | 100 | 10 | 10 | 1 | 38.4 | None, 85% magic resist |
| Dark Knight | 300 | 25 | 8.333333 | 1 | 26.9 | High (80% physical) |
| Ogre | 800 | 50 | 6.25 | 3 | 26.9 | NaN |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Median | 90 | 9.5 | 10.625 | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Source: Wikily (n.d.), Kingdom Rush enemy pages. Players start a campaign stage with 20 lives (Kingdom Rush Wiki, n.d., Lives). | NaN | NaN | NaN | NaN | NaN | NaN |
| Finding: weak enemies pay more gold per HP (Goblin 15 per 100 HP) than big ones (Ogre 6.25). Killing many small enemies is the main income. | NaN | NaN | NaN | NaN | NaN | NaN |

## KR Waves
| Kingdom Rush: Southport (stage 1) wave growth | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 | Unnamed: 6 | Unnamed: 7 | Unnamed: 8 | Unnamed: 9 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Wave | Goblins | Orcs | Total HP | Total bounty | HP x previous | Cumulative bounty | NaN | Goblin HP | 20.0 |
| 1 | 3 | 0 | 60 | 9 | NaN | 274 | NaN | Orc HP | 80.0 |
| 2 | 6 | 0 | 120 | 18 | 2 | 292 | NaN | Goblin gold | 3.0 |
| 3 | 9 | 0 | 180 | 27 | 1.5 | 319 | NaN | Orc gold | 9.0 |
| 4 | 4 | 1 | 160 | 21 | 0.888889 | 340 | NaN | Starting gold | 265.0 |
| 5 | 0 | 3 | 240 | 27 | 1.5 | 367 | NaN | NaN | NaN |
| 6 | 10 | 4 | 520 | 66 | 2.166667 | 433 | NaN | NaN | NaN |
| 7 | 16 | 0 | 320 | 48 | 0.615385 | 481 | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Source: Wikily (n.d.), Southport. Starting gold 265, 20 lives, 7 waves. | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Finding: total wave HP grows fast but not smoothly (it dips at waves 4 and 7). Starting gold buys 3 basic towers, so the first choices matter. | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |

## Takeaways
| Takeaways: proposed starting values for Poker TD (to be tuned) | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 |
| --- | --- | --- | --- | --- |
| NaN | NaN | NaN | NaN | NaN |
| Rule in our game | Taken from | Reference value | Proposed start value | Why |
| Flush damage vs Pair | Balatro score L1 | 7 | 7 | Keep Balatro's ratios between hands, so strong hands feel worth the setup. |
| Pair damage (base) | Balatro Pair score | 20 | 20 | Set so a Pair kills an early enemy in about one burst. |
| Hand upgrade: growth per level | Balatro L1->L2 (Pair) | 2.75 | NaN | Balatro levels are very strong (+275% for a Pair). Our upgrade size is decided in the model, but weak hands should still grow faster. |
| Tower build cost (gold) | KR Archer Tower | 70 | 70 | Same as Kingdom Rush. |
| Tower upgrade cost growth | KR average | 1.487825 | 1.487825 | Each upgrade about 1.5x the last one. |
| Starting gold | KR Southport | 265 | 280 | Rounded to 4 towers, so the first wave starts with a 4-card hand. |
| Gold per 100 enemy HP | KR median | 10.625 | 10.625 | Small enemies pay more per HP than big ones. |
| Base lives | KR campaign | 20 | 20 | Same as Kingdom Rush. |
| Wave HP growth | KR Southport average | 1.445157 | NaN | Endless mode needs a smooth curve; fit a % growth per wave in the model so an average run ends near 10 minutes. |
| NaN | NaN | NaN | NaN | NaN |
| Green = linked from the analysis sheets. Blue = our own choice. Empty proposed values are decided in the economy model. | NaN | NaN | NaN | NaN |
