using System.Collections;
using UnityEngine;

/// <summary>
/// IA 2D do Boss Serberus - sem NavMesh
/// Fluxo de entrada: Fall → (pousa no chão) → Intro → Idle → IA ativa
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class SerberusAI : MonoBehaviour
{
    // ───────────────────────────── REFERÊNCIAS ──────────────────────────────
    [Header("Referências")]
    public Transform player;
    private Animator anim;
    private Rigidbody2D rb;

    // ───────────────────────────── GROUND CHECK ─────────────────────────────
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private bool estaNoChao = false;
    private bool jaPousou   = false;

    // ───────────────────────────── ATRIBUTOS ────────────────────────────────
    [Header("Vida")]
    public float vidaMaxima = 500f;
    private float vidaAtual;

    // ───────────────────────────── DISTÂNCIAS ───────────────────────────────
    [Header("Distâncias")]
    public float distanciaDeteccao = 12f;
    public float distanciaCorrer   = 6f;
    public float distanciaAtaque   = 1.5f;

    // ───────────────────────────── VELOCIDADES ──────────────────────────────
    [Header("Velocidades")]
    public float velocidadeAndar  = 2f;
    public float velocidadeCorrer = 5f;

    // ───────────────────────────── ATAQUE ───────────────────────────────────
    [Header("Ataque")]
    public float danoAtaque      = 40f;
    public float cooldownAtaque  = 2f;
    public float tempoPrepAtaque = 0.8f;
    private float timerAtaque    = 0f;
    private bool estaAtacando    = false;

    // ───────────────────────────── GRITO ────────────────────────────────────
    [Header("Grito")]
    public float intervaloCry = 10f;
    private float timerGrito  = 0f;
    private bool estaGritando = false;

    // ───────────────────────────── ATAQUE ESPECIAL ──────────────────────────
    [Header("Ataque Especial (Uto)")]
    public float duracaoUto    = 2f;
    private bool estaUsandoUto = false;

    // ───────────────────────────── ESTADO ───────────────────────────────────
    private enum Estado { Fall, Intro, Idle, Andando, Correndo, PrepAtaque, Morto }
    private Estado estadoAtual = Estado.Fall;
    private bool morto         = false;
    private bool introCompleta = false;

    // ───────────────────── PARÂMETROS DO ANIMATOR ───────────────────────────
    // O Animator cuida sozinho do fluxo PrepRun → Run usando Prep e Correndo
    // Basta setar Prep=true quando quiser correr e Correndo=true depois
    // O código só seta Correndo — o Animator faz PrepRun→Run automaticamente
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
    void Awake()
    {
        anim      = GetComponent<Animator>();
        rb        = GetComponent<Rigidbody2D>();
        vidaAtual = vidaMaxima;

        rb.gravityScale = 3f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        anim.SetBool(ANIM_NO_CHAO,  false);
        anim.SetBool(ANIM_GRITO,    false);
        anim.SetBool(ANIM_UTO,      false);
        anim.SetBool(ANIM_ANDANDO,  false);
        anim.SetBool(ANIM_PREP_RUN, false);
        anim.SetBool(ANIM_CORRENDO, false);
    }

    // ════════════════════════════════════════════════════════════════════════
    void Update()
    {
        if (morto) return;

        ChecarChao();

        if (!introCompleta) return;

        VirarSprite(player.position.x - transform.position.x);

        float dist = DistanciaAoPlayer();
        AtualizarTimerGrito(dist);
        AtualizarEstado(dist);
    }

    void FixedUpdate()
    {
        if (morto || !introCompleta || estaAtacando || estaGritando || estaUsandoUto) return;
        Mover();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  GROUND CHECK
    // ════════════════════════════════════════════════════════════════════════
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

    // ════════════════════════════════════════════════════════════════════════
    //  ESTADO
    // ════════════════════════════════════════════════════════════════════════
    void AtualizarEstado(float dist)
    {
        if (estaAtacando || estaGritando || estaUsandoUto) return;

        if (dist <= distanciaAtaque && timerAtaque <= 0f)
        {
            SetarEstado(Estado.PrepAtaque);
        }
        else if (dist <= distanciaCorrer)
        {
            SetarEstado(Estado.Correndo);
        }
        else if (dist <= distanciaDeteccao)
        {
            SetarEstado(Estado.Andando);
        }
        else
        {
            SetarEstado(Estado.Idle);
        }
    }

    void SetarEstado(Estado novo)
    {
        if (estadoAtual == novo) return;
        estadoAtual = novo;

        // Reseta todos os bools de movimento primeiro
        anim.SetBool(ANIM_ANDANDO,  false);
        anim.SetBool(ANIM_PREP_RUN, false);
        anim.SetBool(ANIM_CORRENDO, false);

        switch (novo)
        {
            case Estado.Andando:
                anim.SetBool(ANIM_ANDANDO, true);
                break;

            case Estado.Correndo:
                // Seta Prep=true → Animator vai pra PrepRun
                // Quando PrepRun terminar, seta Correndo=true → Animator vai pra Run
                StartCoroutine(IniciarCorrida());
                break;

            case Estado.PrepAtaque:
                if (!estaAtacando)
                    StartCoroutine(SequenciaAtaque());
                break;

            case Estado.Idle:
            default:
                // Tudo já foi resetado acima
                break;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  MOVIMENTO 2D - PLATAFORMA (só eixo X)
    // ════════════════════════════════════════════════════════════════════════
    void Mover()
    {
        if (estadoAtual != Estado.Andando && estadoAtual != Estado.Correndo)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float speed = estadoAtual == Estado.Correndo ? velocidadeCorrer : velocidadeAndar;
        float dirX  = player.position.x - transform.position.x;
        float moveX = Mathf.Sign(dirX) * speed;

        rb.linearVelocity = new Vector2(moveX, rb.linearVelocity.y);
    }

    void VirarSprite(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;

        Vector3 escala = transform.localScale;
        escala.x = Mathf.Abs(escala.x) * (dirX > 0 ? -1 : 1);
        transform.localScale = escala;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CORROTINAS
    // ════════════════════════════════════════════════════════════════════════
    IEnumerator ExecutarIntro()
    {
        estadoAtual    = Estado.Intro;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        anim.SetTrigger(ANIM_INTRO);
        yield return new WaitForSeconds(3f);

        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 1f;

        introCompleta = true;
        SetarEstado(Estado.Idle);
    }

    IEnumerator IniciarCorrida()
    {
        // Trava o estado pra não ser interrompido durante o PrepRun
        Estado estadoSalvo = estadoAtual;

        anim.SetBool(ANIM_PREP_RUN, true);
        yield return new WaitForSeconds(0.5f);
        anim.SetBool(ANIM_PREP_RUN, false);

        // Só começa a correr se ainda deve estar correndo
        if (estadoAtual == estadoSalvo && !estaAtacando && !estaGritando)
        {
            anim.SetBool(ANIM_CORRENDO, true);
        }
    }

    IEnumerator SequenciaAtaque()
    {
        estaAtacando      = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        anim.SetTrigger(ANIM_PREP_ATK);
        yield return new WaitForSeconds(tempoPrepAtaque);

        anim.SetTrigger(ANIM_ATACANDO);
        yield return new WaitForSeconds(0.3f);

        if (DistanciaAoPlayer() <= distanciaAtaque + 0.5f)
            AplicarDanoNoPlayer();

        yield return new WaitForSeconds(0.5f);

        timerAtaque  = cooldownAtaque;
        estaAtacando = false;
        SetarEstado(Estado.Idle);
    }

    IEnumerator ExecutarGrito()
    {
        estaGritando      = true;
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
        estaUsandoUto     = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        anim.SetBool(ANIM_UTO, true);
        yield return new WaitForSeconds(duracaoUto);
        anim.SetBool(ANIM_UTO, false);

        estaUsandoUto = false;
        SetarEstado(Estado.Idle);
    }

    IEnumerator Morrer()
    {
        morto             = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger(ANIM_MORRENDO);
        yield return new WaitForSeconds(3f);
        gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TIMER DO GRITO
    // ════════════════════════════════════════════════════════════════════════
    void AtualizarTimerGrito(float dist)
    {
        if (timerAtaque > 0f)
            timerAtaque -= Time.deltaTime;

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

    // ════════════════════════════════════════════════════════════════════════
    //  VIDA
    // ════════════════════════════════════════════════════════════════════════
    public void ReceberDano(float dano)
    {
        if (morto) return;
        vidaAtual -= dano;
        if (vidaAtual <= 0f)
            StartCoroutine(Morrer());
    }

    void AplicarDanoNoPlayer()
    {
        // player.GetComponent<PlayerHealth>()?.ReceberDano(danoAtaque);
        Debug.Log($"[Serberus] Causou {danoAtaque} de dano!");
    }

    float DistanciaAoPlayer()
    {
        return Vector2.Distance(transform.position, player.position);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  GIZMOS
    // ════════════════════════════════════════════════════════════════════════
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
