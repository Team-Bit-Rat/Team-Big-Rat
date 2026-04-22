using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidadeAndando = 5f;
    public float velocidadeCorrendo = 10f;
    public float aceleracao = 15f;
    public float velocidadeAr = 8f;

    [Header("Pulo")]
    public float forcaPulo = 12f;
    public int pulosExtras = 1;
    public float coyoteTime = 0.15f;
    public float bufferPulo = 0.1f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Sprite e Animação")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;

    [Header("Corrida")]
    public KeyCode teclaCorrida = KeyCode.LeftShift;

    // Referência do combo
    private PlayerCombo comboScript;

    // Variáveis privadas
    private Rigidbody2D rb;
    private float movimentoInput;
    private float velocidadeAtual;
    private bool estaNoChao;
    private bool estaCorrendo;
    private int pulosRestantes;
    private float coyoteTimer;
    private float bufferPuloTimer;
    private bool facingRight = true;
    private string animacaoAtual = "";

    // Parâmetros da animação
    private readonly string ANIM_IDLE = "Idle";
    private readonly string ANIM_WALKING = "Walking";
    private readonly string ANIM_RUNNING = "Running";
    private readonly string ANIM_JUMP = "Jump";
    private readonly string ANIM_FALL = "Fall";

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        comboScript = GetComponent<PlayerCombo>();  // Pega a referência do combo

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (groundCheck == null)
        {
            GameObject go = new GameObject("GroundCheck");
            go.transform.parent = transform;
            go.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = go.transform;
        }

        if (rb != null)
        {
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    void Update()
    {
        if (rb == null) return;

        // Input de movimento
        movimentoInput = Input.GetAxisRaw("Horizontal");

        // Input de corrida
        estaCorrendo = Input.GetKey(teclaCorrida) && estaNoChao && Mathf.Abs(movimentoInput) > 0;

        // Input de pulo
        if (Input.GetButtonDown("Jump"))
        {
            bufferPuloTimer = bufferPulo;
        }

        bufferPuloTimer -= Time.deltaTime;
        coyoteTimer -= Time.deltaTime;

        // Ground Check
        bool estavaNoChao = estaNoChao;
        estaNoChao = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Coyote Time
        if (estaNoChao && !estavaNoChao)
        {
            coyoteTimer = coyoteTime;
            pulosRestantes = pulosExtras;
        }

        if (estaNoChao && rb.linearVelocity.y <= 0)
        {
            pulosRestantes = pulosExtras;
        }

        // Pular
        if (bufferPuloTimer > 0 && (coyoteTimer > 0 || pulosRestantes > 0))
        {
            Pular();
            bufferPuloTimer = 0;
        }

        // Pulo curto
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        // Atualiza animações
        AtualizarAnimacoes();
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        Movimentar();
    }

    void Movimentar()
    {
        // TRAVA MOVIMENTO DURANTE ATAQUE
        if (comboScript != null && comboScript.EstaAtacando())
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float velocidadeAlvo = estaCorrendo ? velocidadeCorrendo : velocidadeAndando;
        float targetSpeed = movimentoInput * velocidadeAlvo;
        float acceleration = estaNoChao ? aceleracao : velocidadeAr;

        velocidadeAtual = Mathf.Lerp(velocidadeAtual, targetSpeed, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(velocidadeAtual, rb.linearVelocity.y);

        // FLIP - USANDO SÓ SPRITERENDERER, SEM MEXER NO SCALE!
        if (movimentoInput > 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (movimentoInput < 0)
        {
            spriteRenderer.flipX = false;
        }

        
    }
    void Pular()
    {
        if (!estaNoChao && pulosRestantes <= 0) return;

        if (!estaNoChao)
        {
            pulosRestantes--;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * forcaPulo, ForceMode2D.Impulse);

        if (animator != null)
        {
            animator.Play(ANIM_JUMP, 0, 0);
            animacaoAtual = ANIM_JUMP;
        }
    }

    void AtualizarAnimacoes()
    {
        if (animator == null) return;

        // Se estiver atacando, não troca animação
        if (comboScript != null && comboScript.EstaAtacando()) return;

        float velocidadeHorizontal = Mathf.Abs(rb.linearVelocity.x);
        bool estaNoAr = !estaNoChao;

        if (estaNoAr)
        {
            if (rb.linearVelocity.y > 0.1f)
            {
                PlayAnimationIfChanged(ANIM_JUMP);
            }
            else if (rb.linearVelocity.y < -0.1f)
            {
                PlayAnimationIfChanged(ANIM_FALL);
            }
        }
        else
        {
            if (velocidadeHorizontal < 0.1f)
            {
                PlayAnimationIfChanged(ANIM_IDLE);
            }
            else if (estaCorrendo && velocidadeHorizontal > velocidadeAndando * 0.5f)
            {
                PlayAnimationIfChanged(ANIM_RUNNING);
            }
            else
            {
                PlayAnimationIfChanged(ANIM_WALKING);
            }
        }
    }

    void PlayAnimationIfChanged(string nomeAnimacao)
    {
        if (animacaoAtual != nomeAnimacao)
        {
            animacaoAtual = nomeAnimacao;
            animator.Play(nomeAnimacao);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}