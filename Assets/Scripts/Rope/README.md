# Rope System — Complete Usage Guide & Documentation

This folder contains the complete, production-ready **Verlet Physics Rope System** designed for casual 3D puzzle mechanics (*Cut the Rope* feel) with obstacle collisions, tension/stretch feedback, and dynamic cutting.

---

## 📁 File Structure

```text
Assets/Scripts/Rope/
├── RopeParticle.cs        # Lightweight particle model for Verlet integration
├── RopeConfig.cs          # ScriptableObject preset for physics, collision & visual tuning
├── VerletRope.cs          # Core physics engine, distance solver, collision, tension & cutting
├── RopeRenderer.cs        # Catmull-Rom spline smoother, stretch-thinning, tension trembling
├── RopeAnchor.cs          # Orientates visual knots/pegs towards rope pull angle
├── RopeDemoController.cs  # Interactive controller to test pulling, stretching, and swipe-cutting
└── README.md              # This guide
```

---

## 🚀 Quick Start: Setting Up a Rope in Unity

### Step 1: Create a Rope Configuration
1. In the Unity Project window, right-click in `Assets/` (e.g. inside `Assets/Settings/` or `Assets/Scripts/Rope/`).
2. Select **Create → Cut Free Pets → Rope Config**.
3. Name it `DefaultRopeConfig`.
4. Key parameters you can tune in the Inspector:
   - **Segment Count**: `16` to `20` (recommended for smooth simulation).
   - **Base Width**: `0.08` (rope thickness).
   - **Elasticity**: `0.02` (compliance/stretchiness).
   - **Enable Collision**: `true` ($\checkmark$).
   - **Max Tension Threshold**: `1.25` (when stretch is 25% past rest length).
   - **High Tension Color**: Reddish/orange tint when pulled tight.

---

### Step 2: Set Up Scene Anchors & Obstacles
In your Scene Hierarchy:
1. **Top Anchor (Peg / Ceiling Hook)**:
   - Right-click → **3D Object → Cylinder** (or Sphere).
   - Name it `Peg_Top`. Position: `(0, 3, 0)`. Scale: `(0.3, 0.3, 0.3)`.
2. **Bottom Anchor (Weight / Pet Tie Point)**:
   - Right-click → **3D Object → Sphere**.
   - Name it `Weight_Bottom`. Position: `(0, -1, 0)`. Scale: `(0.5, 0.5, 0.5)`.
   - *(Optional dynamic physics)*: Add a `Rigidbody` component with `Mass = 1`.
3. **Obstacle Peg (To test collision & wrapping)**:
   - Right-click → **3D Object → Cylinder**.
   - Name it `Obstacle_Peg`. Position: `(0.4, 1.0, 0)`.
   - Rotation: `(90, 0, 0)`. Scale: `(0.4, 0.4, 0.4)`.

---

### Step 3: Create the Rope GameObject
1. Right-click in Hierarchy → **Create Empty** → Name it `Rope`.
2. In the Inspector, click **Add Component** and select **`Verlet Rope`**.
   *(Unity will automatically add `Rope Renderer` and `Line Renderer`)*.
3. Configure **Verlet Rope**:
   - **Config**: Drag your `DefaultRopeConfig` asset here.
   - **Start Anchor**: Drag `Peg_Top`.
   - **End Anchor**: Drag `Weight_Bottom`.
4. Ensure the **Line Renderer** has a valid material assigned (e.g. any default URP Lit or Unlit material).

---

### Step 4: Add the Demo Controller (Optional / Testing)
1. In Hierarchy, create an empty GameObject named `RopeDemo`.
2. Add component **`Rope Demo Controller`**.
3. Drag `Rope` into the **Test Rope** slot.

---

## 🎮 How to Interact During Play Mode

- **Pull & Stretch**:
  - Drag `Weight_Bottom` around in the Scene view or Game view.
  - Notice the rope straighten out taut, become visibly thinner under tension, and shift towards the high-tension color!
- **Obstacle Collision**:
  - Pull the rope across `Obstacle_Peg` — the rope wraps around the cylinder smoothly without passing through it.
- **Swipe-to-Cut**:
  - Drag your mouse (Left-click or Right-click) across any segment of the rope.
  - The rope will snip right at that position, splitting into two physical pieces that swing and fall with gravity!
- **Press 'R'**:
  - Resets the rope back to its initial state.

---

## 💻 Scripting API (How to Use in C# Code)

### 1. Spawning a Rope Dynamically
```csharp
using CutFreePets.Rope;
using UnityEngine;

public class LevelBuilder : MonoBehaviour
{
    [SerializeField] private GameObject ropePrefab;
    [SerializeField] private RopeConfig ropeConfig;

    public VerletRope SpawnRope(Transform pegTransform, Transform petTransform)
    {
        GameObject ropeObj = Instantiate(ropePrefab);
        VerletRope rope = ropeObj.GetComponent<VerletRope>();
        rope.Setup(pegTransform, petTransform, ropeConfig);
        return rope;
    }
}
```

### 2. Cutting a Rope via Code (e.g. Swipe Cut Controller)
```csharp
// Cut by world position (e.g., from swipe raycast)
bool wasCut = rope.CutAtPoint(swipeWorldPosition, cutTolerance: 0.3f);

// Or listen to cut events
rope.OnCut += (cutRope, segmentIdx, cutPoint) => 
{
    Debug.Log($"Rope severed at segment {segmentIdx} at position {cutPoint}!");
    // Play scissors SFX and spawn snip particles here!
};
```

### 3. Monitoring Tension & Stretch
```csharp
rope.OnTensionChanged += (tensionRatio) =>
{
    // tensionRatio = 1.0 (relaxed/rest length)
    // tensionRatio > 1.0 (stretched/taut)
    if (tensionRatio > 1.2f)
    {
        // Rope is under extreme strain
    }
};
```

---

## ⚙️ Key Configuration Properties Reference

| Property | Default | Description |
|---|---|---|
| `segmentCount` | `18` | Number of physical nodes along the rope. Higher = smoother physics, lower = faster. |
| `slackMultiplier` | `1.0` | Initial slack. `1.0` is taut, `1.2` gives a 20% loose hanging curve. |
| `constraintIterations`| `16` | Number of relaxation passes per physics step. Higher = stiffer rope. |
| `damping` | `0.975` | Air resistance factor. |
| `elasticity` | `0.02` | Stretch compliance. `0` = rigid steel cable, `0.05` = stretchy rubber rope. |
| `enableCollision` | `true` | Enables real-time 3D collision against scene colliders. |
| `collisionMask` | `Everything` | Layers the rope will collide with. |
| `particleRadius` | `0.04` | Collision thickness radius of each particle. |
| `baseWidth` | `0.08` | Visual line width when relaxed. |
| `minStretchWidthFactor` | `0.65` | Thickness ratio when pulled tight (rope visibly thins under load). |
| `tensionVibrationAmount`| `0.015` | Micro-tremble intensity when under extreme tension. |
| `splineSubdivisions` | `4` | Catmull-Rom spline smoothing subdivisions between physical points. |
