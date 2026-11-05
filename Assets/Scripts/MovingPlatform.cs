using System.Collections.Generic;
using UnityEngine;

// Движущаяся платформа: ездит туда-обратно и возит героя на себе.
// На платформе: BoxCollider2D (не триггер), Rigidbody2D (Body Type = Kinematic), слой Ground.
public class MovingPlatform : MonoBehaviour
{
    public Vector2 moveOffset = new Vector2(4f, 0f);   // на сколько уезжает (X, Y)
    public float speed = 1.5f;

    Vector3 startPos;
    readonly List<Transform> riders = new List<Transform>(); // кто стоит сверху

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        Vector3 old = transform.position;
        float t = Mathf.PingPong(Time.time * speed, 1f);     // 0..1..0..1
        transform.position = startPos + (Vector3)(moveOffset * t);

        Vector3 delta = transform.position - old;            // на сколько сдвинулись за кадр
        foreach (Transform r in riders) r.position += delta; // везём пассажиров
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.collider.CompareTag("Player") && c.transform.position.y > transform.position.y)
            if (!riders.Contains(c.transform)) riders.Add(c.transform);
    }

    void OnCollisionExit2D(Collision2D c)
    {
        riders.Remove(c.transform);
    }
}
