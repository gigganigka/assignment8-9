# Script map

```mermaid
flowchart TD
    T["EnemyTuning assets"] --> B["EnemyBrain2D: sight, health, movement"]
    H["Player: Health"] --> B
    L["EnemyLabBootstrap: arena and waypoints"] --> B
    B --> P["PatrollerBrain: Patrol / Chase / WindUp / Recover"]
    B --> S["ShooterBrain: Idle / Aim / Shoot / Cooldown"]
    B --> C["ChargerBrain: Patrol / WindUp / Charge / Recover"]
    S --> Q["EnemyShot2D: swept collision"]
    P --> D["Health.TakeDamage"]
    C --> D
    Q --> D
    U["PlayerBullet -> IDamageable.TakeHit"] --> B
```

The base shares mechanics, while each subclass owns its own transitions and attack behaviour. EnemyTuning provides persistent Inspector values. Scene bootstrap preserves the existing player and camera, instantiates all three types and switches to level1 after the encounter is cleared. State names and red warning markers are displayed above enemies for the video.
