using System.Collections;
using UnityEngine;

/// <summary>
/// IA 2D do Arqueiro - sem NavMesh
/// Sprite nativo olha para a DIREITA
/// Comportamento: mantém distância ideal, atira flechas, faz roll ao ser encurralado
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class ArqueiroAI : MonoBehaviour
{
    // ───────────────────────────── REFERÊNCIAS ──────────────────────────────
    [Header("Referências")]
    public Transform player;
    public GameObject flechaPrefab;
    public Transform pontoDisparo;
    private Animator anim;
    private Rigidbody2D rb;

    // ───────────────────────────── VIDA ─────────────────────────────────────
    [Header("Vida")]
    public float vidaMaxima = 1000f;
    private float vidaAtual;

    // ───────────────────────────── DISTÂNCIAS ───────────────────────────────
    [Header("Distâncias")]
    public float distanciaRoll   = 2.5f;
    public float distanciaAtaque = 8f;
    public float distanciaMaxima = 14f;

    // ───────────────────────────── VELOCIDADE ───────────────────────────────
    [Header("Velocidade")]
    public float velocidade = 3f;

    // ───────────────────────────── ATAQUE ───────────────────────────────────
    [Header("Ataque")]
    public float cooldownAtaque      = 2f;
    public float tempoAteDisparar    = 0.9f; // Ajuste: momento certo da animação soltar a flecha
    public float tempoAposDisparar   = 0.35f; // Resto da animação após disparar
    private float timerAtaque        = 0f;
    private bool estaAtacando        = false;

    // ───────────────────────────── ROLL ─────────────────────────────────────
    [Header("Roll")]
    public float velocidadeRoll = 6f;
    public float duracaoRoll    = 0.4f;
    public float cooldownRoll   = 1.5f;
    private float timerRoll     = 0f;
    private bool estaRolando    = false;

    // ───────────────────────────── HURT ─────────────────────────────────────
    private bool estaHurt = false;

    // ───────────────────────────── ESTADO ───────────────────────────────────
    private enum Estado { Idle, Aproximando, Atirando, Roll, Hurt, Morto }
    private Estado estadoAtual = Estado.Idle;
    private bool morto = false;

    // ───────────────────── PARÂMETROS DO ANIMATOR ───────────────────────────
    private static readonly int ANIM_RUN    = Animator.StringToHash("run");
    private static readonly int ANIM_ATTACK = Animator.StringToHash("attack");
    private static readonly int ANIM_HURT   = Animator.StringToHash("hurt");
    private static readonly int ANIM_DEATH  = Animator.StringToHash("death");
    private static readonly int ANIM_ROLL   = Animator.StringToHash("roll");

    // ════════════════════════════════════════════════════════════════════════
    void Awake()
    {
        anim      = GetComponent<Animator>();
        rb        = GetComponent<Rigidbody2D>();
        vidaAtual = vidaMaxima;

        rb.gravityScale = 3f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        anim.SetBool(ANIM_RUN,    false);
        anim.SetBool(ANIM_ATTACK, false);
        anim.SetBool(ANIM_DEATH,  false);
    }

    // ════════════════════════════════════════════════════════════════════════
    void Update()
    {
        if (morto) return;

        if (timerAtaque > 0f) timerAtaque -= Time.deltaTime;
        if (timerRoll   > 0f) timerRoll   -= Time.deltaTime;

        if (estaAtacando || estaRolando || estaHurt) return;

        float dist = DistanciaAoPlayer();
        AtualizarEstado(dist);
    }

    void FixedUpdate()
    {
        if (morto || estaAtacando || estaHurt) return;
        Mover();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ESTADO
    // ════════════════════════════════════════════════════════════════════════
    void AtualizarEstado(float dist)
    {
        if (dist <= distanciaRoll && timerRoll <= 0f)
        {
            SetarEstado(Estado.Roll);
            return;
        }

        if (dist <= distanciaAtaque && dist > distanciaRoll && timerAtaque <= 0f)
        {
            SetarEstado(Estado.Atirando);
            return;
        }

        if (dist > distanciaAtaque)
        {
            SetarEstado(Estado.Aproximando);
            return;
        }

        SetarEstado(Estado.Idle);
    }

    void SetarEstado(Estado novo)
    {
        // Permite re-entrar no Atirando pra ciclar os ataques
        if (estadoAtual == novo && novo != Estado.Atirando) return;
        estadoAtual = novo;

        anim.SetBool(ANIM_RUN,    false);
        anim.SetBool(ANIM_ATTACK, false);

        switch (novo)
        {
            case Estado.Aproximando:
                anim.SetBool(ANIM_RUN, true);
                break;

            case Estado.Atirando:
                StartCoroutine(SequenciaAtaque());
                break;

            case Estado.Roll:
                StartCoroutine(SequenciaRoll());
                break;

            case Estado.Idle:
            default:
                break;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  MOVIMENTO 2D
    // ════════════════════════════════════════════════════════════════════════
    void Mover()
    {
        if (estaRolando) return;

        // Para completamente ao atacar
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

        float dirX  = player.position.x - transform.position.x;
        float moveX = Mathf.Sign(dirX) * velocidade;

        rb.linearVelocity = new Vector2(moveX, rb.linearVelocity.y);
        VirarSprite(dirX);
    }

    /// <summary>
    /// Sprite nativo olha para a DIREITA:
    ///   - Player à direita (dirX maior que 0) → escala positiva (padrão)
    ///   - Player à esquerda (dirX menor que 0) → escala negativa (espelha)
    /// </summary>
    void VirarSprite(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;

        Vector3 escala = transform.localScale;
        escala.x = Mathf.Abs(escala.x) * (dirX > 0 ? 1 : -1);
        transform.localScale = escala;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  CORROTINAS
    // ════════════════════════════════════════════════════════════════════════
    IEnumerator SequenciaAtaque()
    {
        estaAtacando      = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Vira pro player antes de atirar
        VirarSprite(player.position.x - transform.position.x);

        anim.SetBool(ANIM_ATTACK, true);

        // Aguarda o momento certo da animação (ajuste tempoAteDisparar no Inspector)
        yield return new WaitForSeconds(tempoAteDisparar);

        DispararFlecha();

        // Aguarda o resto da animação
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

        // Direção oposta ao player
        float dirFuga = -(player.position.x - transform.position.x);

        // Vira o sprite na direção do roll (oposta ao player)
        // Como sprite olha pra direita: dirFuga > 0 = direita = positivo
        VirarSprite(dirFuga);

        // Toca a animação ANTES de mover
        anim.SetTrigger(ANIM_ROLL);

        // Pequena pausa pra animação começar antes do movimento
        yield return new WaitForSeconds(0.05f);

        // Desloca durante o roll
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
        estaHurt          = true;
        estaAtacando      = false;
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
        morto             = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale   = 0f;

        anim.SetBool(ANIM_ATTACK, false);
        anim.SetBool(ANIM_RUN,    false);
        anim.SetBool(ANIM_DEATH,  true);

        yield return new WaitForSeconds(2f);

        Destroy(gameObject);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  FLECHA
    // ════════════════════════════════════════════════════════════════════════
    void DispararFlecha()
    {
        if (flechaPrefab == null || pontoDisparo == null) return;

        GameObject flecha = Instantiate(flechaPrefab, pontoDisparo.position, Quaternion.identity);

        Vector2 direcao = ((Vector2)player.position - (Vector2)pontoDisparo.position).normalized;

        Flecha scriptFlecha = flecha.GetComponent<Flecha>();
        if (scriptFlecha != null)
            scriptFlecha.Inicializar(direcao);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  VIDA
    // ════════════════════════════════════════════════════════════════════════
    public void ReceberDano(float dano)
    {
        if (morto) return;

        vidaAtual -= dano;

        if (vidaAtual <= 0f)
        {
            StartCoroutine(SequenciaMorte());
            return;
        }

        StopCoroutine(nameof(SequenciaAtaque));
        StartCoroutine(SequenciaHurt());
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaRoll);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanciaMaxima);
    }
}
