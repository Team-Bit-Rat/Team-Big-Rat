using System.Collections;
using UnityEngine;

/// <summary>
/// IA 2D de Inimigo Comum - sem NavMesh
/// Fluxo: Idle → (detecta player) → run → (chega perto) → attack → Idle
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class InimigoAI : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;
    private Animator anim;
    private Rigidbody2D rb;

    [Header("Vida")]
    public float vidaMaxima = 2000f;
    private float vidaAtual;

    [Header("Distâncias")]
    public float distanciaDeteccao = 10f;
    public float distanciaAtaque   = 1.5f;

    [Header("Velocidade")]
    public float velocidade = 4f;

    [Header("Ataque")]
    public float danoAtaque     = 500f;
    public float cooldownAtaque = 1f;
    private float timerAtaque   = 0f;
    private bool estaAtacando   = false;

    private bool estaHurt = false;

    private enum Estado { Idle, Correndo, Atacando, Hurt, Morto }
    private Estado estadoAtual = Estado.Idle;
    private bool morto = false;

    private static readonly int ANIM_RUN    = Animator.StringToHash("run");
    private static readonly int ANIM_ATTACK = Animator.StringToHash("attack");
    private static readonly int ANIM_HURT   = Animator.StringToHash("hurt");
    private static readonly int ANIM_DEATH  = Animator.StringToHash("Death");

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

    void Update()
    {
        if (morto) return;

        if (timerAtaque > 0f)
            timerAtaque -= Time.deltaTime;

        if (estaAtacando || estaHurt) return;

        float dist = DistanciaAoPlayer();
        AtualizarEstado(dist);
    }

    void FixedUpdate()
    {
        if (morto || estaAtacando || estaHurt) return;
        Mover();
    }

    void AtualizarEstado(float dist)
    {
        if (dist <= distanciaAtaque && timerAtaque <= 0f)
            SetarEstado(Estado.Atacando);
        else if (dist <= distanciaDeteccao)
            SetarEstado(Estado.Correndo);
        else
            SetarEstado(Estado.Idle);
    }

    void SetarEstado(Estado novo)
    {
        if (estadoAtual == novo) return;
        estadoAtual = novo;

        anim.SetBool(ANIM_RUN,    false);
        anim.SetBool(ANIM_ATTACK, false);

        switch (novo)
        {
            case Estado.Correndo:
                anim.SetBool(ANIM_RUN, true);
                break;
            case Estado.Atacando:
                StartCoroutine(SequenciaAtaque());
                break;
            case Estado.Idle:
            default:
                break;
        }
    }

    void Mover()
    {
        // Para o X em qualquer estado que não seja correr
        if (estadoAtual != Estado.Correndo)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float dirX  = player.position.x - transform.position.x;
        float moveX = Mathf.Sign(dirX) * velocidade;

        rb.linearVelocity = new Vector2(moveX, rb.linearVelocity.y);
        VirarSprite(dirX);
    }

    void VirarSprite(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        Vector3 escala = transform.localScale;
        escala.x = Mathf.Abs(escala.x) * (dirX > 0 ? 1 : -1);
        transform.localScale = escala;
    }

    IEnumerator SequenciaAtaque()
    {
        estaAtacando      = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // Para imediatamente

        anim.SetBool(ANIM_ATTACK, true);
        yield return new WaitForSeconds(1f);
        anim.SetBool(ANIM_ATTACK, false);

        if (DistanciaAoPlayer() <= distanciaAtaque + 0.5f)
            AplicarDanoNoPlayer();

        timerAtaque  = cooldownAtaque;
        estaAtacando = false;
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

    void AplicarDanoNoPlayer()
    {
        // player.GetComponent<PlayerHealth>()?.ReceberDano(danoAtaque);
        Debug.Log($"[Inimigo] Causou {danoAtaque} de dano!");
    }

    float DistanciaAoPlayer()
    {
        return Vector2.Distance(transform.position, player.position);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);
    }
}
