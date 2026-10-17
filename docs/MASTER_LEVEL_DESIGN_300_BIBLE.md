# 🐾 Cut Free Pets: Master 300-Level Design Bible & Player Experience Guide

> **Core Philosophy**: *"Just One More Level!"*  
> Every level must be intuitive to understand in **2 seconds**, tactile and satisfying to swipe in **5 seconds**, and delightfully rewarding with celebratory juice upon completion.

---

## 📑 Table of Contents
1. [Core Design Philosophy & Retention Psychology](#1-core-design-philosophy--retention-psychology)
2. [Physics, Coordinate Space & Visual Rules](#2-physics-coordinate-space--visual-rules)
3. [The 4-Step Nintendo Learning Progression](#3-the-4-step-nintendo-learning-progression)
4. [Master 12-Chapter Roadmap Overview](#4-master-12-chapter-roadmap-overview)
5. [Chapter 1: The Sunny Treehouse (Levels 1–25) — Complete Level-by-Level Bible](#chapter-1-the-sunny-treehouse-levels-125)
6. [Chapter 2: The Floating Meadow (Levels 26–50) — Balloons & Anti-Gravity](#chapter-2-the-floating-meadow-levels-2650)
7. [Chapter 3: Playful Puppy Park (Levels 51–75) — Barker the Dog & Bouncy Cushions](#chapter-3-playful-puppy-park-levels-5175)
8. [Chapter 4: The Old Watermill & Pulleys (Levels 76–100) — Counterweights](#chapter-4-the-old-watermill--pulleys-levels-76100)
9. [Chapter 5: Sunny Playground & Seesaws (Levels 101–125) — Kinetic Catapults](#chapter-5-sunny-playground--seesaws-levels-101125)
10. [Chapter 6: Best Friends Forever - Dual Pets (Levels 126–150) — Co-Op Puzzles](#chapter-6-best-friends-forever---dual-pets-levels-126150)
11. [Chapter 7: The Enchanted Hollow Portals (Levels 151–175) — Momentum Warping](#chapter-7-the-enchanted-hollow-portals-levels-151175)
12. [Chapter 8: Autumn Breeze & Wind Streams (Levels 176–200) — Pip the Bunny](#chapter-8-autumn-breeze--wind-streams-levels-176200)
13. [Chapter 9: Sliding Timber Tracks (Levels 201–225) — Moving Pulleys](#chapter-9-sliding-timber-tracks-levels-201225)
14. [Chapter 10: Honeycomb Garden & Sticky Drops (Levels 226–250) — Surface Friction](#chapter-10-honeycomb-garden--sticky-drops-levels-226250)
15. [Chapter 11: Starlight Canopy & Anti-Gravity (Levels 251–275) — Cosmic Beams](#chapter-11-starlight-canopy--anti-gravity-levels-251275)
16. [Chapter 12: Grand Master Treehouse (Levels 276–300) — The Final Masterpieces](#chapter-12-grand-master-treehouse-levels-276300)
17. [Implementation & Automated Generator Mapping](#17-implementation--automated-generator-mapping)

---

## 1. Core Design Philosophy & Retention Psychology

### 🎯 The "Dopamine Loop" Formula
To keep players addicted and playing for hours:
1. **Instant Clarity (0–2s)**: The player opens the level and immediately grasps the situation. No walls of text or confusing UI.
2. **Tactile Satisfaction (Swipe Feel)**: Cutting a rope produces a glowing particle trail, realistic snapping physics, and a melodic harp ping.
3. **Pacing & Flow (Near Misses & Momentum)**: The cage swings close to obstacles and swoops through stars with satisfying pendulum physics.
4. **Rescued Euphoria (The Landing Celebration)**:
   - Wood cage breaks apart with juicy debris popping outward.
   - Rescued pet lands centered on the platform upright (`Quaternion.identity`).
   - Happy pet celebration animation triggers with golden star explosion and fanfare.
5. **Instant Restart (0.01s)**: If a cut fails, restarting happens instantaneously with zero loading screens.

---

## 2. Physics, Coordinate Space & Visual Rules

### 📐 Standard Coordinate Space (Camera Orthographic Size = 5.0f, Aspect = 9:16)
* **Screen Visible Range**: $X \in [-2.8f, +2.8f]$, $Y \in [-5.0f, +5.0f]$.
* **Ceiling Anchors (Hangers)**: Located at $Y \in [2.5f, 4.2f]$.
* **Default Goal Platform (`Ground_Platform`)**:
  * Default Position: $(0.0f, -3.85f, 0f)$
  * Scale: $(0.60f, 0.68f, 1f)$ with BoxCollider2D $(3.9f, 0.5f)$
  * Centering Rule: Rescued pet always lands at `center.x` of the platform.
* **Pet Cage**:
  * Size: Scale $(0.70f, 0.70f, 1f)$
  * Hook position (`TopRing_Hook`): local $(0f, 1.65f, 0f)$
* **Visual Palette Compliance (`AGENTS.md`)**:
  * Sky: Sunny daylight sky blue (`#85CCFA` / `{r: 0.52, g: 0.80, b: 0.98, a: 1}`).
  * Woodwork: Honey oak (`#D2691E`) & caramel timber (`#8D5B28`).
  * Stars: Luminous golden amber (`#FFD54F`).

---

## 3. The 4-Step Nintendo Learning Progression

Every Chapter of 25 levels follows this exact 4-phase rhythm:
* **Phase 1: Introduce (Levels 1–5)**:
  * Introduces the core mechanic in its purest, simplest form.
  * Impossible to fail by accident; builds player confidence and smiles.
* **Phase 2: Develop (Levels 6–15)**:
  * Combines the mechanic with earlier elements (e.g. ropes + ramps + swing momentum).
  * 3 stars require thoughtful timing or swing apex snips.
* **Phase 3: Twist (Levels 16–22)**:
  * Introduces surprising angles, narrow gaps, sequential cuts, or quick-reaction timing.
* **Phase 4: Mastery / Boss Challenge (Levels 23–25)**:
  * The grand culmination of the chapter! Requires mastery of both timing and strategy.
  * Maximum visual spectacle and star satisfaction.

---

## 4. Master 12-Chapter Roadmap Overview

| Chapter | Levels | World Name | Signature Mechanic / Component | Featured Pet |
| :---: | :---: | :--- | :--- | :---: |
| **1** | 1 – 25 | **The Sunny Treehouse** | Basic Ropes (Single, Dual, Triple), Slides & Guide Planks | 🐱 Cat (Whiskers) |
| **2** | 26 – 50 | **The Floating Meadow** | 🎈 Anti-Gravity Balloons (Float & Tap-to-Pop) | 🐱 Cat (Whiskers) |
| **3** | 51 – 75 | **Playful Puppy Park** | 🐶 New Pet: Dog (Heavy Physics) + 🍄 Bouncy Cushions | 🐶 Dog (Barker) |
| **4** | 76 – 100 | **The Old Watermill** | ⚙️ Pulley Systems & 🪨 Heavy Stone Counterweights | 🐱 Cat & 🐶 Dog |
| **5** | 101 – 125 | **Sunny Playground** | ⚖️ Wooden Seesaws & Springboard Catapults | 🐱 Cat & 🐶 Dog |
| **6** | 126 – 150 | **Best Friends Forever** | 🐱🐶 Dual Pets on Screen (Co-op Puzzles) | 🐱 Cat + 🐶 Dog |
| **7** | 151 – 175 | **The Enchanted Hollow** | 🌀 Magic Tree Trunk Portals (Momentum Warping) | 🐱 Cat & 🐶 Dog |
| **8** | 176 – 200 | **Autumn Breeze** | 🌪 Wind Streams / Bellows & Floating Bubble Traps | 🐰 Bunny (Pip) |
| **9** | 201 – 225 | **Sliding Timber Tracks** | 🚡 Sliding Ceiling Rope Anchors (Moving Pulleys) | 🐱 Cat & 🐶 Dog |
| **10** | 226 – 250 | **Honeycomb Garden** | 🍯 Sticky Honey Drops (Friction Surfaces) & Bee Obstacles | 🐱 Cat, 🐶 Dog, 🐰 Bunny |
| **11** | 251 – 275 | **Starlight Canopy** | ⚡ Gravity Inversion Beams & Timed Dissolving Ropes | 🐱🐶 Dual Pets |
| **12** | 276 – 300 | **Grand Master Treehouse** | 👑 Full Symphony: All Mechanics Harmonized | All Pets |

---

## Chapter 1: The Sunny Treehouse (Levels 1–25)
*Theme: Warm honey-oak branches, gentle afternoon breeze, introductory physics.*  
*Featured Pet: 🐱 Cat (Whiskers).*

### Phase 1: Introduce (Levels 1–5)

#### Level 1 — "First Snip" (Tutorial: The Pure Drop)
* **Visual**: Cozy honey-oak canopy, sunshine filtering through leaves.
* **Cage Position**: $(0.0, 1.5, 0)$
* **Hanger**: 1 anchor at $(0.0, 3.8, 0)$
* **Platform**: Ground Platform at $(0.0, -3.85, 0)$
* **Stars**: $(0.0, 0.4)$, $(0.0, -1.0)$, $(0.0, -2.4)$ (Direct straight line).
* **Solution**: 1 quick swipe cuts the rope $\rightarrow$ cage drops vertically $\rightarrow$ collects all 3 stars $\rightarrow$ lands centered $\rightarrow$ joyous cat purr!
* **Player Hook**: Zero stress; introduces the glowing blade trail and wood-break sound.

#### Level 2 — "Gentle Hammock" (Dual-Rope Suspension)
* **Visual**: Symmetrical hammock suspension in the middle of the screen.
* **Cage Position**: $(0.0, 0.8, 0)$
* **Hangers**: 2 anchors at $(-1.8, 3.2, 0)$ and $(+1.8, 3.2, 0)$
* **Platform**: Ground Platform at $(0.0, -3.85, 0)$
* **Stars**: $(-0.8, -0.4)$, $(0.0, -1.4)$, $(+0.8, -0.4)$
* **Solution**: Swipe horizontally across both ropes in 1 fluid stroke $\rightarrow$ cage drops straight down, collecting all 3 stars.
* **Player Hook**: The thrill of a double-cut combo!

#### Level 3 — "The Great Slide Debut" (Introduction to Wooden Ramps)
* **Visual**: Cozy honey-oak canopy, sunshine illuminating a sturdy wooden ramp on the left.
* **Cage Position**: $(-1.15, 2.35, 0)$
* **Hanger**: 1 anchor at $(-1.15, 5.80, 0)$
* **Obstacles**: `Obstacle_Hinged_Ramp` at $(-2.55, -2.10, 0)$ sloping downward from left to right.
* **Platform**: Ground Platform at $(+1.35, -3.85, 0)$
* **Stars**: $(-1.15, 0.15, 0)$ [drop star], $(-0.10, -2.10, 0)$ [mid-slide star], $(+1.35, -2.85, 0)$ [landing star].
* **Solution**: Snip rope $\rightarrow$ cage drops onto slide ramp $\rightarrow$ rolls smoothly down collecting stars $\rightarrow$ settles onto platform with celebratory fanfare!
* **Player Hook**: Introduces rolling physics and the satisfying kinetic wood slide sound.

#### Level 4 — "The Big Swing" (Pendulum Momentum & Timing - No Slides!)
* **Visual**: Asymmetrical two-rope suspension with wide open sky for aerial swinging.
* **Cage Position**: $(+0.90, 1.10, 0)$
* **Hangers**:
  * Hanger 1 (Main High Pivot): $(-0.45, 3.85, 0)$ (Center-Left)
  * Hanger 2 (Right Pull Anchor): $(+1.85, 2.90, 0)$ (Long, clear, comfortable cuttable rope!)
* **Platform**: Ground Platform at $(-0.45, -3.85, 0)$ (Shifted slightly left, perfectly aligned under the pivot!).
* **Obstacles**: None (Pure aerial pendulum swing puzzle!).
* **Stars**: Placed along the safe visible arc (completely clear of resting cage at start):
  * Star 1: $(-0.15, -0.65, 0)$ [bottom curve of swing descent — 2.0+ units away from cage!]
  * Star 2: $(-1.25, 0.40, 0)$ [leftward swing apex — high in the air!]
  * Star 3: $(-0.45, -2.15, 0)$ [direct drop trajectory directly above goal platform]
* **Solution**: Cut right rope $\rightarrow$ cage swoops down and left in a smooth pendulum arc, collecting Star 1 and Star 2 $\rightarrow$ as it swings back through center, cut main rope $\rightarrow$ cage drops straight down through Star 3 and lands upright, perfectly centered on the platform!
* **Player Hook**: Dynamic pendulum velocity, long swipeable ropes, and safe screen boundaries.

#### Level 5 — "Triple Tension Web" (Strategic Cut Order)
* **Visual**: Intricate three-rope spiderweb holding the cage in high tension.
* **Cage Position**: $(0.00, 0.50, 0)$
* **Hangers**: Left Anchor $(-2.00, 2.80, 0)$, Top Anchor $(0.00, 3.80, 0)$, Right Anchor $(+2.00, 2.80, 0)$.
* **Platform**: Ground Platform at $(0.00, -3.85, 0)$
* **Stars**: Left Star $(-1.20, 0.50, 0)$, Right Star $(+1.20, 0.50, 0)$, Drop Star $(0.00, -1.80, 0)$.
* **Solution**: Cut Left rope (swings right into Star 2) $\rightarrow$ cut Right rope (swings back through Star 1) $\rightarrow$ cut Top rope (drops through Star 3 onto platform).
* **Player Hook**: Teaches multi-rope tension release and deliberate cut sequencing.

---

### Phase 2: Develop (Levels 6–15)

#### Level 6 — "Swing & Slide Handoff" (Combo Mechanic)
* **Setup**: High pendulum swing on the left swinging into an elevated slide ramp on the right.
* **Solution**: Cut pull rope $\rightarrow$ cage swings across screen, drops onto slide ramp, rolls smoothly into goal platform.

#### Level 7 — "Triple Tension Web"
* **Cage**: $(0.0, 0.5, 0)$ suspended by 3 ropes: Left $(-2.0, 2.8)$, Top $(0.0, 3.8)$, Right $(+2.0, 2.8)$.
* **Stars**: Left star $(-1.2, 0.5)$, Right star $(+1.2, 0.5)$, Bottom star $(0, -1.8)$.
* **Solution**: Cut Left rope (swings right for star), Cut Right rope (swings center for star), Cut Top rope (drops into final star and platform).

#### Level 8 — "High Slide Leap"
* **Setup**: High elevated slide on top-left $(-1.2, 0.2)$.
* **Solution**: Cage rolls down high slide, leaps off the ramp lip, grabs mid-air star, and lands on ground platform below.

#### Level 9 — "The Double Plank Funnel"
* **Setup**: Two guide planks angled inward at $15^\circ$ creating a wide funnel above the platform.
* **Solution**: Asymmetric swing through stars; the guide planks catch the swinging cage and funnel it cleanly onto the platform.

#### Level 10 — "Apex Snip"
* **Setup**: High hanger at $(0.0, 4.0)$, cage pulled far left to $(-1.8, 1.0)$. Star 3 placed at high right apex $(+1.8, 1.0)$.
* **Solution**: Cut pull rope $\rightarrow$ cage swings across to exact right apex grabbing high star $\rightarrow$ snip main rope at peak to drop safely!

#### Level 11 — "Delayed Release"
* **Setup**: 1 tight rope holding cage horizontally, 1 long loose slack rope attached to ceiling.
* **Solution**: Cut tight rope $\rightarrow$ cage drops into a dramatic slack-rope swing across 3 stars $\rightarrow$ cut at center.

#### Level 12 — "Slide & Roll Drop"
* **Setup**: Horizontal timber plank at $(0.0, -0.5)$. Cage dropped on left edge, rolls across plank, drops off right edge into platform.

#### Level 13 — "Cross-Over Swings"
* **Setup**: Two hangers with ropes crossing over each other in an X-shape.
* **Solution**: Cutting the front rope untangles the cage into a sweeping pendulum.

#### Level 14 — "The Ramp Slalom"
* **Setup**: Top slide slopes right, drops cage onto a lower slide sloping right into the goal platform.

#### Level 15 — "Star Cascade"
* **Setup**: 3 stars arranged in a descending staircase diagonal $(-1.5, 1.5)$, $(0.0, 0.0)$, $(+1.5, -1.5)$.
* **Solution**: Timed sequential cut that converts vertical drop into a diagonal slide trajectory.

---

### Phase 3: Twist (Levels 16–22)

#### Level 16 — "The Narrow Chute"
* **Setup**: Two vertical guide planks leave a narrow 1.3-unit gap.
* **Challenge**: Swing must be stabilized before dropping, or cage bumps the side.

#### Level 17 — "Zig-Zag Pinball Slide"
* **Setup**:
  * Top slide at $(-0.8, 0.8)$ angled down-right ($28^\circ$).
  * Bottom slide at $(+0.8, -1.6)$ angled down-left ($28^\circ$).
  * Ground Platform at $(0.0, -3.85)$.
* **Solution**: Cut top rope $\rightarrow$ rolls down top slide $\rightarrow$ drops smoothly onto bottom slide $\rightarrow$ rolls onto platform!
* **Hook**: Incredible kinetic satisfaction!

#### Level 18 — "Counter-Swing Double"
* **Setup**: 2 stars on left swing, 1 star on right return swing. Player must let the cage swing back and forth before cutting.

#### Level 19 — "Precision Drop Gap"
* **Setup**: Solid timber obstacle with a center opening. Stars positioned above and below the opening.

#### Level 20 — "Speed Roll Banking"
* **Setup**: Steep $38^\circ$ ramp that accelerates the cage into high speed before landing.

#### Level 21 — "Asymmetric Web Pressure"
* **Setup**: 3 ropes with 2 ropes pulling strongly to the right. Cutting the wrong rope sends the cage into the screen boundary. Teaches strategic cut order!

#### Level 22 — "High Sky Arc"
* **Setup**: Stars placed high near the screen ceiling ($Y = 2.5$). Requires launching the cage upward from a high-tension swing.

---

### Phase 4: Mastery (Levels 23–25)

#### Level 23 — "The Grand Treehouse Swing"
* **Setup**: Dual swing + middle curved ramp + 3 stars requiring full swing-to-roll transition.

#### Level 24 — "Pinball Wizard"
* **Setup**: Dual zig-zag slides with dynamic star pickups at both transition lips.

#### Level 25 (Chapter 1 Climax) — "Master of Ropes"
* **Visual**: Grand golden treehouse veranda, sunbeams, celebratory flowers.
* **Setup**:
  * Cage at $(0.0, 1.2, 0)$ suspended by 3 ropes.
  * Sloped ramp at $(+1.0, -1.5)$ leading to centered Ground Platform at $(0.0, -3.85)$.
  * 3 golden stars forming a triumphant victory arc.
* **Solution**: Cut Left rope $\rightarrow$ swoops right through Star 1 $\rightarrow$ cut Top rope $\rightarrow$ drops onto ramp collecting Star 2 $\rightarrow$ rolls down ramp through Star 3 $\rightarrow$ lands dead-center on platform!
* **Celebration**: Extra particle bursts, fireworks, and unlocked Chapter 2 medal!

---

## Chapter 2: The Floating Meadow (Levels 26–50)
*Theme: Sunny flower meadow, floating dandelion puffs, anti-gravity mechanics.*  
*NEW Component: 🎈 Anti-Gravity Balloon (attached to cage; floats upward until player taps to pop).*

### Progression Highlights (Levels 26–50)
* **Level 26 — "Up, Up and Away!"** (Introduce Balloon): Cage starts on ground platform with 1 balloon. Floats upward through 3 vertical stars $\rightarrow$ player taps balloon to pop $\rightarrow$ cage drops safely back onto platform!
* **Level 27 — "Tug of War"**: Balloon pulls UP, bottom rope pulls DOWN. Cut rope $\rightarrow$ float $\rightarrow$ pop at apex.
* **Level 28 — "Ceiling Glide"**: Smooth ceiling plank allows floating cage to glide horizontally across the screen.
* **Level 35 — "Inverted Slide Ramp"**: Floating cage slides UPWARD along the underside of an angled wooden slide!
* **Level 42 — "Double Balloon Lift"**: 2 balloons (fast lift). Pop 1 for slow float, pop 2 to drop.
* **Level 50 (Chapter 2 Climax) — "Skyward Symphony"**: Cut bottom rope $\rightarrow$ float up through 2 stars $\rightarrow$ glide along inverted roof $\rightarrow$ pop balloon at exact apex $\rightarrow$ freefall through Star 3 onto Ground Platform!

---

## Chapter 3: Playful Puppy Park (Levels 51–75)
*Theme: Bright green park, dog agility obstacles, energetic bouncy fun.*  
*NEW Pet: 🐶 Barker the Dog (Heavier mass, higher swing inertia).*  
*NEW Component: 🍄 Bouncy Cushion Bumper (Soft bouncy pad that rebounds pet cages with energetic sound).*

### Progression Highlights (Levels 51–75)
* **Level 51 — "Meet Barker!"** (Introduce Dog): Heavy mass drop onto platform. Feel the punchy weight and solid wood break.
* **Level 52 — "The Mushroom Bounce"**: Drop onto a bouncy cushion; cage rebounds high into the air to collect high stars before landing.
* **Level 60 — "Angled Rebound"**: Drop onto a $45^\circ$ angled cushion that rebounds the dog horizontally across the screen onto a slide.
* **Level 68 — "Trampoline Highway"**: Dual bouncy cushions bouncing the cage back and forth like a pinball machine.
* **Level 75 (Chapter 3 Climax) — "Barker’s Big Bounce"**: Long-rope swing into angled cushion $\rightarrow$ high arc rebound $\rightarrow$ rolls down ramp $\rightarrow$ triumphant dog bark celebration!

---

## Chapter 4: The Old Watermill & Pulleys (Levels 76–100)
*Theme: Rustic timber mill, running stream, rotating cogwheels.*  
*NEW Component: ⚙️ Pulley Cable & 🪨 Heavy Stone Counterweight (cutting stone rope drops stone, raising pet).*

### Progression Highlights (Levels 76–100)
* **Level 76 — "The Counterweight"** (Introduce Pulley): Heavy stone suspended on right, cage on left. Cut stone rope $\rightarrow$ stone falls $\rightarrow$ pulls pet cage up through 3 stars!
* **Level 82 — "Stone Gate"**: Falling stone pulls a wooden gate out of the way, allowing pet to swing freely.
* **Level 90 — "Two Stones, One Pet"**: 2-stage elevator using sequential stone drops.
* **Level 100 (Chapter 4 Climax) — "The Grand Watermill"**: A rotating wooden wheel with 2 ropes attached. Cut at the exact rotational angle for a catapult landing!

---

## Chapter 5: Sunny Playground & Seesaws (Levels 101–125)
*Theme: Treehouse playground, wooden teeter-totters, fun kinetic chain reactions.*  
*NEW Component: ⚖️ Wooden Seesaw (Drops on one side catapult the pet on the other side).*

### Progression Highlights (Levels 101–125)
* **Level 101 — "The Catapult"** (Introduce Seesaw): Pet sits on right seat of seesaw. Drop a stone onto the left seat $\rightarrow$ launches pet high into the air!
* **Level 112 — "Dynamic Tilting Bridge"**: Seesaw acts as a bridge that tilts dynamically as the cage rolls across.
* **Level 125 (Chapter 5 Climax) — "Triple Seesaw Circuit"**: Chain reaction of 3 seesaws launching the cage through stars into a basket landing!

---

## Chapter 6: Best Friends Forever - Dual Pets (Levels 126–150)
*Theme: Cooperative play, teamwork, heartwarming twin rescue.*  
*NEW Feature: 🐱🐶 Both Cat & Dog on Screen Simultaneously!*

### Progression Highlights (Levels 126–150)
* **Level 126 — "Two by Two"** (Introduce Dual Pets): Cat on left rope, Dog on right rope. Cut both ropes to land them side-by-side on the goal platform!
* **Level 135 — "Cooperative Switch"**: Cat swings and bumps a release trigger that frees Dog's cage.
* **Level 142 — "Weight Advantage"**: Heavier Dog dropped first to hold down a seesaw, creating a safe ramp for Cat.
* **Level 150 (Chapter 6 Climax) — "Best Friends Celebration"**: Synchronized dual-swipe cut $\rightarrow$ both cages swing in opposite arcs $\rightarrow$ meet and land together on the goal platform with double confetti fanfare!

---

## Chapter 7: The Enchanted Hollow Portals (Levels 151–175)
*Theme: Mystical hollow oak trunks glowing with soft blue and warm orange fairy lights.*  
*NEW Component: 🌀 Hollow Trunk Portals (Enter Blue Trunk, Exit Orange Trunk with full momentum).*

### Progression Highlights (Levels 151–175)
* **Level 151 — "Magic Trunk"** (Introduce Portals): Drop cage into blue trunk at top $\rightarrow$ instantly shoots out orange trunk horizontally at bottom.
* **Level 162 — "Momentum Multiplier"**: High swing into portal exits with high velocity, launching pet across screen stars.
* **Level 175 (Chapter 7 Climax) — "The Endless Loop"**: Recirculating portal loop that builds launch speed for the final star jump!

---

## Chapter 8: Autumn Breeze & Wind Streams (Levels 176–200)
*Theme: Golden falling leaves, brisk autumn air, wind gusts.*  
*NEW Pet: 🐰 Bunny (Pip - ultra-lightweight, floats farther on breezes).*  
*NEW Component: 🌪 Wind Gust Bellows (Pushes cages horizontally).*

### Progression Highlights (Levels 176–200)
* **Level 176 — "Breeze of Autumn"** (Introduce Wind & Bunny): Wind stream pushes floating bunny cage sideways across a wide star chasm.
* **Level 188 — "Wind & Balloon"**: Balloon lifts pet into a high wind stream that carries it across the ceiling.
* **Level 200 (Chapter 8 Climax) — "The Great Gust"**: Navigating multiple cross-winds with precision cut timing.

---

## Chapter 9: Sliding Timber Tracks (Levels 201–225)
*Theme: Old logging railway, moving wooden pulleys.*  
*NEW Component: 🚡 Moving Overhead Hangers (Anchors slide smoothly along wooden rails).*

### Progression Highlights (Levels 201–225)
* **Level 201 — "Rails in the Sky"** (Introduce Moving Anchors): Rope anchor slides left-to-right along a track. Cut when the anchor reaches the center.
* **Level 215 — "Dynamic Rope Length"**: Track slopes downward, lengthening and speeding up the swing.
* **Level 225 (Chapter 9 Climax) — "The Logging Express"**: Multi-track sequence with handoffs between moving hangers.

---

## Chapter 10: Honeycomb Garden & Sticky Drops (Levels 226–250)
*Theme: Sunny bee garden, warm honeycombs.*  
*NEW Component: 🍯 Sticky Honey Pads (Slows down rolls, allows stopping on steep slopes).*

### Progression Highlights (Levels 226–250)
* **Level 226 — "Sweet Honey"** (Introduce Sticky Surface): Honey pad on ramp slows the cage down, allowing perfect drop timing onto the platform.
* **Level 238 — "Temporary Rest"**: Cage stops on a sticky honey ledge until the player cuts an auxiliary rope to break free.
* **Level 250 (Chapter 10 Climax) — "The Hive Ascent"**: Navigating alternating slick wood and sticky honey pads.

---

## Chapter 11: Starlight Canopy & Anti-Gravity (Levels 251–275)
*Theme: Twilight sky, glowing fireflies, cosmic treehouse canopy.*  
*NEW Component: ⚡ Gravity Inversion Beams & Timed Dissolving Ropes.*

### Progression Highlights (Levels 251–275)
* **Level 251 — "Starlight Lift"** (Introduce Gravity Beam): Passing through glowing beam flips gravity upside-down.
* **Level 265 — "Fading Ropes"**: Ropes with glowing timer rings that dissolve after 4 seconds, requiring fast decision-making.
* **Level 275 (Chapter 11 Climax) — "The Celestial Dance"**: Alternating upward and downward gravity shifts across 3 stars!

---

## Chapter 12: Grand Master Treehouse (Levels 276–300)
*Theme: The pinnacle of Cut Free Pets mastery. Magnificent multi-tiered treehouse sanctuary.*  
*All mechanics harmonized into thrilling, cinematic puzzle masterpieces.*

### Levels 290–300: The Masterpiece Finale
* **Level 290 — "The Clockwork Forest"**: Pulleys, balloons, and seesaws working in clockwork harmony.
* **Level 295 — "Balloons & Portals"**: Floating into a portal, shooting out onto a seesaw!
* **Level 298 — "Dual Orbit"**: Cat and Dog orbiting dual magnetic anchors before dropping in tandem.
* **Level 300 (The Grand Finale) — "The Great Tree Sanctuary"**:
  * **Cast**: All 3 pets (Cat, Dog, Bunny)!
  * **Mechanics**: 4 ropes, 2 balloons, 1 seesaw, 1 portal.
  * **The Chain Reaction**:
    1. Cut top-left rope $\rightarrow$ Dog drops onto seesaw.
    2. Seesaw launches Cat high into air $\rightarrow$ collects Stars 1 & 2.
    3. Cat enters Blue Portal $\rightarrow$ exits Orange Portal over goal platform.
    4. Cut Bunny's balloon rope $\rightarrow$ Bunny floats down through Star 3.
    5. All 3 pets land and celebrate together on the platform with golden fireworks, confetti bursts, and rolling victory credits!

---

## 17. Implementation & Automated Generator Mapping

### 🛠 Prefab Recipe Mapping for `LevelDesignerWindow.cs`
Each of the 300 levels maps directly to a procedural Archetype Recipe in your editor tool:

```csharp
public enum LevelArchetype
{
    SingleDrop,          // Ch 1 (Levels 1-5)
    DualVSwing,          // Ch 1 (Levels 6-15)
    SlideFunnel,         // Ch 1 (Levels 16-25)
    BalloonFloat,        // Ch 2 (Levels 26-50)
    CushionBounce,       // Ch 3 (Levels 51-75)
    PulleyCounterweight, // Ch 4 (Levels 76-100)
    SeesawCatapult,      // Ch 5 (Levels 101-125)
    DualPetCoop,         // Ch 6 (Levels 126-150)
    PortalWarp           // Ch 7+ (Levels 151-300)
}
```

### 📁 Asset & Resource Organization
* All level prefabs are stored in: `Assets/Resources/Levels/Level_XXX.prefab`
* Dynamic loading handled with zero scene duplication by `LevelLoader.LoadLevel(int)`.
* Every level strictly complies with `AGENTS.md` rules:
  1. Single Goal `Ground_Platform` per rescued pet.
  2. Rescued pet strictly centered and upright (`rotation = Quaternion.identity`).
  3. Debris bursts outward with `sortingOrder <= 6`, Pet `sortingOrder >= 14`.
  4. Camera clear color set to Daylight Sky (`#85CCFA`).
