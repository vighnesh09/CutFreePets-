# Technical Architecture & Component Reference

## 1. Directory Structure

```text
Assets/
├── Art/
│   ├── Rope/            # Braided hemp texture, peg disc, knot ring, sparks
│   ├── Pets/            # Pet animation frames (Cat, Dog)
│   ├── Environment/     # Cage sprites, landing platform, obstacles, stars
│   └── UI/              # Star icons, victory banners, retry buttons
├── Audio/               # Cut whoosh, cage open, purr/meow, victory chime
├── Prefabs/
│   ├── Gameplay/        # PetCage, LandingPlatform, StarCollectible, Obstacles
│   ├── Pets/            # Cat_Pet, Dog_Pet
│   └── Rope/            # VerletRope, RopeHanger
├── Scenes/
│   ├── SampleScene.unity# Active development scene
│   └── Levels/          # Level_01.unity to Level_30.unity (or prefab-based levels)
├── Scripts/
│   ├── Core/            # LevelManager, GameManager, StarRatingManager
│   ├── Cut/             # SwipeCutBlade (Touch & Mouse input)
│   ├── Gameplay/        # PetCage, LandingPlatform, PetCharacter, FrameAnimationPlayer
│   └── Rope/            # VerletRope, RopeRenderer, RopeHanger, RopeConfig
└── Settings/            # DefaultRopeConfig, Materials, UniversalRP profiles
```

---

## 2. Component Communication & Events

```text
[SwipeCutBlade]
       │ (Slices rope)
       ▼
 [VerletRope] ────► Severance (Dual-half split & drop)
       │
       ▼ (All ropes severed)
   [PetCage] ────► Falls via 2D Rigidbody Physics
       │
       ├───────────────┬───────────────┐
       ▼               ▼               ▼
[LandingPlatform] [SlopeObstacle] [Hazard/Out of Bounds]
       │               │               │
  (Safe Landing)  (Slides down)   (Miss / Hit ground)
       │                               │
       ▼                               ▼
Cage Disappears                 Cage Breaks/Disappears
Cat plays HAPPY animation       Cat plays SAD animation
Stars counted & Next Level      Auto-restart level
```

---

## 3. Extensibility for Pets & Obstacles

- **Pet Character Swapping**:
  `PetCharacter` exposes animation clips for `IdleInCage`, `Falling`, `HappyWin`, `SadFail`. Adding a Dog simply requires creating a new prefab with dog animation frames.
- **Level Obstacles**:
  Obstacles use standard Unity 2D Physics components (`PolygonCollider2D`, `PhysicsMaterial2D` with customizable friction/bounciness) so level designers can create slope blocks, bounce pads, and slides easily in the Editor.
