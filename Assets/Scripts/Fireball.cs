using UnityEngine;

// Огненный шар босса: бьёт игрока, исчезает об землю. Крутится в полёте.
public class Fireball : Projectile
{
    public int damage = 1;
    public float spinSpeed = 360f;   // градусов в секунду - просто для красоты

    protected override void Update()
    {
        base.Update();                                        // полёт из базового класса Projectile
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime); // вращение картинки
    }

    protected override void OnHit(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Health h = other.GetComponent<Health>();
            if (h != null) h.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
