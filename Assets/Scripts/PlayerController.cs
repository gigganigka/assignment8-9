using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 15f;
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;
    public AudioClip jumpSound;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;
    Health health;
    float keyboardMove, touchMove;
    bool touchActive;
    bool isGrounded;
    bool CanAct => isActiveAndEnabled && Time.timeScale > 0f &&
                   (health == null || health.CurrentHp > 0);
    float Move => touchActive ? touchMove : keyboardMove;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        health = GetComponent<Health>();
    }
    void Update()
    {
        CheckGround();
        float move = CanAct ? Move : 0f;
        if (sr != null && Mathf.Abs(move) > 0.1f) sr.flipX = move < 0f;
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.SetFloat("Speed", Mathf.Abs(move));
            anim.SetBool("Grounded", isGrounded);
        }
    }
    void CheckGround()
    {
        isGrounded = groundCheck != null &&
            Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer) != null;
    }
    void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2((CanAct ? Move : 0f) * moveSpeed, rb.linearVelocity.y);
    }
    // PlayerInput still delivers keyboard and physical gamepad actions.
    public void OnMove(InputValue value) { keyboardMove = CanAct ? value.Get<Vector2>().x : 0f; }
    public void OnJump(InputValue value) { if (value.isPressed) TryJump(); }
    public void SetTouchMove(float value, bool active)
    {
        touchMove = CanAct ? Mathf.Clamp(value, -1f, 1f) : 0f;
        touchActive = active && CanAct;
    }
    public void TryJump()
    {
        if (!CanAct || rb == null) return;
        CheckGround();
        if (!isGrounded) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        Sfx.Play(jumpSound != null ? jumpSound : GameAudio.Clip("jump"), 0.75f);
    }
    public void ResetInput()
    {
        keyboardMove = touchMove = 0f;
        touchActive = false;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }
    void OnDisable() { ResetInput(); }
    void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
    void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
}
