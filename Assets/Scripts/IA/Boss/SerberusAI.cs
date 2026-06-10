using System.Collections;
using UnityEngine;

/// <summary>
/// IA 2D do Boss Serberus
/// Fluxo: Fall → (pousa) → Intro → Idle → IA ativa
/// Sprite olha para a ESQUERDA
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class SerberusAI : Entity
{
    [Header("Referências")]
    public Transform player;
    private Animator anim;
    private Rigidbody2D rb;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool estaNoChao = false;
    private bool jaPousou   = false;

    [Header("Vida")]
    public float vidaMaxima = 500f;

    [Header("Distâncias")]
    public float distanciaDeteccao = 12f;
    public float distanciaCorrer   = 6f;
    public float distanciaAtaque   = 1.5f;

    [Header("Velocidades")]
    public float velocidadeAndar  = 2f;
    public float velocidadeCorrer = 5f;

    [Header("Ataque")]
    public float danoAtaque      = 40f;
    public float cooldownAtaque  = 2f;
    public float tempoPrepAtaque = 0.8f;
    private float timerAtaque    = 0f;
    private bool estaAtacando    = false;

    [Header("Grito")]
    public float intervaloCry = 10f;
    private float timerGrito  = 0f;
    private bool estaGritando = false;

    [Header("Ataque Especial (Uto)")]
    public float duracaoUto    = 2f;
    private bool estaUsandoUto = false;

    private enum Estado { Fall, Intro, Idle, Andando, Correndo, PrepAtaque, Morto }
    private Estado estadoAtual  = Estado.Fall;
    private bool introCompleta  = false;

    private static readonly int ANIM_ANDANDO  = Animator.StringToHash("andando");
    private static readonly int ANIM_PREP_RUN = Animator.StringToHash("Prep");
    private static readonly int ANIM_CORRENDO = Animator.StringToHash("Correndo");
    private static readonly int ANIM_PREP_ATK = Animator.StringToHash("Prepataque");
    private static readonly int ANIM_ATACANDO = Animator.StringToHash("Atacando");
    private static readonly int ANIM_UTO      = Animator.StringToHash("Uto");
    private static readonly int ANIM_GRITO    = Animator.StringToHash("Grito");
    private static readonly int ANIM_INTRO    = Animator.StringToHash("intro");
    private static readonly int ANIM_MORRENDO = Animator.StringToHash("morrendo");
    private static readonly int ANIM_NO_CHAO  = Animator.StringToHash("NoChao");

    // ════════════════════════════════════════════════════════════════════════
    protected override void Awake()
    {
        maxHP = vidaMaxima;
        base.Awake();

        anim = GetComponent<Animator>();
        rb   = GetComponent<Rigidbody2D>();

        rb.gravityScale = 3f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        anim.SetBool(ANIM_NO_CHAO,  false);
        anim.SetBool(ANIM_GRITO,    false);
        anim.SetBool(ANIM_UTO,      false);
        anim.SetBool(ANIM_ANDANDO,  false);
        anim.SetBool(ANIM_PREP_RUN, false);
        anim.SetBool(ANIM_CORRENDO, false);

        GameObject hb = new GameObject("Hurtbox");
        hb.transform.SetParent(transform, false);
        hb.transform.localPosition = Vector3.zero;
        var hbCol       = hb.AddComponent<BoxCollider2D>();
        hbCol.size      = new Vector2(1.2f, 1.8f);
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
        ChecarChao();
        if (!introCompleta) return;

        VirarSprite(player.position.x - transform.position.x);
        float dist = DistanciaAoPlayer();
        AtualizarTimerGrito(dist);
        AtualizarEstado(dist);
    }

    void FixedUpdate()
    {
        if (isDead || !introCompleta || estaAtacando || estaGritando || estaUsandoUto) return;
        Mover();
    }

    void ChecarChao()
    {
        estaNoChao = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        anim.SetBool(ANIM_NO_CHAO, estaNoChao);

        if (estaNoChao && !jaPousou)
        {
            jaPousou          = true;
            rb.linearVelocity = Vector2.zero;
            StartCoroutine(ExecutarIntro());
        }
    }

    void AtualizarEstado(float dist)
    {
        if (estaAtacando || estaGritando || estaUsandoUto) return;

        if (dist <= distanciaAtaque && timerAtaque <= 0f)
            SetarEstado(Estado.PrepAtaque);
        else if (dist <= distanciaCorrer)
            SetarEstado(Estado.Correndo);
        else if (dist <= distanciaDeteccao)
            SetarEstado(Estado.Andando);
        else
            SetarEstado(Estado.Idle);
    }

    void SetarEstado(Estado novo)
    {
        if (estadoAtual == novo) return;
        estadoAtual = novo;

        anim.SetBool(ANIM_ANDANDO,  false);
        anim.SetBool(ANIM_PREP_RUN, false);
        anim.SetBool(ANIM_CORRENDO, false);

        switch (novo)
        {
            case Estado.Andando:    anim.SetBool(ANIM_ANDANDO, true); break;
            case Estado.Correndo:   StartCoroutine(IniciarCorrida()); break;
            case Estado.PrepAtaque: if (!estaAtacando) StartCoroutine(SequenciaAtaque()); break;
        }
    }

    void Mover()
    {
        if (estadoAtual != Estado.Andando && estadoAtual != Estado.Correndo)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        float speed = estadoAtual == Estado.Correndo ? velocidadeCorrer : velocidadeAndar;
        float dirX  = player.position.x - transform.position.x;
        rb.linearVelocity = new Vector2(Mathf.Sign(dirX) * speed, rb.linearVelocity.y);
    }

    void VirarSprite(float dirX) // sprite olha para ESQUERDA
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        Vector3 e = transform.localScale;
        e.x = Mathf.Abs(e.x) * (dirX > 0 ? -1 : 1);
        transform.localScale = e;
    }

    IEnumerator ExecutarIntro()
    {
        estadoAtual    = Estado.Intro;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        anim.SetTrigger(ANIM_INTRO);
        yield return new WaitForSeconds(3f);

        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 1f;
        introCompleta   = true;
        SetarEstado(Estado.Idle);
    }

    IEnumerator IniciarCorrida()
    {
        Estado estadoSalvo = estadoAtual;
        anim.SetBool(ANIM_PREP_RUN, true);
        yield return new WaitForSeconds(0.5f);
        anim.SetBool(ANIM_PREP_RUN, false);

        if (estadoAtual == estadoSalvo && !estaAtacando && !estaGritando)
            anim.SetBool(ANIM_CORRENDO, true);
    }

    IEnumerator SequenciaAtaque()
    {
        estaAtacando = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        anim.SetTrigger(ANIM_PREP_ATK);
        yield return new WaitForSeconds(tempoPrepAtaque);

        anim.SetBool(ANIM_ATACANDO, true);
        yield return new WaitForSeconds(0.8f);
        anim.SetBool(ANIM_ATACANDO, false);

        if (DistanciaAoPlayer() <= distanciaAtaque + 0.5f)
            AplicarDanoNoPlayer();

        yield return new WaitForSeconds(0.3f);
        timerAtaque  = cooldownAtaque;
        estaAtacando = false;
        SetarEstado(Estado.Idle);
    }

    IEnumerator ExecutarGrito()
    {
        estaGritando = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        anim.SetBool(ANIM_GRITO, true);
        yield return new WaitForSeconds(2.5f);
        anim.SetBool(ANIM_GRITO, false);

        estaGritando = false;
        SetarEstado(Estado.Idle);
    }

    public void UsarAtaqueEspecial()
    {
        if (!estaUsandoUto && !estaAtacando && !estaGritando && introCompleta)
            StartCoroutine(ExecutarUto());
    }

    IEnumerator ExecutarUto()
    {
        estaUsandoUto = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        anim.SetBool(ANIM_UTO, true);
        yield return new WaitForSeconds(duracaoUto);
        anim.SetBool(ANIM_UTO, false);

        estaUsandoUto = false;
        SetarEstado(Estado.Idle);
    }

    IEnumerator Morrer()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger(ANIM_MORRENDO);
        yield return new WaitForSeconds(3f);
        gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════════
    void AtualizarTimerGrito(float dist)
    {
        if (timerAtaque > 0f) timerAtaque -= Time.deltaTime;

        if (!estaGritando && !estaUsandoUto && !estaAtacando && dist <= distanciaDeteccao)
        {
            timerGrito += Time.deltaTime;
            if (timerGrito >= intervaloCry)
            {
                timerGrito = 0f;
                StartCoroutine(ExecutarGrito());
            }
        }
    }

    public void ReceberDano(float dano)
    {
        if (isDead) return;
        currentHP -= dano;
        if (currentHP <= 0f)
            StartCoroutine(Morrer());
    }

    void AplicarDanoNoPlayer()
    {
        player.GetComponent<PlayerHealth>()?.ReceberDano(danoAtaque);
    }

    float DistanciaAoPlayer() => Vector2.Distance(transform.position, player.position);

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = estaNoChao ? Color.green : Color.white;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, distanciaCorrer);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);
    }
}
