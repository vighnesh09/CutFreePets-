# Coding & Architecture Rules

## 1. Physics & Simulation Integrity
- **Verlet Physics for Ropes**: Always use multi-substep Verlet integration (`physicsSubsteps >= 3`) with bending constraints to ensure ropes never vibrate infinitely or fold into unnatural kinks.
- **Rope Severance**: Cutting a rope must split it cleanly: the top piece swings from the hanger, the bottom piece stays anchored to the cage while falling, and both pieces auto-destroy after a short delay (~2.5s).
- **Physical Cage**: The cage must use `Rigidbody2D` and `BoxCollider2D` (or composite collider). It must NOT be draggable by the player; player interaction is strictly limited to cutting ropes.

---

## 2. Input & Cross-Platform Rules
- **New Input System Only**: All input handling MUST use `UnityEngine.InputSystem` (`Mouse.current`, `Touchscreen.current`, `Pointer.current`). Legacy `Input.mousePosition` is strictly forbidden.
- **Direct Swipe Only**: Zero scissors icons or buttons. The player swipes directly across ropes with a dynamic glowing blade trail.

---

## 3. Visual & Aesthetic Standards
- **Juicy Cut-the-Rope Feel**:
  - Braided rope texture with dynamic UV tiling and Catmull-Rom spline curves.
  - Ropes stretch and thin out under heavy load with warm color tension feedback.
  - End knot rings must align with rope pull angle.
- **Orientation & Camera**:
  - Target orientation is strictly **Vertical Mobile Portrait (9:16)**.
  - All elements (top hanger pegs, swinging ropes, falling cage, slopes, landing platform) must be framed inside the 9:16 vertical screen with appropriate padding for mobile screen edges.

---

## 4. Code Quality & Modularity
- **Component Decoupling**:
  - Rope physics (`VerletRope`) must not directly hardcode cage logic.
  - Cage (`PetCage`) holds references to attached ropes and pet character, but delegates animation to `PetCharacter`.
  - Frame sequence animation (`FrameAnimationPlayer`) must be generic and reusable for any pet (Cat, Dog, etc.).
- **Unity Best Practices**:
  - No empty folders in `Assets/`.
  - Avoid LINQ extensions on raw arrays in non-LINQ scopes (`OfType` issues).
  - Use `Undo` operations and `EditorSceneManager.MarkSceneDirty` in Editor tools.
  - Safe fallback asset loading for multi-sprite textures (`AssetDatabase.LoadAllAssetsAtPath`).

---

## 5. File Size & Component Documentation Rules (CRITICAL)
- **Strict File Size Limits**:
  - NO code file (`.cs`) may exceed **1,000 lines** (absolute maximum limit: **1,200 lines**).
  - Monolithic scripts are strictly forbidden. Any class doing multiple tasks must be decomposed into smaller, specialized components (Single Responsibility Principle).
- **Mandatory Component Header Documentation**:
  - Every class MUST begin with an informative `<summary>` header detailing:
    1. Exact component name & responsibility
    2. Which game object or feature it belongs to
    3. What it does NOT handle (delegations)
  - All public methods, events, and properties MUST be commented so any future developer or AI knows exactly what each piece does without guessing.
- **Prefab-Driven Scene Architecture**:
  - Scene files (`.unity`) MUST be lightweight composition stages consisting of reusable Prefabs (`.prefab`).
  - Complex multi-element structures (like Victory Popups, Level Rigs, and UI sets) MUST live in `Assets/Prefabs/` as prefabs so editing a prefab updates every level simultaneously with zero risk of breaking scenes.

