using UnityEngine;

public class CombatHitbox : MonoBehaviour
{
    public Entity Owner { get; set; }
    public float Damage { get; set; } = 50f;
    public float StunDuration { get; set; } = 0.2f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Owner == null) return;
        Entity target = other.GetComponent<Entity>();
        if (target == null || target == Owner) return;
        Debug.Log($"[HITBOX] {Owner.name} hit {target.name} for {Damage} dmg (stun: {StunDuration}s)");
        // Call ReceiveDmg directly so StunDuration is forwarded correctly
        target.ReceiveDmg(Damage, StunDuration);
    }

    // Reads collider live every frame so resizing in Inspector is instant
    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Vector3 center = transform.TransformPoint(box.offset);
        Vector3 size   = new Vector3(box.size.x * transform.lossyScale.x,
                                     box.size.y * transform.lossyScale.y, 0.1f);
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);
    }
}