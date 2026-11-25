# Brick Panic - Sandbox plan v1
Written: 2026-11-24. Name, free placement and volcano confirmed by Wong on 2026-11-24. Deadline: about 2026-12-10.
This plan replaces Dam Rush (the beaver game). Dam Rush was dropped after the teacher meeting on 2026-11-24 because its scope was too wide to balance before the deadline.

## 1. Concept
The player gets a random island made of toy bricks. A disaster is coming. The player spends money on two things: people and bricks. At least a set number of people must survive the disaster. The player can spawn only the people that are needed and spend the rest on strong buildings, or build less and spawn more people as a safety margin.

The game takes ideas from three games:

| Reference | What we take |
|---|---|
| Sandcastle (Bubblebird Studio, 2026) | Placing building pieces to defend against the sea; calm building, then watching nature test the build. |
| Natural Disaster Survival (Stickmasterluke, 2011) | Rounds with a disaster on a random map; earthquake, tsunami and volcano as disasters. |
| Totally Accurate Battle Simulator (Landfall Games, 2021) | Placing units with money, then watching physics chaos that the player cannot fully predict. |

The art uses toy bricks for everything (Kenney Brick Kit, CC0). People use the Kenney animated characters (CC0).

## 1b. Core rule (Wong, 2026-11-24)
No material is safe forever. Every disaster can destroy every material; a stronger material only buys time. So every round has losses, and the skill is to lose as little as possible and still reach the goal.

## 2. Round loop
1. Forecast: the next disaster, its strength and its side or location are shown (for example "Tsunami from the east, level 2").
2. Build: the player places, moves and sells bricks. There is no timer. The player presses Start when ready.
3. Spawn: the player buys people and places them anywhere on the map, like placing units in TABS (Landfall Games, 2021).
4. Disaster: about 30-45 s. The player only watches and can move the camera.
5. Count: if survivors >= goal, the player gets money and the next round starts on the same map. If survivors < goal, the run ends.

What stays between rounds: surviving people, standing bricks and rubble. Rubble can be sold. So a round is never a fresh start, and earlier choices matter later.

## 3. Economy (starting numbers, tuned in the Excel model)
- Start money: 100.
- Person: 5 each.
- Goal: 5 survivors in round 1, +1 each round.
- Reward: 40 + 8 for every survivor above the goal. This bonus keeps the people-or-bricks choice meaningful. Without it, the best plan is always "exactly 5 people, the rest walls". A choice is only meaningful when its outcome is clear and it changes the game later (Salen & Zimmerman, 2004).
- Brick price = studs x material price (see 4.1).
- Selling: 50% refund. Map pieces (trees, rocks, huts) can be moved for free or sold the same way.

## 4. Building blocks
The rubric asks for at least 3 categories that combine and at least 3 variations per category.

### 4.1 Category A - Material (4 levels)
| Material | Price per stud | Mass | Joint strength | Floats | Heat |
|---|---|---|---|---|---|
| Plastic | 1 | very low | weak | yes | melts fast |
| Wood | 2 | low | medium | yes | burns |
| Stone | 4 | high | strong | no | safe |
| Steel | 7 | very high | very strong | no | safe |

Each material is good against one disaster and bad against another, but none is immune (see 8, wear). Wood floats away in a tsunami, but it is cheap for an earthquake. Steel holds, but it is expensive and heavy, so a falling steel beam can hurt people too.

### 4.2 Category B - Shape (from the Kenney kit)
Brick 1x1, 1x2, 2x2, 2x4, plate 2x4, slope 2x2, round 1x1. Shape decides size, mass and how many neighbours it can join.

### 4.3 Category C - Special blocks
- Anchor plate: locked to the ground, never pushed off.
- Glue brick: joins to its neighbours 3x stronger.
- Buoy: very light, floats up in water. Tie it to a platform to make a raft.
- Stretch: Water tank. When it breaks, its water cools lava into stone (chain reaction).

### 4.4 Systematic variations
One shape list x one material list makes every block. A block is not a hand-made prefab. It is built at runtime from a Shape (ScriptableObject) and a Material (ScriptableObject): mesh from the shape, colour, mass, joint strength and price from the material. 7 shapes x 4 materials = 28 bricks from 11 data files. Adding a material adds 7 bricks at once.

### 4.5 Shared rules and specific rules (rubric requirement)
- Shared rules (Unity physics): mass, gravity, collisions, joints that break above a force.
- Specific rules (scripts): heat damage per material, floating per material, anchor, glue, water tank.

## 5. Disasters (basic versions)
All three get stronger each round.
- Earthquake: the island shakes (kinematic body moved with a sine wave plus noise; strength set as peak acceleration in g) and slowly leans in one random direction (up to about 6 degrees), then levels out at the end. Shaking plus lean makes bricks and people slide to the edge and fall off. Weak joints break first.
- Tsunami (built 2026-11-24): a wall of see-through blue wave bricks (4 layers, 2 rows) comes from one side and is pushed across the island. Each wave brick starts with 100% power; every brick or person it hits halves it (100 -> 50 -> 25 -> 12 %), and below about 8% it fades away (Wong's design). A hit pushes the target and damages its joints by power x hitDamage / durability, so steel holds better than plastic. Wave bricks do not hit each other (own physics layer), float at full power, climb over steps when blocked, and fall like crashing water when weak. People hit by a wave are swept over.
- Volcano (built 2026-11-24): the volcano is always on the map. For about 20 s it throws lava bombs on ballistic arcs from the crater to random places. A bomb kills a person it hits, damages bricks and sets plastic and wood on fire (they burn away after their heat time; stone and steel do not burn). Bombs cool down and disappear.
- Head hits (2026-11-24): a moving brick on the head (above 1.35 m) makes a person faint (about 6 s) or die; on the body it knocks them over or crushes them. Measured as impulse (mass x speed), so a falling stone brick is far more dangerous than a small plastic one.

## 6. People AI (basic)
TABS-style "active ragdoll" (bodies that balance with physics) is too big for this deadline. Built instead (2026-11-24):
- Movement with physics, not a NavMesh: a NavMesh is fixed, but the island shakes and leans. People are Rigidbody capsules that steer with steering behaviours (Reynolds, 1999), so a quake can push them and they can slide off the edge. They jump onto one-brick steps and go around walls.
- Idle: walk around near the spawn point and stop now and then.
- Panic: grows with the disaster strength (scared people faster), from danger close by, and from people nearby. Panic spreads (Helbing et al., 2000), but others only pull a person up to their own level, so the crowd calms down together after the disaster. In panic people run away from moving or tall bricks, follow the crowd (Reynolds, 1987) and add random noise.
- Each person rolls traits once: bravery, speed, recklessness (chance to run off a drop in panic) and herd feeling. So no two people act the same.
- Toy tumble instead of a bone ragdoll: when hit by a moving brick or when losing balance in a quake, the person falls over as one stiff toy figure and gets up after about 2.5 s.
- Death: a very hard hit ("hit"), a very hard landing ("fall"), or falling off the island ("fell off"). Causes are counted for balancing and the report.
- First test (14 people, level-1 quake): 10 alive / 4 dead in one run, 13 alive / 1 dead in another. Results differ per run, as intended.

## 7. Random map
- The map is a floating island, like the board in Poker TD. Bricks and people that fall off the edge are lost (people count as dead).
- Camera: free fly camera (hold right mouse = look, WASD = move, Q/E = down/up, Shift = fast, wheel = speed).
- A height map from Perlin noise (Perlin, 1985), built from brick columns. A second noise decides the zone: beach, grass and rock. Every map gets one volcano hill, placed away from the map centre. The seed is shown so a map can be replayed.
- Environment pieces are placed by rules: trees and bushes on grass, rocks on rock, a few huts. They are all made of bricks.
- The player can move or sell everything on top of the ground. The ground itself is locked.
- The ground is merged into chunk meshes with no rigidbodies, to keep it fast.

## 8. Physics plan
- During building, bricks snap to a grid and have no physics.
- When the disaster starts, every brick gets a Rigidbody. Touching bricks are joined with a FixedJoint. The break force comes from the weaker material (x3 for glue). Anchors are joined to the ground.
- Grace time: joints cannot break in the first 0.5 s after physics turns on, so the first solver push does not break them.
- Wear: while a disaster runs, every joint loses strength. Speed = disaster intensity x (1 + load, capped at 3x) / material durability. Durability (seconds at intensity 1, no load): plastic 10, wood 20, stone 35, steel 60. A joint at 0 strength breaks.
- After the round, bricks that are still standing freeze again. (To decide: does wear carry over to the next round?)
- Budget (spike, Editor): 853 bricks + 1230 joints run at 110-145 fps while standing, but drop to 11-50 fps when almost all of them collapse at once on a leaning island. Plan for about 500-600 bricks and remove small rubble.

## 9. Controls and UX
- Build like Sandcastle: a hotbar with three tabs (material, shape, special), a ghost preview on the grid, R to rotate, left-click to place, right-click to sell, a move tool and undo.
- Camera: drag to orbit, wheel to zoom (the same idea as Poker TD).
- Flow: main menu -> new map (seed) -> short tips -> rounds -> game over with stats.
- Screenshot key (F12): saves a picture of the build. The report needs screenshots of creative builds made by players.
- Every Inspector field has a [Tooltip] with what it does, the unit and the default value.

## 10. Rubric check
| Rubric item | Where |
|---|---|
| 3 categories that combine, 3+ variations each | 4.1-4.3 |
| Shared rules vs specific rules | 4.5 |
| Excellent: systematic item variations | 4.4 |
| Excellent: chain reactions with unscripted outcomes | 4.3 water tank, 5, 6 |
| Excellent: modular building-block architecture | 4.4, section 11 |
| Excellent: smooth UX from start to finish | 9 |
| Excellent: prototype-test-improve cycles | 12 (two playtests) |

## 11. Code structure
- Folder Assets/BrickPanic/, namespace BrickPanic, its own .asmdef, one scene.
- Data: ShapeDef, MaterialDef, SpecialDef, DisasterDef, Config (all ScriptableObjects).
- Systems: MapGenerator, BuildController, BlockFactory, PhysicsActivator, RoundManager, Economy, Disaster (base) + Earthquake, Tsunami, Volcano, PersonAI, Ragdoll, Hud.

## 12. Schedule (about 2026-12-10)
Weekends (11/28-29 and 12/5-6) are work days for Wong, so they are not counted.

| Day | Work |
|---|---|
| Wed 11/25 | Physics spike: 600 bricks + joints + earthquake, check fps. Shape x Material data. |
| Thu 11/26 | Random map + environment pieces. Build controls. |
| Fri 11/27 | People: spawn, state machine, ragdoll, NavMesh. Round loop + economy. |
| Mon 11/30 | Tsunami + volcano. Special blocks. Playable prototype. |
| Tue 12/1 | Playtest 1 + fixes. |
| Wed 12/2 | HUD, forecast, game over, juice. Excel balance. |
| Thu 12/3 | Playtest 2 + fixes. WebGL build. |
| Fri 12/4 | Buffer. |
| Mon-Wed 12/7-9 | Reports and videos for all three games. |

Poker TD step 6 (CSV + graph) and the Excel update still need time. That is not in this table yet.

## 13. Risks and cuts
- Physics too slow or unstable -> fewer bricks, bigger bricks, freeze bricks far from the disaster.
- NavMesh problems on a changing map -> people run straight toward their target with simple avoidance.
- No time for the volcano eruption -> keep the volcano hill on the map, but only use earthquake + tsunami until the eruption works.
- Cut first: water tank, undo, juice.

## 14. Decisions (Wong, 2026-11-24)
- Name: Brick Panic.
- People can be placed anywhere on the map.
- The volcano hill is always on the map.

## References
Bubblebird Studio. (2026). *Sandcastle* [Demo; PC game]. Future Friends Games.
Helbing, D., Farkas, I., & Vicsek, T. (2000). Simulating dynamical features of escape panic. *Nature, 407*(6803), 487-490. https://doi.org/10.1038/35035023
Juul, J. (2002). The open and the closed: Games of emergence and games of progression. In F. Mäyrä (Ed.), *Computer Games and Digital Cultures Conference Proceedings* (pp. 323-329). Tampere University Press.
Landfall Games. (2021). *Totally Accurate Battle Simulator* [PC game]. Landfall Games.
Perlin, K. (1985). An image synthesizer. *ACM SIGGRAPH Computer Graphics, 19*(3), 287-296. https://doi.org/10.1145/325165.325247
Reynolds, C. W. (1987). Flocks, herds and schools: A distributed behavioral model. *ACM SIGGRAPH Computer Graphics, 21*(4), 25-34. https://doi.org/10.1145/37402.37406
Reynolds, C. W. (1999). Steering behaviors for autonomous characters. In *Proceedings of the Game Developers Conference 1999* (pp. 763-782). Miller Freeman Game Group.
Salen, K., & Zimmerman, E. (2004). *Rules of play: Game design fundamentals*. MIT Press.
Stickmasterluke. (2011). *Natural Disaster Survival* [Online game]. Roblox.
