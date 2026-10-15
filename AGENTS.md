# Project Rules: No Regression & Stability

## Core Guidelines
1. **Never Break Existing Levels**:
   - Any modifications made for new levels (e.g. Level 2, Level 3, etc.) MUST NEVER break or alter Level 1 or any other existing level.
   - Level 1 has a single vertical drop onto `Ground_Platform` with 3 stars. Its layout, rope attachments, collectible positions, and win condition must remain intact and functional.

2. **Single Platform Rule**:
   - Each level contains only ONE Goal Platform (`Ground_Platform`) where the rescued pet lands and celebrates.
   - Any intermediate physics features (such as slides, ramps, barriers, chutes) must be distinct obstacles (`Obstacle_Slide_Ramp`, etc.) and NOT duplicates of the goal platform.

3. **Pet Rescued Presentation**:
   - The rescued pet must always land centered on the goal platform (`targetCatX = platformCollider.bounds.center.x`).
   - The rescued pet must always be strictly upright (`rotation = Quaternion.identity`) and never tilted ("crossed").
   - Rescued celebration happy animations must only trigger upon actually landing on the goal platform, never in mid-air or upon falling into the abyss.
   - Shattered debris must burst outward away from the pet and render behind the pet (`sortingOrder <= 6` for debris, `sortingOrder >= 14` for the rescued pet).

4. **Preserve Code Architecture & Documentation**:
   - Retain all existing event signatures, components, and docstrings.
   - Always verify compilation in Unity before completing tasks.

5. **Common Component Modification Rule (Zero Side-Effects on Working Features)**:
   - Any time changes are made to ANY common/shared component, system, or script (including but not limited to `PetCage`, `CageBreakEffect`, `CatAnimationPlayer`, `VerletRope`, `SwipeCutBlade`, `LevelManager`, UI, or physics parameters):
     - You MUST verify that NO existing working levels or features are broken or altered by that change.
     - **Scale & Visual Preservation**: The pet's visual size, proportions, and scale must remain consistent before and after the cage breaks. The pet must never unexpectedly balloon, pop, or grow bigger when landing.
     - **Cross-Level Verification**: Always check all working levels (especially Level 1) to ensure zero regressions in layout, gameplay mechanics, visual sizing, and celebration sequences.
