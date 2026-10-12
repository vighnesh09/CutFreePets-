# Cut Free Pets — Game Concept & Architecture Context

## 1. High-Level Vision
- **Genre**: 2D/2.5D Casual Physics Puzzle Game (inspired by *Cut the Rope* feel).
- **Core Loop**:
  1. A cute pet (currently **Cat**, extensible to **Dog** and other pets) is trapped inside a suspended **Cage**.
  2. The cage is held in mid-air by one or multiple braided physics ropes connected to ceiling/wall **Hanger Pegs**.
  3. The player cuts ropes using natural touch/mouse swipe gestures (zero scissors UI).
  4. The cage drops and interacts with physics obstacles (slopes, bouncers, blocks).
  5. **Win Condition**: The cage lands safely on a designated **Landing Platform**.
     - Cage disappears/opens.
     - Cat stays on the platform and plays a celebratory **Happy Animation**.
     - Star rating (1 to 3 stars collected along the way) is displayed, with a Next Level button.
  6. **Fail Condition**: The cage misses the platform, hits a hazard, or falls out of bounds.
     - Cage disappears/breaks.
     - Cat plays a **Sad Animation**.
     - Level automatically restarts.

---

## 2. Scalability & Level Design Scope
- **Level Count**: Planned for **20 to 30 levels** with progressive difficulty.
- **Level Progression**:
  - *Early Levels*: Single rope, straight drop, straightforward landing platform.
  - *Intermediate Levels*: Multiple ropes (2-3 ropes) requiring strategic cut order and timing to swing the cage.
  - *Advanced Levels*: Obstacles such as angled slope blocks, bouncers, and barriers where the cage must slide or roll down to reach the platform.
- **Multi-Pet Support**:
  - The pet inside the cage is decoupled from the cage physics.
  - Initial character: **Cat** with frame-by-frame sprite animation.
  - Future characters: **Dog**, Bunny, etc. via ScriptableObject pet profiles.

---

## 3. Core Gameplay Components Breakdown

| Component | Responsibility |
|---|---|
| `VerletRope` | Physics-based particle chain using Verlet integration, tension, bending constraints, and dual-half severance. |
| `RopeHanger` | Fixed anchor peg (`Peg_Disc_Albedo`) attached to ceilings/walls that ropes hang from. |
| `RopeRenderer` | Braided texture UV tiling, Catmull-Rom spline curve rendering, tension strain feedback. |
| `SwipeCutBlade` | New Input System touch/mouse drag slice blade with glowing trail; cuts ropes on intersection. |
| `PetCage` | Physical Rigidbody2D cage container holding the pet; connects to multiple ropes; detects landing on goal vs hazards. |
| `PetCharacter` | Controller for the pet inside the cage; manages states (Idle in cage, Falling, Happy Win, Sad Fail). |
| `FrameAnimationPlayer` | Ultra-smooth frame sequence animator for the cat's sprite animations. |
| `LandingPlatform` | Target goal platform with collider trigger to detect safe landing. |
| `LevelManager` | Handles level loading (Levels 1–30), star tracking, win/loss UI, and quick restarts. |
| `SlopeObstacle` / Physics Blocks | Sloped wedges, guide ramps, and colliders for dynamic cage deflection. |

---

## 4. Screen & Mobile Target
- **Orientation**: Vertical Mobile Portrait (9:16 aspect ratio).
- **Camera**: Orthographic Camera sized and positioned to frame the vertical drop, hanger pegs, and landing platform cleanly on mobile screens.
