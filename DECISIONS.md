# Assignment 10 decisions

- 2026-11-12: Asked Codex to add three different enemies to Coinbound using the existing player, Health and IDamageable interfaces.
- 2026-11-12: Implemented separate Patroller, Shooter and Charger state machines; common perception and movement stay in EnemyBrain2D. Added red wind-ups, cooldowns and camera visibility guards.
- 2026-11-12: Source review found runtime-created enemy fields would be inconvenient to tune persistently. Added three EnemyTuning assets so changes can be saved outside Play Mode.
- 2026-11-12: Used a separate enemy_lab encounter before the original campaign, preserving the original player and camera. This requires Unity/phone validation; no playtest result is invented.
- Student follow-up: after testing, replace this placeholder with what you changed, the real old/new values and why. Record both testers and attempt counts in Docs/Assignment10/PLAYTEST.csv.
