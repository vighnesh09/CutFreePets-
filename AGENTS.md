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

6. **Screen Boundary Fail Condition**:
   - Anytime the pet cage goes outside of the screen boundaries horizontally or drops into the abyss without successfully landing on the goal platform, the level is considered failed and must automatically restart cleanly.

7. **Visual Theme & Art Consistency Rule ("The Secret Garden & Treehouse Adventure")**:
   - **Unified World Narrative**:
     - All screens and gameplay levels exist in one continuous, vibrant cartoon world.
     - **Home Screen**: Cozy sunlit treehouse veranda/porch with warm honey-oak flooring looking out through an open pergola onto the garden stone path, flower meadow, and rustic gate.
     - **Level Select (Adventure Map)**: Winding stepping-stone path ascending from the garden flower beds up through sunny green hills, wooden footbridges, and leafy canopies toward the great tree.
     - **In-Game Levels**: Sunlit open-air tree canopy with leafy branch anchors at the top, warm honey-oak platforms/obstacles, and sunny meadow vistas below.
     - **Strict Prohibition**: Never use dark, gloomy, windowless dungeon or dimly lit room backgrounds. The game must always feel bright, cheerful, warm, and inviting.
   - **Official Color Palette**:
     - **Sky & Camera Clear**: Sunny daylight sky blue (`#85CCFA` / `{r: 0.52, g: 0.80, b: 0.98, a: 1}`). All scene cameras must clear to this color to eliminate dark borders on tall phone displays (19.5:9, 20:9).
     - **Foliage & Meadows**: Vibrant apple green (`#8BC34A`), lawn green (`#4CAF50`), and fresh spring lime (`#AEEA00`).
     - **Woodwork & Obstacles**: Warm honey oak (`#D2691E` / `#B87333`) and rich caramel timber (`#8D5B28`). All platforms, ramps, and obstacle slides must share this warm golden-wood tone.
     - **Collectibles & Accents**: Sunny golden yellow (`#FFD54F` / `#FFA000`) with bright cartoon sparkle effects.
   - **Lighting & Rendering**:
     - Cheerful 2:00 PM afternoon sunlight with warm golden highlights and soft shadows.
     - Backgrounds must have gentle depth-of-field separation so foreground ropes, blades, cages, and collectibles maintain razor-sharp visual clarity.
   - **Mandatory Rule for All Future Assets**:
     - Any future asset, pet model/spritesheet (e.g. Dog, Puppy, Bunny), obstacle mechanism, UI screen, or new world expansion MUST strictly adhere to this color theme, warm daylight lighting, and treehouse/garden aesthetic.
