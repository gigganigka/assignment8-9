using UnityEngine;

public sealed class ChargerBrain : EnemyBrain2D
{
    public enum State { Patrol, WindUp, Charge, Recover }
    [SerializeField] State state;
    [SerializeField, Min(0.1f)] float chargeSpeed = 6f;
    [SerializeField, Min(0.1f)] float chargeDuration = 0.6f;
    [SerializeField, Min(0.05f)] float wallProbe = 0.6f;
    bool dealtDamage;
    public override string StateName => state.ToString();
    protected override bool Telegraphing => state == State.WindUp;
    protected override void Awake()
    {
        base.Awake();
        if (tuning == null) return;
        chargeSpeed = tuning.chargeSpeed;
        chargeDuration = tuning.chargeDuration;
    }
    void Enter(State next) { state = next; enteredAt = Time.time; moveIntent = 0; }
    protected override void TickState()
    {
        switch (state)
        {
            case State.Patrol:
                Patrol();
                if (MayStartAttack) { FaceTarget(); Enter(State.WindUp); }
                break;
            case State.WindUp:
                if (!AttackVisible) { Enter(State.Recover); break; }
                if (StateAge >= windUp) { dealtDamage = false; Enter(State.Charge); }
                break;
            case State.Charge:
                if (!AttackVisible || StateAge >= chargeDuration ||
                    Physics2D.Raycast(transform.position, Vector2.right * facing, wallProbe, walls))
                { Enter(State.Recover); break; }
                if (!MoveGrounded(facing * chargeSpeed)) { Enter(State.Recover); break; }
                if (!dealtDamage && DistanceToTarget <= attackRange)
                {
                    Strike(); dealtDamage = true;
                }
                break;
            case State.Recover:
                if (StateAge >= cooldown) Enter(State.Patrol);
                break;
        }
    }
}
