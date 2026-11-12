## Read me
| Wind Pet balance sheet |
| --- |
| NaN |
| What is in here |
| Parameters: every number of the bar system, as it is in the game now, with the script that uses it. |
| Bands: the bands of each bar (Energy, Mood, Fullness, Temperature) and what the player sees in each. |
| Rules: how the bars push each other, in the order the game applies them. |
| Simulation: 15 minutes of play, 5 seconds per row, driven only by formulas. A chart shows the four bars. |
| NaN |
| How to use it |
| Yellow cells with blue text are inputs. Change them, and every other sheet updates. |
| In Simulation, type how many food puffs the pet eats in a row of the 'Puffs eaten' column to test feeding. |
| When a number feels right, copy it into the same field in Unity (Data/PetConfig, or the component named in 'Where in the code'). |
| NaN |
| Simplifications in the simulation |
| No band margin (the game waits 2 points before switching a band). |
| Flying and resting are averaged per step (moveShare), instead of real trips around the room. |
| An Angry pet always counts as a full rampage; the slow build-up of rage is left out. |
| Food puffs are eaten the moment they are made (no flying to them). |
| One feeding tool per run: pick it with feedFoodPerPuff and feedTempPerPuff at the top of Simulation. |
| NaN |
| Colours |
| Blue text on yellow = input you can change. Green text = number taken from the Parameters sheet. Black = formula. |

## Parameters
| Wind Pet: tuning numbers | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 |
| --- | --- | --- | --- | --- | --- |
| Blue values are the numbers used in the game right now (10 Nov 2026). Change them here: the Bands, Rules and Simulation sheets update by themselves. Rates are per second of game time, bars run from 0 to 100. | NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN | NaN |
| Group | Parameter | Value | Unit | What it does | Where in the code |
| Start values | NaN | NaN | NaN | NaN | NaN |
| NaN | startEnergy | 80 | points | Energy when the game starts. | PetConfig.startEnergy |
| NaN | startFullness | 70 | points | Fullness when the game starts (100 = full). | PetConfig.startFullness |
| NaN | startMood | 50 | points | Mood when the game starts. | PetConfig.startMood |
| NaN | startTemperature | 50 | points | Temperature when the game starts (50 = comfortable). | PetConfig.startTemperature |
| NaN | startMoney | 20 | euro | Money when the game starts. | PetConfig.startMoney |
| Energy bands | NaN | NaN | NaN | NaN | NaN |
| NaN | sleepBelow | 10 | energy | Below this the pet falls asleep. | PetConfig.sleepBelow |
| NaN | tiredBelow | 40 | energy | Below this the pet is Tired (flies slower, mood drops). | PetConfig.tiredBelow |
| NaN | energeticAbove | 75 | energy | Above this the pet is Energetic. | PetConfig.energeticAbove |
| NaN | wakeAbove | 40 | energy | A sleeping pet needs at least this much energy to wake up... | PetConfig.wakeAbove |
| NaN | wakeMoodAbove | 40 | mood | ...and at least this much mood, so it never wakes up into a storm. | PetConfig.wakeMoodAbove |
| Mood bands | NaN | NaN | NaN | NaN | NaN |
| NaN | angryBelow | 10 | mood | Below this the pet is Angry: storm, lightning, tornado. | PetConfig.angryBelow |
| NaN | sadBelow | 35 | mood | Below this the pet is Sad. | PetConfig.sadBelow |
| NaN | happyAbove | 75 | mood | Above this the pet is Happy. | PetConfig.happyAbove |
| Fullness bands | NaN | NaN | NaN | NaN | NaN |
| NaN | hungryBelow | 40 | fullness | Below this the pet is Hungry. | PetConfig.hungryBelow |
| Temperature bands | NaN | NaN | NaN | NaN | NaN |
| NaN | coldBelow | 35 | temperature | Below this the pet is Cold. | PetConfig.coldBelow |
| NaN | hotAbove | 65 | temperature | Above this the pet is Hot. | PetConfig.hotAbove |
| Band switching | NaN | NaN | NaN | NaN | NaN |
| NaN | bandMargin | 2 | points | A bar must pass a border by this much before the band changes (stops flickering). Not used in the simulation. | PetConfig.bandMargin |
| Fullness | NaN | NaN | NaN | NaN | NaN |
| NaN | fullnessDecay | 0.12 | per second | Fullness lost every second, always (awake or asleep). | PetConfig.fullnessDecay |
| NaN | fullnessPerEnergy | 2 | fullness / energy | Extra fullness paid for every point of energy restored while awake. | PetConfig.fullnessPerEnergy |
| Energy gain | NaN | NaN | NaN | NaN | NaN |
| NaN | restRegen | 0.12 | per second | Energy restored while awake and resting (standing still). Paid with fullness. | PetConfig.restRegen |
| NaN | restRegenUntil | 90 | energy | Resting restores energy up to this level. | PetConfig.restRegenUntil |
| NaN | sleepRegen | 1 | per second | Energy restored while asleep. Free: no fullness is used. | PetConfig.sleepRegen |
| Energy cost | NaN | NaN | NaN | NaN | NaN |
| NaN | costPerMetre | 0.15 | energy / metre | Energy used for every metre the pet flies on its own. | PetEnergyUse.costPerMetre |
| NaN | stormEnergyDrain | 0.8 | per second | Extra energy used per second by a full storm (rampage). | PetConfig.stormEnergyDrain |
| NaN | coldCostMultiplier | 1.5 | x | All energy costs are multiplied by this when the pet is Cold. | PetConfig.coldCostMultiplier |
| NaN | normalCostMultiplier | 1 | x | Multiplier at normal temperature. | PetConfig.normalCostMultiplier |
| NaN | hotCostMultiplier | 0.7 | x | Multiplier when the pet is Hot. | PetConfig.hotCostMultiplier |
| Mood rules | NaN | NaN | NaN | NaN | NaN |
| NaN | energeticAndFullMood | 0.6 | per second | Mood gained while Energetic AND Full. | PetConfig.energeticAndFullMood |
| NaN | hungryMood | -0.3 | per second | Mood change while Hungry. | PetConfig.hungryMood |
| NaN | tiredMood | -0.2 | per second | Mood change while Tired (not while asleep). | PetConfig.tiredMood |
| NaN | hungryAndTiredExtra | -0.2 | per second | Extra mood change when Hungry AND Tired at the same time. | PetConfig.hungryAndTiredExtra |
| NaN | moodDriftToNormal | 0.15 | per second | When no rule is active, mood moves back to 50 at this speed. | PetConfig.moodDriftToNormal |
| NaN | sleepMoodGain | 1 | per second | Mood gained while asleep (replaces all other mood rules). | PetConfig.sleepMoodGain |
| Food | NaN | NaN | NaN | NaN | NaN |
| NaN | fanFoodPerPuff | 12 | fullness | Fullness gained per fan puff (free food fills less). | ToolItem.food (fan) |
| NaN | dryerFoodPerPuff | 24 | fullness | Fullness gained per dryer puff (paid food fills more). | ToolItem.food (dryer) |
| NaN | eatMoodBonus | 5 | mood | Mood gained per food puff eaten (not when already full). | PetConfig.eatMoodBonus |
| NaN | fanTempPerPuff | -6 | temperature | Temperature change per fan puff (cools). | ToolItem.temperatureChange (fan) |
| NaN | dryerTempPerPuff | 6 | temperature | Temperature change per dryer puff (warms). | ToolItem.temperatureChange (dryer) |
| NaN | dryerCostPerPuff | 1 | euro | Money paid per dryer puff. The fan is free. | ToolItem.costPerPuff (dryer) |
| NaN | fullAbove | 95 | fullness | A full pet still eats, but above this a puff only changes its temperature (no fullness, no mood). | PetFeeding.fullAbove |
| NaN | maxPuffs | 8 | puffs | Most puffs that can float in the room at once. | ToolBelt.maxPuffs |
| NaN | hitMoodPenalty | 15 | mood | Mood lost when a thrown item hits the pet. | ToolBelt.hitMoodPenalty |
| Movement | NaN | NaN | NaN | NaN | NaN |
| NaN | moveSpeed | 1.4 | m/s | Normal flying speed. | PetWander.moveSpeed |
| NaN | tiredSpeed | 0.6 | x | Speed multiplier when Tired. | PetMoodBehaviour.tiredSpeed |
| NaN | stormSpeed | 3.2 | x | Speed multiplier in a rampage (Angry storm). | PetMoodBehaviour.stormSpeed |
| NaN | foodSpeedMultiplier | 1.5 | x | Speed multiplier when flying to food. | PetWander.foodSpeedMultiplier |
| Storm feel (not simulated) | NaN | NaN | NaN | NaN | NaN |
| NaN | stormFromMood | 40 | mood | Below this mood the pet starts to grow and darken. | PetLook.stormFromMood |
| NaN | rageBuildTime | 20 | seconds | Seconds of being Angry until the rage (size, speed) is full. | PetLook.rageBuildTime |
| NaN | maxScale | 1.7 | x | The storm never grows bigger than this. | PetLook.maxScale |
| NaN | maxBoltsPerSecond | 8 | bolts / s | Lightning at full storm. | PetStorm.maxBoltsPerSecond |
| NaN | maxDebris | 18 | props | Most props spinning around the storm at once. | PetStorm.maxDebris |

## Bands
| Bands of each bar | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 |
| --- | --- | --- | --- | --- |
| Every border comes from the Parameters sheet. Each bar is its own small state machine; the four run side by side. | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN |
| Bar | Band | From | To | What the player sees |
| Energy | Sleep | 0 | 10 | Sinks onto the desk, eyes closed, wind spins very slowly. Wakes at wakeAbove energy AND wakeMoodAbove mood. |
| Energy | Tired | 10 | 40 | Flies slower; mood drops. |
| Energy | Normal | 40 | 75 | Normal flying. |
| Energy | Energetic | 75 | 100 | With a full stomach, mood goes up fast. |
| Mood | Angry | 0 | 10 | Storm: dark, big, purple lightning, tornado that carries props, rampage, angry eyes. Burns energy. |
| Mood | Sad | 10 | 35 | Bigger, darker, cloudier wind (no lightning). |
| Mood | Normal | 35 | 75 | Normal look. |
| Mood | Happy | 75 | 100 | Normal look (happy reactions still to come). |
| Fullness | Hungry | 0 | 40 | Mood drops; flies to food puffs. |
| Fullness | Full | 40 | 100 | Still eats; above fullAbove a puff only changes the temperature. |
| Temperature | Cold | 0 | 35 | Turns deep blue; every action costs coldCostMultiplier x energy. |
| Temperature | Normal | 35 | 65 | Normal colour. |
| Temperature | Hot | 65 | 100 | Turns red-orange; actions cost hotCostMultiplier x energy. |

## Rules
| How the bars affect each other | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 |
| --- | --- | --- | --- | --- | --- |
| The Rate column links to the Parameters sheet. Order matters: the game applies them every frame in this order. | NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN | NaN |
| # | When | Effect | Rate | Unit | Code |
| 1 | Always | Fullness drains | -0.12 | per second | PetStateMachine.Update |
| 2 | Asleep | Energy comes back for free | 1 | per second | PetStateMachine.Update |
| 3 | Asleep | Mood comes back (no other mood rule runs) | 1 | per second | PetStateMachine.Update |
| 4 | Awake, resting, energy < restRegenUntil | Energy comes back... | 0.12 | per second | PetStateMachine.RestoreEnergy |
| 5 | ...same moment | ...and is paid with fullness | -0.24 | per second | PetStateMachine.RestoreEnergy |
| 6 | Flying on its own | Energy is used per metre (x temperature multiplier) | -0.15 | per metre | PetEnergyUse |
| 7 | Angry storm (rampage) | Extra energy used (x temperature multiplier) | -0.8 | per second | PetMoodBehaviour |
| 8 | Cold | Every energy cost is multiplied | 1.5 | x | PetStateMachine.CostMultiplier |
| 9 | Hot | Every energy cost is multiplied | 0.7 | x | PetStateMachine.CostMultiplier |
| 10 | Energetic AND Full | Mood goes up | 0.6 | per second | PetStateMachine.ApplyMoodRules |
| 11 | Hungry | Mood goes down | -0.3 | per second | PetStateMachine.ApplyMoodRules |
| 12 | Tired | Mood goes down | -0.2 | per second | PetStateMachine.ApplyMoodRules |
| 13 | Hungry AND Tired | Mood goes down extra (adds to the two above) | -0.2 | per second | PetStateMachine.ApplyMoodRules |
| 14 | No mood rule active | Mood moves back to 50 | 0.15 | per second | PetStateMachine.ApplyMoodRules |
| 15 | Eats a fan puff (not full) | Fullness up | 12 | per puff | PetFeeding |
| 16 | Eats a dryer puff (not full) | Fullness up | 24 | per puff | PetFeeding |
| 17 | Eats any puff (not full) | Mood up | 5 | per puff | PetFeeding |
| 18 | Eats a puff while fullness >= fullAbove | Only the temperature changes | NaN | NaN | PetFeeding |
| 19 | Eats a fan puff | Temperature down | -6 | per puff | PetFeeding |
| 20 | Eats a dryer puff | Temperature up; player pays | 6 | per puff | PetFeeding / ToolBelt |
| 21 | Hit by a thrown item | Mood down | -15 | per hit | ThrownItem |
| 22 | Energy < sleepBelow | Falls asleep; the storm stops and its props burst outwards | NaN | NaN | PetStateMachine.UpdateBands, PetStorm |
| 23 | Asleep | Wakes only when energy >= wakeAbove AND mood >= wakeMoodAbove | NaN | NaN | PetStateMachine.UpdateBands |

## Simulation
| 15-minute simulation | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 | Unnamed: 6 | Unnamed: 7 | Unnamed: 8 | Unnamed: 9 | Unnamed: 10 | Unnamed: 11 | Unnamed: 12 | Unnamed: 13 | Unnamed: 14 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Each row is one step. Yellow cells are yours to change: the behaviour assumptions below and the 'Puffs eaten' column (how many food puffs the pet eats at that moment). Simplified: no band margin, an Angry pet is counted as a full rampage. | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Step | 5 | seconds per row | NaN | Summary | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| moveShare | 0.5 | share of awake time the pet is flying (rest = resting) | NaN | First hungry (s) | 130 | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| avgFlySpeed | 0.8 | average speed while flying, m/s (wander slows down near targets) | NaN | First sad (s) | 490 | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| feedFoodPerPuff | 12 | fullness per puff for the feeding tool (=fanFoodPerPuff or =dryerFoodPerPuff) | NaN | First angry (s) | 575 | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| feedTempPerPuff | -6 | temperature per puff for the feeding tool (=fanTempPerPuff or =dryerTempPerPuff) | NaN | First asleep (s) | 605 | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | Time Angry (s) | 90 | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | Time Happy (s) | 280 | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | Puffs eaten | 3 | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | Dryer cost if dryer (euro) | 3 | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Time (s) | Puffs eaten | Asleep | Energy | Fullness | Mood | Temperature | Energy band | Hunger band | Mood band | Temp band | Energy cost /s | Energy gain /s | Mood change /s | Fullness change /s |
| 0 | 0 | 0 | 80 | 70 | 50 | 50 | Energetic | Full | Normal | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 5 | 0 | 0 | 80 | 68.8 | 53 | 50 | Energetic | Full | Normal | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 10 | 0 | 0 | 80 | 67.6 | 56 | 50 | Energetic | Full | Normal | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 15 | 0 | 0 | 80 | 66.4 | 59 | 50 | Energetic | Full | Normal | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 20 | 0 | 0 | 80 | 65.2 | 62 | 50 | Energetic | Full | Normal | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 25 | 0 | 0 | 80 | 64 | 65 | 50 | Energetic | Full | Normal | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 30 | 0 | 0 | 80 | 62.8 | 68 | 50 | Energetic | Full | Normal | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 35 | 0 | 0 | 80 | 61.6 | 71 | 50 | Energetic | Full | Normal | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 40 | 0 | 0 | 80 | 60.4 | 74 | 50 | Energetic | Full | Normal | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 45 | 0 | 0 | 80 | 59.2 | 77 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 50 | 0 | 0 | 80 | 58 | 80 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 55 | 0 | 0 | 80 | 56.8 | 83 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 60 | 0 | 0 | 80 | 55.6 | 86 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 65 | 0 | 0 | 80 | 54.4 | 89 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 70 | 0 | 0 | 80 | 53.2 | 92 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 75 | 0 | 0 | 80 | 52 | 95 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 80 | 0 | 0 | 80 | 50.8 | 98 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 85 | 0 | 0 | 80 | 49.6 | 100 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 90 | 0 | 0 | 80 | 48.4 | 100 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 95 | 0 | 0 | 80 | 47.2 | 100 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 100 | 0 | 0 | 80 | 46.0 | 100 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 105 | 0 | 0 | 80 | 44.8 | 100 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 110 | 0 | 0 | 80 | 43.6 | 100 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 115 | 0 | 0 | 80 | 42.4 | 100 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 120 | 0 | 0 | 80 | 41.2 | 100 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 125 | 0 | 0 | 80 | 40.0 | 100 | 50 | Energetic | Full | Happy | Normal | 0.06 | 0.06 | 0.6 | -0.24 |
| 130 | 0 | 0 | 80 | 38.8 | 100 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 135 | 0 | 0 | 80 | 37.6 | 98.5 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 140 | 0 | 0 | 80 | 36.4 | 97 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 145 | 0 | 0 | 80 | 35.2 | 95.5 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 150 | 0 | 0 | 80 | 34.0 | 94 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 155 | 0 | 0 | 80 | 32.8 | 92.5 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 160 | 0 | 0 | 80 | 31.6 | 91 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 165 | 0 | 0 | 80 | 30.4 | 89.5 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 170 | 0 | 0 | 80 | 29.2 | 88 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 175 | 0 | 0 | 80 | 28.0 | 86.5 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 180 | 0 | 0 | 80 | 26.8 | 85 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 185 | 0 | 0 | 80 | 25.6 | 83.5 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 190 | 0 | 0 | 80 | 24.4 | 82 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 195 | 0 | 0 | 80 | 23.2 | 80.5 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 200 | 0 | 0 | 80 | 22.0 | 79 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 205 | 0 | 0 | 80 | 20.8 | 77.5 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 210 | 0 | 0 | 80 | 19.6 | 76 | 50 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 215 | 0 | 0 | 80 | 18.4 | 74.5 | 50 | Energetic | Hungry | Normal | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 220 | 0 | 0 | 80 | 17.2 | 73 | 50 | Energetic | Hungry | Normal | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 225 | 0 | 0 | 80 | 16.0 | 71.5 | 50 | Energetic | Hungry | Normal | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 230 | 0 | 0 | 80 | 14.8 | 70 | 50 | Energetic | Hungry | Normal | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 235 | 0 | 0 | 80 | 13.6 | 68.5 | 50 | Energetic | Hungry | Normal | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 240 | 1 | 0 | 80 | 24.4 | 72 | 44 | Energetic | Hungry | Normal | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 245 | 1 | 0 | 80 | 35.2 | 75.5 | 38 | Energetic | Hungry | Happy | Normal | 0.06 | 0.06 | -0.3 | -0.24 |
| 250 | 1 | 0 | 80 | 46.0 | 79 | 32 | Energetic | Full | Happy | Cold | 0.09 | 0.06 | 0.6 | -0.24 |
| 255 | 0 | 0 | 79.85 | 44.8 | 82 | 32 | Energetic | Full | Happy | Cold | 0.09 | 0.06 | 0.6 | -0.24 |
| 260 | 0 | 0 | 79.7 | 43.6 | 85 | 32 | Energetic | Full | Happy | Cold | 0.09 | 0.06 | 0.6 | -0.24 |
| 265 | 0 | 0 | 79.55 | 42.4 | 88 | 32 | Energetic | Full | Happy | Cold | 0.09 | 0.06 | 0.6 | -0.24 |
| 270 | 0 | 0 | 79.4 | 41.2 | 91 | 32 | Energetic | Full | Happy | Cold | 0.09 | 0.06 | 0.6 | -0.24 |
| 275 | 0 | 0 | 79.25 | 40.0 | 94 | 32 | Energetic | Full | Happy | Cold | 0.09 | 0.06 | 0.6 | -0.24 |
| 280 | 0 | 0 | 79.1 | 38.8 | 97 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 285 | 0 | 0 | 78.95 | 37.6 | 95.5 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 290 | 0 | 0 | 78.8 | 36.4 | 94 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 295 | 0 | 0 | 78.65 | 35.2 | 92.5 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 300 | 0 | 0 | 78.5 | 34.0 | 91 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 305 | 0 | 0 | 78.35 | 32.8 | 89.5 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 310 | 0 | 0 | 78.2 | 31.6 | 88 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 315 | 0 | 0 | 78.05 | 30.4 | 86.5 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 320 | 0 | 0 | 77.9 | 29.2 | 85 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 325 | 0 | 0 | 77.75 | 28.0 | 83.5 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 330 | 0 | 0 | 77.6 | 26.8 | 82 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 335 | 0 | 0 | 77.45 | 25.6 | 80.5 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 340 | 0 | 0 | 77.3 | 24.4 | 79 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 345 | 0 | 0 | 77.15 | 23.2 | 77.5 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 350 | 0 | 0 | 77.0 | 22.0 | 76 | 32 | Energetic | Hungry | Happy | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 355 | 0 | 0 | 76.85 | 20.8 | 74.5 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 360 | 0 | 0 | 76.7 | 19.6 | 73 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 365 | 0 | 0 | 76.55 | 18.4 | 71.5 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 370 | 0 | 0 | 76.4 | 17.2 | 70 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 375 | 0 | 0 | 76.25 | 16.0 | 68.5 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 380 | 0 | 0 | 76.1 | 14.8 | 67 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 385 | 0 | 0 | 75.95 | 13.6 | 65.5 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 390 | 0 | 0 | 75.8 | 12.4 | 64 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 395 | 0 | 0 | 75.65 | 11.2 | 62.5 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 400 | 0 | 0 | 75.5 | 10.0 | 61 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 405 | 0 | 0 | 75.35 | 8.8 | 59.5 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 410 | 0 | 0 | 75.2 | 7.6 | 58 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 415 | 0 | 0 | 75.05 | 6.4 | 56.5 | 32 | Energetic | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 420 | 0 | 0 | 74.9 | 5.2 | 55 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 425 | 0 | 0 | 74.75 | 4.0 | 53.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 430 | 0 | 0 | 74.6 | 2.8 | 52 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 435 | 0 | 0 | 74.45 | 1.6 | 50.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 440 | 0 | 0 | 74.3 | 0.4 | 49 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0.06 | -0.3 | -0.24 |
| 445 | 0 | 0 | 74.15 | 0 | 47.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 450 | 0 | 0 | 73.7 | 0 | 46 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 455 | 0 | 0 | 73.25 | 0 | 44.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 460 | 0 | 0 | 72.8 | 0 | 43 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 465 | 0 | 0 | 72.35 | 0 | 41.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 470 | 0 | 0 | 71.9 | 0 | 40 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 475 | 0 | 0 | 71.45 | 0 | 38.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 480 | 0 | 0 | 71.0 | 0 | 37 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 485 | 0 | 0 | 70.55 | 0 | 35.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 490 | 0 | 0 | 70.1 | 0 | 34 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 495 | 0 | 0 | 69.65 | 0 | 32.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 500 | 0 | 0 | 69.2 | 0 | 31 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 505 | 0 | 0 | 68.75 | 0 | 29.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 510 | 0 | 0 | 68.3 | 0 | 28 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 515 | 0 | 0 | 67.85 | 0 | 26.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 520 | 0 | 0 | 67.4 | 0 | 25 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 525 | 0 | 0 | 66.95 | 0 | 23.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 530 | 0 | 0 | 66.5 | 0 | 22 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 535 | 0 | 0 | 66.05 | 0 | 20.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 540 | 0 | 0 | 65.6 | 0 | 19 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 545 | 0 | 0 | 65.15 | 0 | 17.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 550 | 0 | 0 | 64.7 | 0 | 16 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 555 | 0 | 0 | 64.25 | 0 | 14.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 560 | 0 | 0 | 63.8 | 0 | 13 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 565 | 0 | 0 | 63.35 | 0 | 11.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 570 | 0 | 0 | 62.9 | 0 | 10 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 575 | 0 | 0 | 62.45 | 0 | 8.5 | 32 | Normal | Hungry | Angry | Cold | 2.208 | 0 | -0.3 | -0.12 |
| 580 | 0 | 0 | 51.41 | 0 | 7 | 32 | Normal | Hungry | Angry | Cold | 2.208 | 0 | -0.3 | -0.12 |
| 585 | 0 | 0 | 40.37 | 0 | 5.5 | 32 | Normal | Hungry | Angry | Cold | 2.208 | 0 | -0.3 | -0.12 |
| 590 | 0 | 0 | 29.33 | 0 | 4 | 32 | Tired | Hungry | Angry | Cold | 2.208 | 0 | -0.7 | -0.12 |
| 595 | 0 | 0 | 18.29 | 0 | 0.5 | 32 | Tired | Hungry | Angry | Cold | 2.208 | 0 | -0.7 | -0.12 |
| 600 | 0 | 0 | 7.25 | 0 | 0 | 32 | Tired | Hungry | Angry | Cold | 2.208 | 0 | -0.7 | -0.12 |
| 605 | 0 | 1 | 0 | 0 | 0 | 32 | Sleep | Hungry | Angry | Cold | 0 | 1 | 1 | -0.12 |
| 610 | 0 | 1 | 5 | 0 | 5 | 32 | Sleep | Hungry | Angry | Cold | 0 | 1 | 1 | -0.12 |
| 615 | 0 | 1 | 10 | 0 | 10 | 32 | Sleep | Hungry | Sad | Cold | 0 | 1 | 1 | -0.12 |
| 620 | 0 | 1 | 15 | 0 | 15 | 32 | Sleep | Hungry | Sad | Cold | 0 | 1 | 1 | -0.12 |
| 625 | 0 | 1 | 20 | 0 | 20 | 32 | Sleep | Hungry | Sad | Cold | 0 | 1 | 1 | -0.12 |
| 630 | 0 | 1 | 25 | 0 | 25 | 32 | Sleep | Hungry | Sad | Cold | 0 | 1 | 1 | -0.12 |
| 635 | 0 | 1 | 30 | 0 | 30 | 32 | Sleep | Hungry | Sad | Cold | 0 | 1 | 1 | -0.12 |
| 640 | 0 | 1 | 35 | 0 | 35 | 32 | Sleep | Hungry | Normal | Cold | 0 | 1 | 1 | -0.12 |
| 645 | 0 | 1 | 40 | 0 | 40 | 32 | Sleep | Hungry | Normal | Cold | 0 | 1 | 1 | -0.12 |
| 650 | 0 | 0 | 45 | 0 | 45 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 655 | 0 | 0 | 44.55 | 0 | 43.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 660 | 0 | 0 | 44.1 | 0 | 42 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 665 | 0 | 0 | 43.65 | 0 | 40.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 670 | 0 | 0 | 43.2 | 0 | 39 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 675 | 0 | 0 | 42.75 | 0 | 37.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 680 | 0 | 0 | 42.3 | 0 | 36 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 685 | 0 | 0 | 41.85 | 0 | 34.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 690 | 0 | 0 | 41.4 | 0 | 33 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 695 | 0 | 0 | 40.95 | 0 | 31.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 700 | 0 | 0 | 40.5 | 0 | 30 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 705 | 0 | 0 | 40.05 | 0 | 28.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 710 | 0 | 0 | 39.6 | 0 | 27 | 32 | Tired | Hungry | Sad | Cold | 0.054 | 0 | -0.7 | -0.12 |
| 715 | 0 | 0 | 39.33 | 0 | 23.5 | 32 | Tired | Hungry | Sad | Cold | 0.054 | 0 | -0.7 | -0.12 |
| 720 | 0 | 0 | 39.06 | 0 | 20 | 32 | Tired | Hungry | Sad | Cold | 0.054 | 0 | -0.7 | -0.12 |
| 725 | 0 | 0 | 38.79 | 0 | 16.5 | 32 | Tired | Hungry | Sad | Cold | 0.054 | 0 | -0.7 | -0.12 |
| 730 | 0 | 0 | 38.52 | 0 | 13 | 32 | Tired | Hungry | Sad | Cold | 0.054 | 0 | -0.7 | -0.12 |
| 735 | 0 | 0 | 38.25 | 0 | 9.5 | 32 | Tired | Hungry | Angry | Cold | 2.208 | 0 | -0.7 | -0.12 |
| 740 | 0 | 0 | 27.21 | 0 | 6 | 32 | Tired | Hungry | Angry | Cold | 2.208 | 0 | -0.7 | -0.12 |
| 745 | 0 | 0 | 16.17 | 0 | 2.5 | 32 | Tired | Hungry | Angry | Cold | 2.208 | 0 | -0.7 | -0.12 |
| 750 | 0 | 0 | 5.13 | 0 | 0 | 32 | Tired | Hungry | Angry | Cold | 2.208 | 0 | -0.7 | -0.12 |
| 755 | 0 | 1 | 0 | 0 | 0 | 32 | Sleep | Hungry | Angry | Cold | 0 | 1 | 1 | -0.12 |
| 760 | 0 | 1 | 5 | 0 | 5 | 32 | Sleep | Hungry | Angry | Cold | 0 | 1 | 1 | -0.12 |
| 765 | 0 | 1 | 10 | 0 | 10 | 32 | Sleep | Hungry | Sad | Cold | 0 | 1 | 1 | -0.12 |
| 770 | 0 | 1 | 15 | 0 | 15 | 32 | Sleep | Hungry | Sad | Cold | 0 | 1 | 1 | -0.12 |
| 775 | 0 | 1 | 20 | 0 | 20 | 32 | Sleep | Hungry | Sad | Cold | 0 | 1 | 1 | -0.12 |
| 780 | 0 | 1 | 25 | 0 | 25 | 32 | Sleep | Hungry | Sad | Cold | 0 | 1 | 1 | -0.12 |
| 785 | 0 | 1 | 30 | 0 | 30 | 32 | Sleep | Hungry | Sad | Cold | 0 | 1 | 1 | -0.12 |
| 790 | 0 | 1 | 35 | 0 | 35 | 32 | Sleep | Hungry | Normal | Cold | 0 | 1 | 1 | -0.12 |
| 795 | 0 | 1 | 40 | 0 | 40 | 32 | Sleep | Hungry | Normal | Cold | 0 | 1 | 1 | -0.12 |
| 800 | 0 | 0 | 45 | 0 | 45 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 805 | 0 | 0 | 44.55 | 0 | 43.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 810 | 0 | 0 | 44.1 | 0 | 42 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 815 | 0 | 0 | 43.65 | 0 | 40.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 820 | 0 | 0 | 43.2 | 0 | 39 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 825 | 0 | 0 | 42.75 | 0 | 37.5 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 830 | 0 | 0 | 42.3 | 0 | 36 | 32 | Normal | Hungry | Normal | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 835 | 0 | 0 | 41.85 | 0 | 34.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 840 | 0 | 0 | 41.4 | 0 | 33 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 845 | 0 | 0 | 40.95 | 0 | 31.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 850 | 0 | 0 | 40.5 | 0 | 30 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 855 | 0 | 0 | 40.05 | 0 | 28.5 | 32 | Normal | Hungry | Sad | Cold | 0.09 | 0 | -0.3 | -0.12 |
| 860 | 0 | 0 | 39.6 | 0 | 27 | 32 | Tired | Hungry | Sad | Cold | 0.054 | 0 | -0.7 | -0.12 |
| 865 | 0 | 0 | 39.33 | 0 | 23.5 | 32 | Tired | Hungry | Sad | Cold | 0.054 | 0 | -0.7 | -0.12 |
| 870 | 0 | 0 | 39.06 | 0 | 20 | 32 | Tired | Hungry | Sad | Cold | 0.054 | 0 | -0.7 | -0.12 |
| 875 | 0 | 0 | 38.79 | 0 | 16.5 | 32 | Tired | Hungry | Sad | Cold | 0.054 | 0 | -0.7 | -0.12 |
| 880 | 0 | 0 | 38.52 | 0 | 13 | 32 | Tired | Hungry | Sad | Cold | 0.054 | 0 | -0.7 | -0.12 |
| 885 | 0 | 0 | 38.25 | 0 | 9.5 | 32 | Tired | Hungry | Angry | Cold | 2.208 | 0 | -0.7 | -0.12 |
| 890 | 0 | 0 | 27.21 | 0 | 6 | 32 | Tired | Hungry | Angry | Cold | 2.208 | 0 | -0.7 | -0.12 |
| 895 | 0 | 0 | 16.17 | 0 | 2.5 | 32 | Tired | Hungry | Angry | Cold | 2.208 | 0 | -0.7 | -0.12 |
| 900 | 0 | 0 | 5.13 | 0 | 0 | 32 | Tired | Hungry | Angry | Cold | 2.208 | 0 | -0.7 | -0.12 |
