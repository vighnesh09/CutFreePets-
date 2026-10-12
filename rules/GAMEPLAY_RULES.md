# Gameplay & Level Design Rules

## 1. Win & Loss Rules
- **Win Condition**:
  - The cage must land on the designated `LandingPlatform`.
  - When landing is confirmed:
    - The cage opens/vanishes (with a nice dust/poof particle effect).
    - The cat emerges onto the platform and plays its celebratory **Happy Animation** loop.
    - Stars collected during the level (up to 3 stars) are awarded and displayed.
    - Level complete panel is shown with button to proceed to the next level.
- **Fail Condition**:
  - The cage misses the landing platform (falls below the screen boundary or hits a death/hazard zone).
  - When failure occurs:
    - The cage breaks/vanishes.
    - The cat displays its **Sad/Crying Animation**.
    - The level smoothly and automatically resets/restarts after a short pause.

---

## 2. Level Design & Progression (20–30 Levels)
- **Early Levels (1–5)**:
  - Simple 1-rope suspensions with straight vertical drops or gentle swings.
  - Teaches the basic swipe-to-cut mechanic and safe landing on the platform.
- **Intermediate Levels (6–15)**:
  - 2 to 3 ropes holding the cage at angled tensions.
  - Requires cutting in the correct order to swing the cage like a pendulum towards the target platform.
  - Introduction of collectible stars placed in the trajectory.
- **Advanced Levels (16–30)**:
  - Inclusion of obstacles: slope blocks/ramps where the cage rolls or slides down.
  - Tight landing zones, moving obstacles, or timing-dependent swings.

---

## 3. Character & Animation Extensibility
- **Pet Decoupling**:
  - Any pet placed inside the cage must conform to standard animation states:
    1. `IdleInCage`: Looking out between cage bars, blinking or looking nervous.
    2. `InAirFalling`: Frightened / waving paws while in freefall.
    3. `HappyWin`: Joyful celebration, jumping, smiling when safely landed.
    4. `SadFail`: Disappointed, sad eyes / crying when landing fails.
- **Frame Sequence Format**:
  - Animations are driven by sequence arrays of Sprite frames (`Sprite[]`) played at configurable frame rates (e.g. 12 to 24 FPS) by `FrameAnimationPlayer`.
