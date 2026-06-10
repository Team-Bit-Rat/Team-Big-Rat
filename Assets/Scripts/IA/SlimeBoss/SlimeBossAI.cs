using System.Collections;
using UnityEngine;

/// <summary>
/// IA 2D do SlimeBoss — sprite olha para a ESQUERDA
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class SlimeBossAI : Entity
{
    [Header("Referências")]
    public Transform player;
    private Animator anim;
    private Rigidbody2D rb;

    [Header("Vida")]
    public float vidaMaxima = 3000f;

    [Header("Distâncias")]
    public float distanciaDeteccao = 10f;
    public float distanciaAtaque   = 1.5f;

    [Header("Velocidade")]
    public float velocidade = 3f;

    [Header("Ataque")]
    public float danoAtaque     = 1000f;
    public float cooldownAtaque = 1f;
    private float timerAtaque   = 0f;
    private bool estaAtacando   = false;
    private bool estaHurt       = false;

    private enum Estado { Idle, Andando, Atacando, Hurt, Morto }
    private Estado estadoAtual = Estado.Idle;

    private static readonly int ANIM_WALK    = Animator.StringToHash("walk");
    private static readonly int ANIM_CLEAVE  = Animator.StringToHash("cleave");
    private static readonly int ANIM_TAKEHIT = Animator.StringToHash("take hit");
    private static readonly int ANIM_DEATH   = Animator.StringToHash("death");

    // ════════════════════════════════════════════════════════════════════════
    protected override void Awake()
    {
        maxHP = vidaMaxima;
        base.Awake();

        anim = GetComponent<Animator>();
        rb   = GetComponent<Rigidbody2D>();

        rb.gravityScale = 3f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        anim.SetBool(ANIM_WALK,   false);
        anim.SetBool(ANIM_CLEAVE, false);
        anim.SetBool(ANIM_DEATH,  false);

        GameObject hb = new GameObject("Hurtbox");
        hb.transform.SetParent(transform, false);
        hb.transform.localPosition = Vector3.zero;
        var hbCol       = hb.AddComponent<BoxCollider2D>();
        hbCol.size      = new Vector2(1.5f, 1.8f);
        hbCol.isTrigger = true;
        hb.layer        = gameObject.layer;
    }

    public override void ReceiveDmg(float amount, float stunDuration = 0.2f)
    {
        ReceberDano(amount);
    }

    // ════════════════════════════════════════════════════════════════════════
    void Update()
    {
        if (isDead) return;
        if (timerAtaque > 0f) timerAtaque -= Time.deltaTime;
        if (estaAtacando || estaHurt) return;
        AtualizarEstado(DistanciaAoPlayer());
    }

    void FixedUpdate()
    {
        if (isDead || estaAtacando || estaHurt) return;
        Mover();
    }

    void AtualizarEstado(float dist)
    {
        if (dist <= distanciaAtaque && timerAtaque <= 0f)
            SetarEstado(Estado.Atacando);
        else if (dist <= distanciaDeteccao)
            SetarEstado(Estado.Andando);
        else
            SetarEstado(Estado.Idle);
    }

    void SetarEstado(Estado novo)
    {
        if (estadoAtual == novo) return;
        estadoAtual = novo;

        anim.SetBool(ANIM_WALK,   false);
        anim.SetBool(ANIM_CLEAVE, false);

        switch (novo)
        {
            case Estado.Andando:  anim.SetBool(ANIM_WALK, true); break;
            case Estado.Atacando: StartCoroutine(SequenciaAtaque()); break;
        }
    }

    void Mover()
    {
        if (estadoAtual != Estado.Andando)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        float dirX = player.position.x - transform.position.x;
        rb.linearVelocity = new Vector2(Mathf.Sign(dirX) * velocidade, rb.linearVelocity.y);
        VirarSprite(dirX);
    }

    void VirarSprite(float dirX) // sprite olha para ESQUERDA
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        Vector3 e = transform.localScale;
        e.x = Mathf.Abs(e.x) * (dirX > 0 ? -1 : 1);
        transform.localScale = e;
    }

    IEnumerator SequenciaAtaque()
    {
        estaAtacando = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        anim.SetBool(ANIM_CLEAVE, true);
        yield return new WaitForSeconds(1.25f);
        anim.SetBool(ANIM_CLEAVE, false);

        if (DistanciaAoPlayer() <= distanciaAtaque + 0.5f)
            AplicarDanoNoPlayer();

        timerAtaque  = cooldownAtaque;
        estaAtacando = false;
        SetarEstado(Estado.Idle);
    }

    IEnumerator SequenciaHurt()
    {
        estaHurt     = true;
        estaAtacando = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        anim.SetBool(ANIM_CLEAVE, false);
        anim.SetBool(ANIM_WALK,   false);
        anim.SetTrigger(ANIM_TAKEHIT);

        yield return new WaitForSeconds(0.33f);
        estaHurt = false;
        SetarEstado(Estado.Idle);
    }

    IEnumerator SequenciaMorte()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale   = 0f;

        anim.SetBool(ANIM_CLEAVE, false);
        anim.SetBool(ANIM_WALK,   false);
        anim.SetBool(ANIM_DEATH,  true);

        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    // ════════════════════════════════════════════════════════════════════════
    public void ReceberDano(float dano)
    {
        if (isDead) return;
        currentHP -= dano;

        if (currentHP <= 0f)
        {
            StartCoroutine(SequenciaMorte());
            return;
        }

        StopCoroutine(nameof(SequenciaAtaque));
        StartCoroutine(SequenciaHurt());
    }

    void AplicarDanoNoPlayer()
    {
        player.GetComponent<PlayerHealth>()?.ReceberDano(danoAtaque);
    }

    float DistanciaAoPlayer() => Vector2.Distance(transform.position, player.position);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);
    }
}
