using UnityEngine;

[CreateAssetMenu(menuName = "Coinbound/Enemy tuning")]
public sealed class EnemyTuning : ScriptableObject
{
    [Min(1)] public int hitPoints = 2;
    [Min(0)] public float moveSpeed = 1.6f;
    [Min(0.1f)] public float visionRange = 6f;
    [Range(1, 360)] public float visionAngle = 150f;
    [Range(0.3f, 0.8f)] public float windUp = 0.6f;
    [Min(0.1f)] public float cooldown = 1.3f;
    [Min(1)] public int damage = 1;
    [Min(0.1f)] public float attackRange = 1.1f;
    [Min(0.1f)] public float chaseSpeed = 2.3f;
    [Min(0)] public float sightMemory = 0.8f;
    [Min(0.1f)] public float bulletSpeed = 5f;
    [Min(0.1f)] public float bulletLifetime = 2.5f;
    [Min(0.1f)] public float chargeSpeed = 6f;
    [Min(0.1f)] public float chargeDuration = 0.6f;
    void OnValidate()
    {
        hitPoints = Mathf.Max(1, hitPoints); damage = Mathf.Max(1, damage);
        windUp = Mathf.Clamp(windUp, 0.3f, 0.8f);
        cooldown = Mathf.Max(0.1f, cooldown);
    }
}
