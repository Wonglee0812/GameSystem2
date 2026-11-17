# Dam Rush - Plan v2: seasons, food, machines and new buildings

Date: 2026-11-13. Status: approved by Wong, Phase A in progress.
Purpose: the second design step after playtest 2. It gives the game a clear goal and adds the building variations the brief asks for (3 categories x 3 variations, chain reactions).

---

## 1. Player experience

The player leads a beaver family through the year. Every season asks for a different kind of work, and the family only lives on if the leader plans ahead: strong dams in spring, growth in summer, a full food store in autumn and enough deep water to get through winter. Water is the tool and the threat at the same time.

## 2. Goal and seasons

- The game is endless. Time runs in years of four seasons; one season is 10 game days (one day = 240 s). For playtests there is a 5x speed.
- The family loses when every beaver has died. Rewards per season will be decided later.

| Season | Goal | Rule in the game |
|---|---|---|
| Spring | Hold the snowmelt floods | Flood waves come from upstream and get stronger every year |
| Summer | Build and grow | Aquatic plants grow: foragers get the most food |
| Autumn | Store food for winter | Foragers take branches from trees; the game shows how much food winter needs |
| Winter | Survive | Ice covers the water and gets thicker. Beavers can only reach food storages whose water is deeper than the ice. If the water at the lodge is too shallow, the family is frozen in |

Why: beavers store branches under water at the end of summer and live from this cache when the pond freezes (Dorset Wildlife Trust, 2024). If the water level drops too low, the ice can trap the beavers in the lodge or freeze the food cache (WXPR, 2019). The floods grow each year, in the same way Timberborn makes each hazard season longer than the one before (The Timberborn Wiki, n.d.).

## 3. Food

- Food is its own resource. Wood is no longer food.
- Each beaver eats about one food per day; kits eat half. A bigger family needs more food.
- A new work group, Foragers, brings food to the food storages.
- Spring and summer: foragers pick aquatic plants in shallow, slow water. About 90% of a beaver's summer diet is grasses, sedges and other aquatic plants (Beavers in England, n.d.).
- Autumn: foragers cut branches from trees, because beavers eat more bark and twigs in autumn and winter (Beavers in England, n.d.).
- No fish traps: beavers do not eat fish (Beavers in England, n.d.). A food farm grows water lilies and duckweed instead (Phase B).
- Hungry beavers lose health; a beaver dies when its health reaches 0. New kits cost food.

## 4. Object categories and variations

| Category | Variations |
|---|---|
| Materials | wood, mud, stone, food; processed: branches, pressed wood, gravel, sand, cement |
| Machines (converters) | wood splitter (wood -> branches), wood press, stone crusher (stone -> gravel), sand sifter (mud -> sand), cement mixer, plus the water wheel that powers them |
| Structures | Dam Mk1, Dam Mk2, Spillway; Base Mk1, Base Mk2; Storage (one type per storage), Nursery, Material lab, Building lab, Food farm |
| Earthworks (tools) | Dig (-0.35 m per order, max -1.5 m), Fell tree |

Rules:
- Dam Mk1 only holds water. Dam Mk2 is stronger and also works as a base; when it breaks, the buildings on it are lost too (50% of the materials come back as piles).
- Spillway: water runs over its top without wear, so the pond keeps a fixed level.
- Base Mk2 pumps water from downstream into the hexes around it (+0.5 m), so buildings on it do not need deep water nearby. The water is moved, not created.
- Storages hold one material each; the player picks the type when ordering.
- Labs unlock new recipes and buildings step by step, paid with food (a tech tree like Plague Inc.).
- Machines need one beaver to run them and a water wheel. Faster water = faster production. The player orders a number of products, or turns on automation so the machine runs until its inputs run out (control in the style of Hay Day).

Shared and specific rules:
- Shared: water physics (depth, flow, pressure) for all structures; the same item and storage rules for all materials.
- Specific: each machine has its own recipe; each dam type has its own height, leak and wear values.

Chain reaction example: a dam upstream makes a deep pond for winter, but slows the stream. The water wheel turns slower, the wood splitter makes fewer branches, and the food store for winter fills slower.

## 5. Architecture

Materials, recipes, machines and buildings are described by data, so a new variation is mostly a new data entry, not new code. This targets the Excellent criteria "modular building-block architecture" and "systematic approach to many variations".

## 6. Map

The map grows from 96 x 72 m to 128 x 96 m (1.8x the area) so there is room for the new buildings. The water grid grows with it; the cost of a water step is checked after the change.

## 7. Phases

| Phase | Content |
|---|---|
| A | Bigger map, seasons, ice, food, foragers, storages per type, health, game over, 5x speed |
| B | Dig, Fell tree, Dam Mk2, Spillway, Base Mk2, building on dams, food farm |
| C | Labs and tech tree, machines, water wheel, new materials |
| D (stretch) | Wolves that ambush beavers far from water |

A playtest follows each phase.

---

## References

Beavers in England. (n.d.). *Diet and feeding activity*. https://beaversinengland.com/beaver-biology/diet-and-feeding-activity/

Dorset Wildlife Trust. (2024, January). *Beaver blog January 2024*. https://dorsetwildlifetrust.org.uk/node/4077

The Timberborn Wiki. (n.d.). *Day and night cycle, time & speed*. https://timberborn.org/articles/day-night-cycle-time-guide

WXPR. (2019, January 21). *A glimpse into the life of a beaver*. https://www.wxpr.org/natural-resources/2019-01-21/a-glimpse-into-the-life-of-a-beaver
