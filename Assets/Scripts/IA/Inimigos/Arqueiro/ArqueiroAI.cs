using System.Collections;
using UnityEngine;

/// <summary>
/// IA 2D do Arqueiro — sprite olha para a DIREITA
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class ArqueiroAI : Entity
{
    [Header("Referências")]
    public Transform player;
    public GameObject flechaPrefab;
    public Transform pontoDisparo;
    private Animator anim;
    private Rigidbody2D rb;

    [Header("Vida")]
    public float vidaMaxima = 1000f;

    [Header("Distâncias")]
    public float distanciaRoll   = 2.5f;
    public float distanciaAtaque = 8f;
    public float distanciaMaxima = 14f;

    [Header("Velocidade")]
    public float velocidade = 3f;

    [Header("Ataque")]
    public float cooldownAtaque    = 2f;
    public float tempoAteDisparar  = 0.9f;
    public float tempoAposDisparar = 0.35f;
    private float timerAtaque      = 0f;
    private bool estaAtacando      = false;

    [Header("Roll")]
    public float velocidadeRoll = 6f;
    public float duracaoRoll    = 0.4f;
    public float cooldownRoll   = 1.5f;
    private float timerRoll     = 0f;
    private bool estaRolando    = false;

    private bool estaHurt = false;

    private enum Estado { Idle, Aproximando, Atirando, Roll, Hurt, Morto }
    private Estado estadoAtual = Estado.Idle;

    private static readonly int ANIM_RUN    = Animator.StringToHash("run");
    private static readonly int ANIM_ATTACK = Animator.StringToHash("attack");
    private static readonly int ANIM_HURT   = Animator.StringToHash("hurt");
    private static readonly int ANIM_DEATH  = Animator.StringToHash("death");
    private static readonly int ANIM_ROLL   = Animator.StringToHash("roll");

    // ════════════════════════════════════════════════════════════════════════
    protected override void Awake()
    {
        maxHP = vidaMaxima;
        base.Awake();

        anim = GetComponent<Animator>();
        rb   = GetComponent<Rigidbody2D>();

        rb.gravityScale = 3f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        anim.SetBool(ANIM_RUN,    false);
        anim.SetBool(ANIM_ATTACK, false);
        anim.SetBool(ANIM_DEATH,  false);

        GameObject hb = new GameObject("Hurtbox");
        hb.transform.SetParent(transform, false);
        hb.transform.localPosition = Vector3.zero;
        var hbCol       = hb.AddComponent<BoxCollider2D>();
        hbCol.size      = new Vector2(0.9f, 1.5f);
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
        if (timerRoll   > 0f) timerRoll   -= Time.deltaTime;
        if (estaAtacando || estaRolando || estaHurt) return;
        AtualizarEstado(DistanciaAoPlayer());
    }

    void FixedUpdate()
    {
        if (isDead || estaAtacando || estaHurt) return;
        Mover();
    }

    void AtualizarEstado(float dist)
    {
        if (dist <= distanciaRoll && timerRoll <= 0f)
        { SetarEstado(Estado.Roll); return; }

        if (dist <= distanciaAtaque && dist > distanciaRoll && timerAtaque <= 0f)
        { SetarEstado(Estado.Atirando); return; }

        if (dist > distanciaAtaque)
        { SetarEstado(Estado.Aproximando); return; }

        SetarEstado(Estado.Idle);
    }

    void SetarEstado(Estado novo)
    {
        if (estadoAtual == novo && novo != Estado.Atirando) return;
        estadoAtual = novo;

        anim.SetBool(ANIM_RUN,    false);
        anim.SetBool(ANIM_ATTACK, false);

        switch (novo)
        {
            case Estado.Aproximando: anim.SetBool(ANIM_RUN, true); break;
            case Estado.Atirando:    StartCoroutine(SequenciaAtaque()); break;
            case Estado.Roll:        StartCoroutine(SequenciaRoll()); break;
        }
    }

    void Mover()
    {
        if (estaRolando) return;
        if (estaAtacando || estadoAtual == Estado.Atirando || estadoAtual == Estado.Idle)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        if (estadoAtual != Estado.Aproximando)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        float dirX = player.position.x - transform.position.x;
        rb.linearVelocity = new Vector2(Mathf.Sign(dirX) * velocidade, rb.linearVelocity.y);
        VirarSprite(dirX);
    }

    void VirarSprite(float dirX) // sprite olha para DIREITA
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        Vector3 e = transform.localScale;
        e.x = Mathf.Abs(e.x) * (dirX > 0 ? 1 : -1);
        transform.localScale = e;
    }

    IEnumerator SequenciaAtaque()
    {
        estaAtacando = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        VirarSprite(player.position.x - transform.position.x);

        anim.SetBool(ANIM_ATTACK, true);
        yield return new WaitForSeconds(tempoAteDisparar);
        DispararFlecha();
        yield return new WaitForSeconds(tempoAposDisparar);
        anim.SetBool(ANIM_ATTACK, false);

        timerAtaque  = cooldownAtaque;
        estaAtacando = false;
        SetarEstado(Estado.Idle);
    }

    IEnumerator SequenciaRoll()
    {
        estaRolando = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        float dirFuga = -(player.position.x - transform.position.x);
        VirarSprite(dirFuga);
        anim.SetTrigger(ANIM_ROLL);
        yield return new WaitForSeconds(0.05f);

        float timer = 0f;
        while (timer < duracaoRoll)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(dirFuga) * velocidadeRoll, rb.linearVelocity.y);
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        timerRoll   = cooldownRoll;
        estaRolando = false;
        SetarEstado(Estado.Idle);
    }

    IEnumerator SequenciaHurt()
    {
        estaHurt     = true;
        estaAtacando = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        anim.SetBool(ANIM_ATTACK, false);
        anim.SetBool(ANIM_RUN,    false);
        anim.SetTrigger(ANIM_HURT);

        yield return new WaitForSeconds(0.58f);
        estaHurt = false;
        SetarEstado(Estado.Idle);
    }

    IEnumerator SequenciaMorte()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale   = 0f;

        anim.SetBool(ANIM_ATTACK, false);
        anim.SetBool(ANIM_RUN,    false);
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

    void DispararFlecha()
    {
        if (flechaPrefab == null || pontoDisparo == null) return;
        GameObject flecha = Instantiate(flechaPrefab, pontoDisparo.position, Quaternion.identity);
        Vector2 direcao = ((Vector2)player.position - (Vector2)pontoDisparo.position).normalized;
        flecha.GetComponent<Flecha>()?.Inicializar(direcao);
    }

    float DistanciaAoPlayer() => Vector2.Distance(transform.position, player.position);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaRoll);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanciaMaxima);
    }
}
