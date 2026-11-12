using UnityEngine;

public sealed class PatrollerBrain : EnemyBrain2D
{
    public enum State { Patrol, Chase, WindUp, Recover }
    [SerializeField] State state;
    [SerializeField, Min(0.1f)] float chaseSpeed = 2.3f;
    [SerializeField, Min(0)] float sightMemory = 0.8f;
    float lastSeen;
    public override string StateName => state.ToString();
    protected override bool Telegraphing => state == State.WindUp;
    protected override void Awake()
    {
        base.Awake();
        if (tuning == null) return;
        chaseSpeed = tuning.chaseSpeed;
        sightMemory = tuning.sightMemory;
    }
    void Enter(State next) { state = next; enteredAt = Time.time; moveIntent = 0; }
    protected override void TickState()
    {
        switch (state)
        {
            case State.Patrol:
                Patrol();
                if (CanSeeTarget()) { lastSeen = Time.time; Enter(State.Chase); }
                break;
            case State.Chase:
                if (CanSeeTarget()) lastSeen = Time.time;
                if (Time.time - lastSeen > sightMemory) { Enter(State.Patrol); break; }
                FaceTarget();
                if (DistanceToTarget <= attackRange && MayStartAttack) Enter(State.WindUp);
                else MoveGrounded(facing * chaseSpeed);
                break;
            case State.WindUp:
                // Facing is locked during the warning: stepping behind evades the hit.
                if (!AttackVisible) { Enter(State.Recover); break; }
                if (StateAge >= windUp) { Strike(); Enter(State.Recover); }
                break;
            case State.Recover:
                if (StateAge >= cooldown) Enter(State.Patrol);
                break;
        }
    }
}
