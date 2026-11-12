# Game Systems – Rubric & Requirements Checklist (Solo)

Sources: `00_Course/Assignments/*.pdf` (briefs + rubric) and `00_Course/Lessons/*.pdf` (lesson slides).
Target: **Excellent** on all three projects = at least **two** Excellent criteria per rubric row.
Team size: **Solo** → use the lower number wherever the rubric says "two/three*" or "three/four*".

---

## 1. Virtual Pet

### Deliverables
- [ ] WebGL build on itch.io, fully playable without external instructions
- [ ] Project files as a compressed folder (everything except `\Library`)
- [ ] Demonstration video, max 3 minutes: pet behaviour + short explanation of the state machine (Lesson 002, "Submission and Finalization")
- [ ] Design report (PDF) with:
  - [ ] Personality description (likes/dislikes, serious or playful, reactions to items, reactions to how the player treats it, idle behaviour)
  - [ ] Visual design rationale (shape, colour, proportions, materials → personality)
  - [ ] State diagram (states, actions, emotions, transition events/conditions)
  - [ ] Playtest summary + iterations (feedback, observations, changes and why)

### Hard requirements
- [ ] Infinite loop: no Game Over, no end state
- [ ] At least 2 FSM states and 2 core parameters; parameters drive behaviour and transitions
- [ ] At least 2 distinct player interactions with clearly different outcomes
- [ ] Pet built from primitives (Unity / Blender / Maya / ProBuilder primitives – Lesson 001). No existing assets for the pet itself; licensed props are allowed
- [ ] All animations made by hand in the Unity Animation window
- [ ] Basic animation for every state
- [ ] Visual design shows baby schema, colour theory and shape theory
- [ ] Playtested with at least 10 people (Lesson 002); players perceive a personality

### Excellent criteria (need ≥ 2) – planned choice marked ★
- [ ] ★ Emotional development during play: internal state changes over time
- [ ] ★ Deliberately designed to encourage caretaking behaviour
- [ ] Refined, cohesive audiovisual design with a strong sense of personality
- [ ] ★ Procedural/adaptive elements: parameters significantly change personality or appearance

### Lesson theory to apply (cite in report)
MDA framework · core loop (feed → react → mood → respond) · shape language · colour language · baby schema (Lorenz) · pareidolia & anthropomorphism · operant conditioning (Skinner) · FSM / Animator · moods from combined parameters · EmoGBL (Plass et al., 2015) · behavioural matrix · blend trees · non-verbal communication (Ekman, 1992) · flour sack exercise.

---

## 2. Progression Game

### Deliverables
- [ ] Working prototype (digital) + clear screenshots of setup and core components + all source files
- [ ] Demonstration video, max 3 minutes, .mp4: full play from start to finish, core loop and key mechanics
- [ ] Design report, max 4 pages:
  - [ ] Game design description: goal and resources
  - [ ] Gameplay analysis: Machinations diagram (screenshot + link + short analysis) OR Excel balance model (.xlsx + graph screenshot + short analysis)
  - [ ] Playtest report: sessions, balance findings, design iterations
- [ ] Credit LLM use and other resources

### Sufficient (required)
- [ ] Clear and substantial progression system
- [ ] Player collects at least 2 resources and has strategic options to use them
- [ ] Progression loop modelled in a functional Machinations diagram or dynamic Excel model
- [ ] Balanced for ~10 minutes of fair, engaging play

### Excellent criteria (need ≥ 2) – plan to hit all 3
- [ ] ★ Strategy balance shown in a graph built from comparative playthrough data
- [ ] ★ Values (upgrade costs, yields) based on analysis of an existing game
- [ ] ★ Mechanics and progression fit the theme and reinforce the narrative

---

## 3. Sandbox

### Deliverables
- [ ] Unity build (WebGL or multiplatform) on itch.io
- [ ] Demonstration video, max 8 minutes, .mp4: gameplay + main systems
- [ ] Design report, max 6 pages:
  - [ ] How the game is played and the goal
  - [ ] Overview of building items, their properties and why they were added
  - [ ] Playtest summary + iterations
  - [ ] Screenshots of creative builds made by players

### Hard requirements
- [ ] At least 3 object categories that combine with each other
- [ ] At least 3 variations per category
- [ ] Decide which interactions use specific rules and which use shared system rules (e.g. physics)

### Design – Excellent (need ≥ 2)
- [ ] ★ Systematic/procedural approach to create many meaningful item variations
- [ ] ★ Player experience defined and refined through multiple prototype–test–improve cycles
- [ ] ★ Building blocks can trigger chain reactions with unscripted outcomes

### Implementation – Excellent (need ≥ 2)
- [ ] ★ Smooth, well-structured UX from start to finish
- [ ] ★ Modular building-block architecture for efficient variations
- [ ] Unintended behaviours, edge cases or emergent player actions refined into stable mechanics

---

## Folder map
| Folder | Content |
|---|---|
| `00_Course/` | Assignment briefs, lesson slides, this checklist |
| `0X_<Project>/1_Plan` | Concept, task plan, schedule |
| `0X_<Project>/2_Design` | Sketches, state diagrams, models (Machinations/Excel), report drafts |
| `0X_<Project>/3_Playtest` | Forms, raw results, analysis |
| `0X_<Project>/4_Video` | Script, raw footage, edit project |
| `0X_<Project>/Final` | Only the files that get submitted |
| `Assets/<Project>/` | Unity content per game (one scene each) |
| `_to_delete/` | Unity template leftovers – safe to delete |
