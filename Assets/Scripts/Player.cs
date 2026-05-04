using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float velocidadeAndando = 5f;
    [SerializeField] private float velocidadeCorrendo = 10f;
    [SerializeField] private float aceleracao = 10f;

    [Header("Pulo")]
    [SerializeField] private float forcaPulo = 10f;
    [SerializeField] private int pulosExtras = 0;
    [SerializeField] private float coyoteTime = 0.1f;

    [Header("Dash")]
    [SerializeField] private float dashForca = 15f;
    [SerializeField] private float dashDuracao = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Ground")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Teclas")]
    [SerializeField] private KeyCode teclaCorrida = KeyCode.LeftShift;
    [SerializeField] private KeyCode teclaDash = KeyCode.LeftAlt;

    [Header("Componentes")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Animator anim;

    private Rigidbody2D rb;

    // ================= ESTADOS =================
    private float inputX;
    private float velocidadeAtual;
    private bool viradoDireita;

    private bool noChao;
    private bool correndo;

    private bool dashando;
    private bool podeDash = true;

    private bool atacando;

    private int pulosRestantes;
    private float coyoteTimer;

    // Combo
    private int comboAtual;
    private float tempoUltimoAtaque;
    private readonly float tempoMaximoCombo = 0.8f;

    // ================= INIT =================
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (!sprite) sprite = GetComponent<SpriteRenderer>();
        if (!anim) anim = GetComponent<Animator>();

        rb.gravityScale = 3f;
    }

    // ================= UPDATE =================
    void Update()
    {
        LerInputs();
        GerenciarGround();
        GerenciarPulo();
        GerenciarCombo();
        AtualizarAnimacoes();
    }

    void FixedUpdate()
    {
        if (dashando || atacando) return;
        Movimentar();
    }

    // ================= INPUT =================
    void LerInputs()
    {
        if (dashando) return;

        inputX = Input.GetAxisRaw("Horizontal");
        correndo = Input.GetKey(teclaCorrida) && noChao && Mathf.Abs(inputX) > 0;

        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J)) && !atacando)
            Atacar();

        if (Input.GetKeyDown(teclaDash) && podeDash && !atacando)
            StartCoroutine(Dash());
    }

    // ================= MOVIMENTO =================
    void Movimentar()
    {
        float velocidadeAlvo = correndo ? velocidadeCorrendo : velocidadeAndando;
        float alvo = inputX * velocidadeAlvo;

        velocidadeAtual = Mathf.Lerp(velocidadeAtual, alvo, aceleracao * Time.fixedDeltaTime);

        if (Mathf.Abs(inputX) < 0.01f)
            velocidadeAtual = Mathf.Lerp(velocidadeAtual, 0, aceleracao * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(velocidadeAtual, rb.linearVelocity.y);

        // FIX: só vira quando não está atacando
        if (!atacando)
        {
            if (inputX > 0 && !viradoDireita)
            {
                viradoDireita = true;
                sprite.flipX = true;
            }
            else if (inputX < 0 && viradoDireita)
            {
                viradoDireita = false;
                sprite.flipX = false;
            }
        }
    }

    // ================= DASH =================
    IEnumerator Dash()
    {
        podeDash = false;
        dashando = true;

        float direcao = sprite.flipX ? 1f : -1f;

        rb.linearVelocity = Vector2.zero;
        rb.linearVelocity = new Vector2(direcao * dashForca, 0);

        anim.SetTrigger("Dash");

        yield return new WaitForSeconds(dashDuracao);

        dashando = false;

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        yield return new WaitForSeconds(dashCooldown);
        podeDash = true;
    }

    // ================= PULO =================
    void GerenciarPulo()
    {
        coyoteTimer -= Time.deltaTime;

        if (Input.GetButtonDown("Jump"))
        {
            if (coyoteTimer > 0 || pulosRestantes > 0)
            {
                if (!noChao)
                {
                    pulosRestantes--;
                    anim.SetBool("Pulando Denovo", true);
                }
                else
                {
                    anim.SetBool("Pulando Denovo", false);
                }

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(Vector2.up * forcaPulo, ForceMode2D.Impulse);
                coyoteTimer = 0;
            }
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
    }

    // ================= CHÃO =================
    void GerenciarGround()
    {
        bool estavaNoChao = noChao;

        noChao = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (noChao && !estavaNoChao)
        {
            coyoteTimer = coyoteTime;
            pulosRestantes = pulosExtras;

            anim.SetBool("Pulando Denovo", false);
            ///anim.SetTrigger("Land");
        }

        if (noChao && rb.linearVelocity.y <= 0)
            pulosRestantes = pulosExtras;
    }

    // ================= ATAQUE =================
    void Atacar()
    {
        bool dentroDoTempo = (Time.time - tempoUltimoAtaque) <= tempoMaximoCombo;

        comboAtual = (comboAtual == 0 || dentroDoTempo)
            ? Mathf.Min(comboAtual + 1, 3)
            : 1;

        tempoUltimoAtaque = Time.time;
        StartCoroutine(ExecutarAtaque());
    }

    IEnumerator ExecutarAtaque()
    {
        atacando = true;

        // FIX: trava movimento lateral durante o ataque
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        string trigger = comboAtual switch
        {
            1 => "Ataque",
            2 => "Ataque2",
            3 => "Ataque3",
            _ => "Ataque"
        };

        anim.SetTrigger(trigger);

        yield return new WaitForSeconds(0.35f);

        atacando = false;

        if (comboAtual == 3)
        {
            comboAtual = 0;
            yield return new WaitForSeconds(0.15f);
        }
    }

    void GerenciarCombo()
    {
        if (comboAtual > 0 && Time.time > tempoUltimoAtaque + tempoMaximoCombo)
            comboAtual = 0;
    }

    // ================= ANIMAÇÕES =================
    void AtualizarAnimacoes()
    {
        if (!anim) return;

        float velX = Mathf.Abs(rb.linearVelocity.x);
        float velY = rb.linearVelocity.y;

        anim.SetBool("NoChao", noChao);
        anim.SetFloat("VelocidadeY", velY);

        if (noChao)
        {
            bool movendo = velX > 0.1f;

            anim.SetBool("Andando", movendo);
            anim.SetBool("Correndo", correndo && movendo);
        }
        else
        {
            anim.SetBool("Andando", false);
            anim.SetBool("Correndo", false);
        }

        if (!noChao)
        {
            anim.SetBool("Pulando", velY > 0.1f);
            anim.SetBool("Queda", velY < -0.1f);
        }
        else
        {
            anim.SetBool("Pulando", false);
            anim.SetBool("Queda", false);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
}