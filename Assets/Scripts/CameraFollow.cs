using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;                              // за кем следим (Player)
    public float smoothSpeed = 5f;                        // чем больше, тем резче догоняет
    public Vector3 offset = new Vector3(0, 1, -10);       // смещение; Z = -10 обязателен для 2D

    // LateUpdate идёт после всех Update, поэтому камера не дёргается
    void LateUpdate()
    {
        if (target == null) return; // если цель не назначена, ничего не делаем

        Vector3 desired = target.position + offset;
        // Lerp плавно двигает камеру к желаемой точке
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
