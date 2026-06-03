using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] protected float maxHP = 1000f;
    protected float currentHP;

    protected bool isStunned;
    protected bool isDead;

    public float MaxHP { get => maxHP; protected set => maxHP = value; }
    public float CurrentHP { get => currentHP; protected set => currentHP = Mathf.Clamp(value, 0, maxHP); }
    public bool IsStunned { get => isStunned; protected set => isStunned = value; }
    public bool IsDead { get => isDead; protected set => isDead = value; }

    protected virtual void Awake() { currentHP = maxHP; }

    public virtual void Attack() { }

    public virtual void ReceiveDmg(float amount, float stunDuration = 0.2f)
    {
        if (isDead) return;
        CurrentHP -= amount;
        Debug.Log($"[DAMAGE] {name} took {amount} — HP: {currentHP}/{maxHP}");
        if (currentHP <= 0) { Die(); return; }
        if (stunDuration > 0) StartCoroutine(ApplyStun(stunDuration));
        OnDamageReceived(amount);
    }

    public virtual void InflictDmg(Entity target, float amount)
    {
        if (target == null || target.IsDead) return;
        Debug.Log($"[INFLICT] {name} → {target.name} | {amount} dmg");
        target.ReceiveDmg(amount);
    }

    protected virtual void Die()
    {
        isDead = true;
        Debug.Log($"[DEATH] {name} died.");
        OnDeath();
    }

    protected virtual void OnDamageReceived(float amount) { }
    protected virtual void OnDeath() { }

    protected System.Collections.IEnumerator ApplyStun(float duration)
    {
        Debug.Log($"[STUN] {name} stunned for {duration}s");
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
        Debug.Log($"[STUN] {name} recovered");
    }

    // Hurtbox: green normally, blue while stunned — reads collider live
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Color c = isStunned ? new Color(0.2f, 0.2f, 1f, 0.35f) : new Color(0f, 1f, 0f, 0.25f);
        Gizmos.color = c;

        if (col is BoxCollider2D box)
        {
            Vector3 center = transform.TransformPoint(box.offset);
            Vector3 size   = new Vector3(box.size.x * transform.lossyScale.x,
                                         box.size.y * transform.lossyScale.y, 0.1f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(c.r, c.g, c.b, 1f);
            Gizmos.DrawWireCube(center, size);
        }
        else if (col is CircleCollider2D circle)
        {
            Gizmos.DrawSphere(transform.TransformPoint(circle.offset),
                              circle.radius * transform.lossyScale.x);
        }

#if UNITY_EDITOR
        // HP label above entity
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f,
            $"{name}\nHP: {currentHP:0}/{maxHP:0}");
#endif
    }
}