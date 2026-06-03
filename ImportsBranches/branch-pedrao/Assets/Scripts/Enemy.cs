using UnityEngine;
using System.Collections;

public class Enemy : Entity
{
    [Header("Enemy Combat")]
    [SerializeField] private float attackDamage = 60f;
    [SerializeField] private float attackRange  = 1.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackStunDuration = 0.25f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Components")]
    [SerializeField] private Animator anim;
    [SerializeField] private CombatHitbox hitbox; // opcional — criado automaticamente se vazio

    private float attackTimer;
    private bool  isAttacking;

    protected override void Awake()
    {
        base.Awake();
        if (!anim) anim = GetComponent<Animator>();
        EnsureHitbox();
    }

    // Cria o hitbox filho automaticamente se não estiver atribuído no Inspector
    private void EnsureHitbox()
    {
        if (hitbox != null) { InitHitbox(); return; }

        // Procura filho existente
        hitbox = GetComponentInChildren<CombatHitbox>(true);

        if (hitbox == null)
        {
            // Cria filho "AttackHitbox"
            GameObject go = new GameObject("AttackHitbox");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0.6f, 0f, 0f); // frente do inimigo

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size      = new Vector2(1f, 1f);
            col.isTrigger = true;

            // Mesmo layer do inimigo para não bloquear física
            go.layer = gameObject.layer;

            hitbox = go.AddComponent<CombatHitbox>();
            Debug.Log("[ENEMY] AttackHitbox criado automaticamente.");
        }

        InitHitbox();
    }

    private void InitHitbox()
    {
        hitbox.Owner         = this;
        hitbox.Damage        = attackDamage;
        hitbox.StunDuration  = attackStunDuration;
        hitbox.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isDead || isStunned) return;
        attackTimer -= Time.deltaTime;

        Collider2D player = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        if (player != null && attackTimer <= 0f && !isAttacking)
            StartCoroutine(PerformAttack());
    }

    public override void Attack()
    {
        if (!isAttacking)
            StartCoroutine(PerformAttack());
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;

        if (anim) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.15f); // windup

        hitbox.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.15f); // janela ativa

        hitbox.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.2f);  // recovery
        isAttacking = false;
    }

    protected override void OnDamageReceived(float amount)
    {
        if (anim) anim.SetTrigger("Hit");
    }

    protected override void OnDeath()
    {
        if (anim)
        {
            anim.SetTrigger("Die");
            // Wait for death animation before destroying, or destroy immediately if no animator
            StartCoroutine(DestroyAfterDeath());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private System.Collections.IEnumerator DestroyAfterDeath()
    {
        // Disable physics immediately so corpse doesn't interact
        if (TryGetComponent(out Collider2D col)) col.enabled = false;
        if (TryGetComponent(out Rigidbody2D rb))  rb.simulated = false;

        // Wait for the death animation clip to finish
        yield return null; // one frame for animator to switch state
        float deathLength = 0f;
        AnimatorClipInfo[] clips = anim.GetCurrentAnimatorClipInfo(0);
        if (clips.Length > 0) deathLength = clips[0].clip.length;

        yield return new WaitForSeconds(deathLength);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}