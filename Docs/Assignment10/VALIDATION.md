# Validation - 2026-11-12

Executed locally:
- Existing 61 offline project resource/reference/layout checks passed.
- New scene root registration and local component IDs checked; no duplicate IDs.
- All new asset GUIDs and tuning-script references resolve locally and are unique.
- enemy_lab is present in EditorBuildSettings and MobileProjectSetup, and UI recognizes it as a gameplay scene.
- All 49 C# files parsed with a C# grammar for Editor, Windows, Web and iOS preprocessor selections without syntax errors. This is not semantic/type checking or a Unity compilation.
- git diff --check passed.

Source review covered: separate FSMs, raycast occlusion excluding triggers, camera visibility guards at attack preparation/release/during charge and projectile flight, swept shot collision, pause-safe scaled timers, one hit per charge, no passive contact damage, cached player references, persistent tuning assets and enemy_lab -> original campaign routing.

Not performed: Unity import/compilation, gameplay execution, Device Simulator rendering, physical phone tests, two-person playtest, measured attempt counts, before/after tuning evidence, APK/Web/iOS build or recorded video. These remain pending. No 2-4-tries result is claimed.

The encounter world and enemies are created at runtime by EnemyLabBootstrap in the committed enemy_lab scene. Inspect in Play Mode; change tuning persistently through Assets/Resources/EnemyLab/*Settings.asset.
