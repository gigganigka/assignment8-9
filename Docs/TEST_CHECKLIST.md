# Required play tests - NOT YET EXECUTED

Fill in the real editor version, phone model, Android version, date, and result. A prepared checkbox is not evidence of a passing test.

## Import and compilation
- [ ] Package Manager resolves all dependencies; no missing scripts or pink materials.
- [ ] Unity Console has no compilation errors.
- [ ] Tools > Mobile Game > 1. Prepare Project and Bake UI completes.
- [ ] Tools > Mobile Game > 2. Build Android APK succeeds.

## Each of: 16:9 phone, notch phone (19.5:9 or 20:9), 4:3 tablet
- [ ] Main Menu title, Play and Quit are visible; settings respond.
- [ ] Play loads level1; health and coins display actual state.
- [ ] HUD remains inside the safe area; nothing overlaps or clips.
- [ ] Controls remain in bottom corners and are comfortably reachable.
- [ ] Pause stops enemies, physics, projectiles and player actions.
- [ ] Resume restores time; there is no stuck movement or shot.
- [ ] Restart reloads current scene with fresh coins and health.
- [ ] Main Menu returns; Play works again at normal speed.
- [ ] Death shows Try Again; Resume is unavailable; Restart/Menu work.
- [ ] level1 -> level2 -> bosslevel -> win transitions work.
- [ ] Boss health bar updates; victory screen returns to the menu.

## Physical Android device
- [ ] Dragging the left stick moves the player; releasing stops movement.
- [ ] Move + Jump with two fingers works.
- [ ] Move + Fire works; holding Fire respects cooldown.
- [ ] Three fingers can move, jump and fire without stealing the stick pointer.
- [ ] Drag outside the stick and release: input stops.
- [ ] Background app / lock screen / focus loss: controls clear, game pauses.
- [ ] Resume after returning: no stuck movement or firing.
- [ ] Rotate landscape left/right: layout remains in the safe area.
- [ ] Quit closes the Android player. In the Unity Editor it only logs a message.

## Evidence
- [ ] HUD_16x9.png
- [ ] HUD_notch.png
- [ ] HUD_tablet.png
- [ ] MainMenu.png
- [ ] Pause.png
- [ ] Narrated English MP4, 2-3 minutes, showing UI, GDD, own commits and merged PR.
- [ ] Teacher can open repository; GDD PDF and AI_LOG are included.
