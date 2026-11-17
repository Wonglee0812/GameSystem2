# Dam Rush - Research notes: beaver behaviour (AI) and water simulation

Date: 2026-11-13. Purpose: a base for the beaver colony AI and for the water system in the Sandbox project.
Note: the main beaver experiments (Wilsson, 1971) were read through other papers and articles, not in the original. Those claims are marked "cited in".

---

## Part 1 - Beaver behaviour as a base for the AI

### 1.1 The colony
- A beaver family is one breeding pair, the kits of this year and the yearlings of last year. Older young (two years or more) can also stay, but they do not breed (PBS Nature, n.d.).
- The family has a clear rank order: adults dominate yearlings, and yearlings dominate kits (PBS Nature, n.d.).
- An average family has about four animals (Beavers in England, n.d., based on Müller-Schwarze, 2011).
- Both parents keep up the territory and care for the young. Some division of labour has been seen, but adults of both sexes mostly behave the same (Wilsson, 1971, cited in Steyaert et al., 2015).
- Young beavers stay with the family for about two years before they leave to start their own territory (The Mammal Society, n.d.).

**Game use:** the colony is a family with three age roles (adult, yearling, kit). The player leads the family; the roles decide who can do which job. Kits grow into workers over time, and grown-up young leave or found a new lodge. This gives the colony a life cycle.

### 1.2 What makes beavers build
- Wilsson raised beavers in different habitats. Beavers in running water built dams, while beavers in still, silent water did not. When he played the sound of running water through a speaker, the beavers built on top of the speaker, even on a concrete floor (Wilsson, 1971, cited in Mental Floss, n.d.; Gizmodo, 2015).
- Building activity also depends on the season (most in autumn), pregnancy and the presence of young. Water rising into the burrow starts lodge building, and lodge building starts dam building (Wilsson, 1971, cited in Żurowski, 1992).
- Newer field advice says the water level at a site is the main trigger, and that the "sound of running water" story is too simple (Beaver Trust, n.d.). Beavers also do not try to dam every loud river.
- The leak-repair reflex works well for small daily leaks, but it does not protect a dam against a rare large flood (Hillhouse, 2026).

**Game use:** a "leak alarm" value per dam cell. It is high where water flows fast through or over a dam, and where the pond level drops below what the family needs. Idle beavers move to the strongest alarm and repair it. Because the reflex is simple, a big flood can still beat it. The player's job is to decide what the reflex cannot: where to build, how high, and when to give up a spot.

### 1.3 How a dam is built
- Building usually starts at a narrow point of the stream (Western Producer, n.d.).
- The beavers first lay a base of mud, stones and plants. Then they put longer sticks on top, pointing upstream, and weigh them down with mud and stones. Last, they fill the gaps with plants and mud (Beaver Trust, n.d.).
- The upstream face gets a thick layer of mud, which makes the dam close to watertight (Hillhouse, 2026). An engineer compares the result to a zoned embankment dam: a stable pile of sticks with a sealing layer (Hillhouse, 2026).
- Model tests showed that beaver dams are semi-permeable. A 1.4 m high dam held flows of up to 1.34 m3/s per metre of dam width (Müller & Watling, 2016).
- Beavers keep adding to their dams and lodges, also when it is not needed (Mother Earth News, n.d.).

**Game use:** three material roles match three real roles. Stones and mud make the base, sticks and logs make the body, and mud seals the upstream face. The dam stays a "leaky" structure: water seeps through until it is sealed, and sealing needs repeated work. This supports the Sandbox rule split: physics and water are the shared rules, and "mud seals" and "mud washes away" are specific rules.

### 1.4 Food and trees (central-place foraging)
- Beavers are central-place foragers. They go out from the water, cut trees and branches, and carry them back to their refuge (Haarberg & Rosell, 2006).
- Foraging is most intense close to the water and drops with distance (Haarberg & Rosell, 2006).
- Further from the shore, beavers become more selective and cut larger trees (Raffel et al., 2009).
- They prefer mid-sized trees and avoid very large and very small ones (Raffel et al., 2009).
- Typical foraging ranges reach up to about 20 m from the central place (Salandre et al., 2017, cited in Lapp, 2021).

**Game use:** a utility score per tree, for example value of the tree minus a cost that grows with the distance to water. Beavers cut near the water first. When those trees are gone, they need to walk further, which is slow and risky. A dam that raises the pond brings the water closer to new trees. This links building to food.

### 1.5 Daily rhythm, danger and territory
- Beavers are mainly active at night. They leave the lodge at dusk and come back at dawn (Bartra Cabré et al., 2020).
- In a GPS study of 47 beavers, the mean daily activity time was about 9 h 42 min. Beavers spent less time on land during bright nights, which points to avoiding predators (Bartra Cabré et al., 2020).
- An adult slaps its tail on the water as an alarm signal. The others then go to deep water (Smithsonian's National Zoo, n.d.).
- The dominant pair marks the territory with scent mounds near the border (Bartra Cabré et al., 2020). Resident beavers react to foreign scent mounds near their lodge with aggressive behaviour and more activity (Müller-Schwarze & Heckman, 1980).

**Game use:** a day/night cycle where work mostly happens at night. A "safety" need pulls beavers back towards deep water. A tail-slap event sends the whole family to the lodge, so work stops for a moment. A raised pond (deep water) makes beavers feel safe further from the lodge. Scent mounds can mark the borders of the living area.

### 1.6 Suggested AI structure (to discuss)
- **Needs (utility AI):** safety (distance to deep water), food, rest, and the colony's "alarm" values (leaks, low pond).
- **Jobs:** cut tree, carry material, repair leak, build (follow the player's plan), forage, rest in lodge, flee.
- **Player role:** the player is the leader of the family. The player marks where to build and what is important. The beavers carry it out with their own instincts (repair the loudest leak first, stay near water, forage close first). The player does not place blocks by hand.

---

## Part 2 - Water simulation

### 2.1 Methods used in games
- **Height-field with virtual pipes.** Each grid cell holds a water depth. Water moves to the four neighbours through "pipes", and the flow grows with the difference in surface height. Mei et al. (2007) adapted this model for fast GPU erosion of rivers and rain. It is the model our water already uses.
- **Shallow water equations (SWE).** These also track horizontal velocity, so currents and whirlpools work. Chentanez and Müller (2010) used a specialised shallow water solver for large water bodies in real time, with particles for small details.
- **Breaking waves** do not exist in a pure height-field. Thürey et al. (2007) added them to a shallow water simulation and coupled it with rigid bodies.
- **Timberborn** first used a water sheet laid over the terrain (a 2.5D system) and moved to 3D water in Update 6 in 2024 (Mechanistry, 2024; Timberborn Wiki, n.d.). Its water sources are measured in cubic metres per second (Timberborn Wiki, n.d.).

### 2.2 Problems found in our first water version
1. **No real bed friction.** The old version multiplied every flow by a fixed factor (0.993). That does not depend on depth or speed, so rivers were too fast in some places and too slow in others. Flood models use Manning bed friction instead, where shallow, fast water slows down more (de Almeida et al., 2012).
2. **Water was not pushed away by parts.** When a block landed in water, the water stayed on top of the block. Kellomäki (2013) removes the water that a body occupies and gives it back to the surface around it, so the total volume stays the same.
3. **Forces were measured at only five points.** This ignored the real shape and rotation of a part, and it needed a "pressure scale" of 0.5 as a hack.
4. **Floating parts do not move the water** (one-way coupling). Kellomäki (2013) also leaves out forces on bodies and lists them as future work. Our parts do get forces, but the water does not get a push back.

### 2.3 What was changed (testable in the Water Lab)
- **Manning friction** (toggle in the lab, roughness n as a slider). It is applied semi-implicitly, which keeps it stable in very shallow water (de Almeida et al., 2012).
- **Water displacement** (toggle): when a part fills a wet cell, its water goes to the free neighbour cells (based on Kellomäki, 2013).
- **Pressure on surface patches.** Each part is cut into small surface squares. Every square under water gets the pressure rho x g x depth. This follows the idea of adding up the pressure on the submerged surface of a boat hull (Kerner, 2015). Buoyancy, the side push on a dam and the turning force all come from this one rule.
- **Pressure drag** on patches that face into the current, with the usual 0.5 x rho x Cd x A x v² (Kerner, 2016).
- **Water sources in m3/s**, like Timberborn (Timberborn Wiki, n.d.).

### 2.4 First lab results (2026-11-13)
| Test | Expected | Lab result |
|---|---|---|
| Wood block (600 kg/m3) floats | lift = weight, about 60% under water | lift/weight 0.96 to 1.01, 56% of surface under water |
| Wood log floats | lift = weight | lift/weight 1.01, 58% under water |
| Stone block (2500 kg/m3) | lift = 1000/2500 = 0.40 of weight | 0.40 |
| Mud block (1700 kg/m3) | lift = 0.59 of weight | 0.60 |
| Wall, 1 m wide, water 0.74 m deep on one side | 0.5 x 1000 x 9.81 x 0.74² = 2.69 kN | 2.9 kN |

### 2.5 Still open
- Floating parts do not push the water (no two-way coupling).
- No waves or splashes; a height-field cannot show breaking water (Thürey et al., 2007).
- Water step time is about 3 to 5 ms in the editor for a 64 x 40 grid; many small debris pieces will cost extra force checks.

---

## References

Bartra Cabré, L., Mayer, M., Steyaert, S., & Rosell, F. (2020). Beaver (*Castor fiber*) activity and spatial movement in response to light and weather conditions. *Mammalian Biology, 100*(3), 261–271. https://doi.org/10.1007/s42991-020-00029-7

Beaver Trust. (n.d.). *Beaver impacts*. https://beavertrust.org/beaver-basics/beaver-impacts/

Beavers in England. (n.d.). *Beaver biology*. https://beaversinengland.com/beaver-biology/

Chentanez, N., & Müller, M. (2010). Real-time simulation of large bodies of water with small scale details. In *Proceedings of the 2010 ACM SIGGRAPH/Eurographics Symposium on Computer Animation* (pp. 197–206). Eurographics Association.

de Almeida, G. A. M., Bates, P., Freer, J. E., & Souvignet, M. (2012). Improving the stability of a simple formulation of the shallow water equations for 2-D flood modeling. *Water Resources Research, 48*, W05528. https://doi.org/10.1029/2011WR011570

Gizmodo. (2015). *How to manipulate a beaver*. https://gizmodo.com/how-to-manipulate-a-beaver-1725288635

Haarberg, O., & Rosell, F. (2006). Selective foraging on woody plant species by the Eurasian beaver (*Castor fiber*) in Telemark, Norway. *Journal of Zoology, 270*(2), 201–208.

Hillhouse, G. (2026, August 18). *Are beavers actually good at dams?* Practical Engineering. https://practical.engineering/blog/2026/8/18/are-beavers-actually-good-at-dams

Kellomäki, T. (2013). Interaction with dynamic large bodies in efficient, real-time water simulation. *Journal of WSCG, 21*, 117–126.

Kerner, J. (2015, February 27). *Water interaction model for boats in video games*. Game Developer. https://gamedeveloper.com/programming/water-interaction-model-for-boats-in-video-games

Kerner, J. (2016). *Water interaction model for boats in video games: Part 2*. Game Developer. https://www.gamedeveloper.com/programming/water-interaction-model-for-boats-in-video-games-part-2

Lapp, G. (2021). *Beaver habitat selection* [Topic summary, ZOOL 567]. University of Calgary. https://exhibits.library.ucalgary.ca/files/original/825/ZOOL_567_Dissemination_PDF.pdf

Mechanistry. (2024, October 10). *Wonders of Water: Timberborn Update 6 adds 3D water physics, new skyward engineering, and official mod support* [Press release]. https://mechanistry.com/press/wonders-of-water-timberborn-update-6-adds-3d-water-physics-new-skyward-engineering-and-official-mod-support

Mei, X., Decaudin, P., & Hu, B.-G. (2007). Fast hydraulic erosion simulation and visualization on GPU. In *15th Pacific Conference on Computer Graphics and Applications (PG '07)* (pp. 47–56). IEEE. https://doi.org/10.1109/PG.2007.15

Mental Floss. (n.d.). *The sound of running water puts beavers in the mood to build*. https://www.mentalfloss.com/animals/sound-running-water-puts-beavers-mood-build

Mother Earth News. (n.d.). *Busy beavers: Ecosystem engineers*. https://www.motherearthnews.com/nature-and-environment/busy-beavers-ecosystem-engineers-zmaz10jjzraw

Müller, G., & Watling, J. (2016). The engineering in beaver dams. In *River Flow 2016: Eighth International Conference on Fluvial Hydraulics*. St. Louis, MO, United States.

Müller-Schwarze, D., & Heckman, S. (1980). The social role of scent marking in beaver (*Castor canadensis*). *Journal of Chemical Ecology, 6*, 81–95. https://doi.org/10.1007/BF00987529

PBS Nature. (n.d.). *Beaver fact sheet*. https://www.pbs.org/wnet/nature/blog/beaver-fact-sheet

Raffel, T. R., Smith, N., Cortright, C., & Gatz, A. J. (2009). Central place foraging by beavers (*Castor canadensis*) in a complex lake habitat. *The American Midland Naturalist, 162*(1), 62–73. https://doi.org/10.1674/0003-0031-162.1.62

Steyaert, S. M. J. G., Zedrosser, A., & Rosell, F. (2015). Socio-ecological features other than sex affect habitat selection in the socially obligate monogamous Eurasian beaver. *Oecologia, 179*, 1023–1032. https://doi.org/10.1007/s00442-015-3388-1

Smithsonian's National Zoo. (n.d.). *North American beaver*. https://nationalzoo.si.edu/animals/north-american-beaver

The Mammal Society. (n.d.). *Eurasian beaver*. https://mammal.org.uk/british-mammals/eurasian-beaver

Thürey, N., Müller-Fischer, M., Schirm, S., & Gross, M. (2007). Real-time breaking waves for shallow water simulations. In *15th Pacific Conference on Computer Graphics and Applications (PG '07)* (pp. 39–46). IEEE.

Timberborn Wiki. (n.d.). *Water / Flowing*. https://timberborn.wiki.gg/wiki/Water/Flowing

Western Producer. (n.d.). *Beavers as ecosystem engineers*. https://www.producer.com/?p=281984

Wilsson, L. (1971). Observations and experiments on the ethology of the European beaver (*Castor fiber* L.). *Viltrevy, 8*, 115–266.

Żurowski, W. (1992). Building activity of beavers. *Acta Theriologica, 37*(4), 403–411.
