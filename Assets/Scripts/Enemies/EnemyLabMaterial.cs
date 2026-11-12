using UnityEngine;
public sealed class EnemyLabMaterial : MonoBehaviour
{
    public Material owned;
    void OnDestroy() { if (owned != null) Destroy(owned); }
}
