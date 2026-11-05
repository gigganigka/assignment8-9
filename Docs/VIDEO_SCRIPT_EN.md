# Video plan - 2 to 3 minutes, your own English voice

Record an actual Unity/phone demonstration. Replace or remove any sentence that does not match your real tests. This text is a speaking aid, not a completed recording.

## 0:00-0:50 - GDD

My game is called Coinbound. It is a two-dimensional platformer for IOS, played in landscape mode. The core loop is to move, jump, collect coins, and reach the portal. The game has two platforming levels and a final boss arena. The minimum playable version includes movement, jumping, shooting, health, coins, scene transitions, and a complete mobile interface. I excluded multiplayer, an open world, an inventory system, and procedural levels to keep the scope manageable. My weekly plan covers mobile UI, enemy behaviour, content and saving, audio, and visual polish.

## 0:50-1:50 - Interface

This is the main menu. Play opens the first level. The HUD shows my health and collected coins. My left thumb controls movement, and my right thumb controls jumping and shooting. [Demonstrate simultaneous input on a real phone if available.] Pause freezes the game. Resume continues it, Restart resets the current level, and Main Menu returns to the start screen.

[Show 16:9, a notch phone and a 4:3 tablet in Device Simulator.]

The Canvas uses Scale With Screen Size at 1920 by 1080, matching height. The HUD and touch controls stay inside a safe-area panel, and the background fills the whole screen. The buttons follow screen corners using anchors.

## 1:50-2:40 - Repository and disclosure

[Open your actual GitHub repository.]

Here are my meaningful commits from my own account. This feature branch was merged into main through this pull request. The repository includes Assets, Packages, ProjectSettings, the GDD, and an AI log. Generated folders and APK builds are excluded.

[Describe your real contribution and AI use accurately. This version contains Codex-written modifications, which conflicts with the assignment's no-agent requirement. Do not claim that the work meets that restriction or that you wrote every line yourself.]

[If tested: state phone model, Android version and what you verified. If not tested, say the phone build has not yet been verified.]
