# Rule: No Regression on Existing Levels & Gameplay

- Any changes made for specific levels (e.g. Level 2, Level 3) must never break Level 1 or other existing levels.
- Only one Goal Platform (`Ground_Platform`) per level. Obstacles (like ramps, slides, shelves) must use dedicated obstacle assets, not duplicate the goal platform.
- When the cage breaks upon landing on the platform, the pet must always be centered on the platform, upright (`Quaternion.identity`), and unobstructed by debris.
- Happy victory animation plays only upon landing on the platform. Abyss/hazard drops trigger a retry, never a mid-air celebration.
- **Common Components Rule**: Whenever making changes in any common component (PetCage, CageBreakEffect, CatAnimationPlayer, VerletRope, Blade, etc.), thoroughly check and verify that all other working levels and features (especially Level 1) do not break or experience regressions. The pet must maintain its exact visual size and proportions without ballooning or growing larger upon landing.
