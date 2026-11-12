# COINBOUND

Individual 2D landscape platformer by Daryn Talgatov for Assignment 8-9.

Collect coins, reach portals and defeat the boss. Controls: A/D or arrows, Space to jump, X to fire; mobile stick, JUMP and FIRE.

Open with Unity 6000.6.2f1. Let Package Manager restore packages, then run Tools > Mobile Game > 1. Prepare Project and Bake UI and open Assets/mainmenu.unity.

Three screens: Main Menu, HUD and Pause. Landscape Canvas Scaler: 1920x1080, Match 1. Foreground controls use SafeArea.

Builds: menu command 6 for Windows; command 5 for Web/Safari; command 4 exports an unsigned iOS Xcode project. See Docs/IOS_WEB_RU.md.

GDD: Docs/GDD.pdf. Submission checklist and actual verification status: Docs/SUBMISSION_STATUS.md.

The current source upload comes from the student-supplied Assets archive. Packages and ProjectSettings were restored from the prepared project baseline because the upload did not include them. Teacher should verify import on Unity.

AI assistance is disclosed in AI_LOG.txt. The assignment forbids code-writing agents; prior Codex modifications conflict with that condition. The log does not waive that rule. No test results, student voice recording or past development history are fabricated.

## Assignment 10: Three enemy types

New encounter: `enemy_lab`, started by Play before the original campaign. Read [setup and tuning](Docs/Assignment10/START_RU.md), [script map](Docs/Assignment10/SCRIPT_MAP.md), and [required playtest proof](Docs/Assignment10/PROOF_CHECKLIST.md). Three separate state machines and persistent tuning assets are included. Agent use is permitted for Assignment 10; see DECISIONS.md. Unity/device/playtest results are pending, not fabricated.
