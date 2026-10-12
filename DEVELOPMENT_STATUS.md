# STRIKE ZONE: MOBILE — development status

This branch contains a first gameplay-prototype pass for the Unity Android project.

## Changes in this pass
- Added extra waist-high cover and clearer lanes to the procedural arena.
- Added menu choices for Training, 10 Kills, and Survival.
- Added a 1.15-second reload delay and basic reload state.
- Added basic bot line-of-sight checks, obstacle steering, approach behavior, and player damage.
- Added procedural placeholder audio for shots, reloads, hits, and footsteps. These are generated at runtime and should later be replaced with authored sound assets.

## Important limitations
- This is still a primitive-based prototype, not a finished art pass.
- Bot navigation is lightweight raycast steering, not a NavMesh/pathfinding solution; it can still get stuck around complex geometry.
- Team battle is not implemented yet. The current modes are Training, 10 Kills, and a 90-second Survival round.
- Procedural audio is placeholder feedback, not production-quality weapon sound.
- Mobile touch look and the full game loop still need device testing.

## Verification status
Unity has not been launched in this environment. No Unity compilation, Android build, APK installation, frame-rate test, or on-device gameplay test has been performed. Please treat the changes as unverified until the project is opened with Unity 2022.3.50f1 and checked.
