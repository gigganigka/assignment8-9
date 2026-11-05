using UnityEngine;

// Uses the supplied coloured explorer artwork. Only the sprite is changed:
// collisions, jump force, animation transforms and invincibility blinking remain intact.
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerAppearance : MonoBehaviour
{
    SpriteRenderer visual;
    Material skinMaterial;
    Sprite idle, run;
    Rigidbody2D body;
    void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        visual.color = Color.white;
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null) { skinMaterial = new Material(shader); visual.sharedMaterial = skinMaterial; }
        idle = MobileSprites.Load("Mobile/ExplorerIdle");
        run = MobileSprites.Load("Mobile/ExplorerRun");
        Apply();
    }
    void OnDestroy() { if (skinMaterial != null) Destroy(skinMaterial); }
    void LateUpdate() { Apply(); }
    void Apply()
    {
        if (visual == null || idle == null) return;
        bool moving = body != null && Mathf.Abs(body.linearVelocity.x) > 0.1f;
        visual.sprite = moving && run != null && (int)(Time.time * 8f) % 2 == 1 ? run : idle;
    }
}
