# Dam Rush - Plan v4: seasons, food, machines, floods and flood defences

Date: 2026-11-13. Status: approved by Wong. v4 adds the new flood and the flood defences (section 10). v3 added the Phase C recipes, costs and tech tree (section 8) and the new Phase B costs (section 9). Sections 1-7 are the same as in v2.
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
- Labs unlock new recipes and buildings step by step, paid with food (a tech tree like Plague Inc.; Ndemic Creations, 2012).
- Machines need one beaver to run them and a water wheel. Faster water = faster production. The player orders a number of products, or turns on automation so the machine runs until its inputs run out (control in the style of Hay Day; Supercell, 2012).

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

## 8. Phase C: machines, water wheel and tech tree (decided 2026-11-13)

### 8.1 Materials and their use

| Material | Made by | Used for |
|---|---|---|
| Food (branches) | Foragers, food farm, wood splitter | Eating; paying for research |
| Pressed wood | Wood press | Body of Dam Mk2, pipe of Base Mk2 |
| Gravel | Stone crusher | Crest of the spillway, cement |
| Sand | Sand sifter | Cement, beds of the food farm |
| Cement | Cement mixer | Cap of Dam Mk2, top of Base Mk2 |

Branches count as food, because beavers eat bark and twigs, mostly in autumn and winter (Beavers in England, n.d.). The wood splitter turns spare wood into food for the winter.

### 8.2 Recipes

Times are game seconds for one batch when the water at the wheel runs at 1 m/s. One game day is 240 s.

| Machine | Recipe | Time | Build cost |
|---|---|---|---|
| Wood splitter | 1 wood -> 2 food | 40 s | 4 wood, 2 stone |
| Stone crusher | 1 stone -> 2 gravel | 50 s | 2 wood, 4 stone |
| Sand sifter | 2 mud -> 1 sand | 50 s | 3 wood, 2 mud |
| Wood press | 3 wood -> 1 pressed wood | 80 s | 4 wood, 3 stone |
| Cement mixer | 1 gravel + 1 sand -> 1 cement | 100 s | 2 pressed wood, 2 stone, 2 gravel |

Rules:
- A machine stands on a base (Base, Base Mk2 or Dam Mk2) and needs one operator beaver (new work group "Operators"). The operator fetches the inputs, runs the machine and carries the product to a storage.
- A water wheel (6 wood, 2 stone) stands on flowing water at least 0.3 m deep and drives up to 2 machines on the hexes next to it.
- Machine speed = water speed / 1 m/s, between 0.25x and 2x. Below 0.2 m/s or under ice the machine stops.
- The leader orders batches (+1, +5). After the Automation node a machine can run on "auto" until its inputs or the storage room run out.

### 8.3 Tech tree

Research is paid with food when it starts and then takes time. Each lab researches one node at a time. At the start the player has Dam Mk1, Base, Nursery, Storage, Dig, Fell trees and both labs (Material lab: 6 wood, 4 mud, 2 stone; Building lab: 6 wood, 4 mud, 4 stone).

| Node | Lab | Food | Days | Needs |
|---|---|---|---|---|
| M1 Water wheel | Material | 20 | 1 | - |
| M2 Wood splitter | Material | 25 | 1 | M1 |
| M3 Stone crusher | Material | 35 | 1.5 | M1 |
| M4 Sand sifter | Material | 35 | 1.5 | M1 |
| M5 Wood press | Material | 50 | 2 | M2 |
| M6 Cement mixer | Material | 60 | 2.5 | M3, M4 |
| M7 Automation | Material | 50 | 2 | M2, M3 |
| B1 Food farm | Building | 30 | 1 | M4 |
| B2 Spillway | Building | 50 | 2 | M3 |
| B3 Base Mk2 | Building | 60 | 2.5 | M5, M6 |
| B4 Dam Mk2 | Building | 70 | 3 | M5, M6 |
| B5 Big storage (40 -> 60) | Building | 40 | 1.5 | - |

The wood splitter (M2) comes right after the water wheel, so the family can make food for the next research early.

## 9. Phase B costs with the new materials

| Building | Stages |
|---|---|
| Dam Mk2 | stones + mud (3 stone, 2 mud); pressed wood body (3 pressed wood); mud seal (3 mud); cement cap (2 cement, 1 stone) |
| Spillway | stones + mud (3 stone, 2 mud); gravel crest (4 gravel, 1 mud) |
| Base Mk2 | stones (4 stone, 1 mud); pipe (2 pressed wood, 2 wood); cement top (2 mud, 1 cement) |
| Food farm | mud and sand beds (2 mud, 2 sand); planting (2 mud, 1 wood) |

All numbers are a first version and will be tuned after the playtests.

## 10. Floods v2 and flood defences (decided 2026-11-13)

### 10.1 Why a new flood

In the first playtest the flood was only an event. It lasted about 78 game seconds, it only wore down dams and the family hid by itself. The player could do nothing about it and only lost things. Wong's rule for v4: a flood must hurt, but every kind of damage needs an answer the player can prepare.

### 10.2 The flood

- One snowmelt flood per spring. A forecast comes on day 1 of spring; the waves start one day later.
- The flood lasts about a week: half a day of rising water, six days of waves and half a day of falling water.
- Waves: high water for half a day, lower water (45% of the top) for half a day. The water is fast and strong the whole week.
- Strength: x5 the normal flow in year 1, +35% every year.
- There is no automatic alarm any more. The leader rings the alarm bell.

### 10.3 Damage and answers

| What the flood does | What the player can do |
|---|---|
| Beavers outside the lodge in deep (0.3 m+), fast (1.2 m/s+) water can be swept away and die. Risk per second: kits 4%, yearlings 1.5%, adults 0.4% (more in faster water). | Build an alarm bell and ring it when a wave comes; give the all clear between waves so the family can work and eat. |
| Water above the floor of a building floods it: a storage loses its stock (food every 6 s, other materials every 20 s); machines, labs and nurseries stop. | Read the forecast: buildings that may flood have a red outline. Build levees around them, or build on higher ground or on a Dam Mk2. |
| Water over a dam wears it down. During a flood a dam also wears from the pressure of the water it holds: a full pond before the flood makes a break much more likely. | Open floodgates and use spillways to let water through; lower the pond before the flood; put sandbags on dams (+0.3 m, 30% less wear). |

### 10.4 New constructions

| Construction | Cost | Rule |
|---|---|---|
| Levee | 3 mud | Earth bank, 0.8 m, can stand on dry ground. Open from the start. |
| Floodgate | 3 stone + 2 mud; 4 wood + 1 mud | Dam with a gate, 1.6 m. Open: the water runs over a low sill and the gate does not wear. Closed: holds water like a dam. Open from the start. |
| Sandbags | 2 sand | Put on a finished dam, levee or floodgate: +0.3 m and 30% less wear. They wash out two days after the flood. Needs M4 (sand sifter). |
| Alarm bell | 3 wood; 1 stone + 1 mud | Ring: the whole family swims to the lodge and stays. All clear: back to work. Starving beavers still come out to eat. |

### 10.5 What the flood brings

Each wave that falls leaves piles on the shallow banks near the lodge: duckweed (food), mud and driftwood (wood). After the flood the pond is full, which helps against the winter ice.

### 10.6 First test (2026-11-13, script)

Without the bell, one wave swept away 6 of 12 beavers; a levee broke and the food storage flooded. After the bell was rung, every beaver stayed in the lodge and nobody died. The first wave left 12 food and 4 wood. All numbers are a first version and will be tuned in playtests.

---

## References

Beavers in England. (n.d.). *Diet and feeding activity*. https://beaversinengland.com/beaver-biology/diet-and-feeding-activity/

Dorset Wildlife Trust. (2024, January). *Beaver blog January 2024*. https://dorsetwildlifetrust.org.uk/node/4077

Ndemic Creations. (2012). *Plague Inc.* [Video game]. Ndemic Creations.

Supercell. (2012). *Hay Day* [Video game]. Supercell.

The Timberborn Wiki. (n.d.). *Day and night cycle, time & speed*. https://timberborn.org/articles/day-night-cycle-time-guide

WXPR. (2019, January 21). *A glimpse into the life of a beaver*. https://www.wxpr.org/natural-resources/2019-01-21/a-glimpse-into-the-life-of-a-beaver
