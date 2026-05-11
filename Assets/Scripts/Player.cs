using UnityEngine;
using System.Collections;

public class Player : Entity
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

    [Header("Combat")]
    [SerializeField] private float attackDamage = 50f;
    [SerializeField] private float attackStunOnEnemy = 0.2f;
    // Combo damage multipliers: hit 1 = x1, hit 2 = x1.2, hit 3 = x1.8
    private static readonly float[] comboDmgMult = { 1f, 1.2f, 1.8f };

    private Rigidbody2D rb;

    // ================= STATES =================
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
    // Hitbox created fully in code — no child GO needed in the editor
    private CombatHitbox hitbox;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        if (!sprite) sprite = GetComponent<SpriteRenderer>();
        if (!anim)   anim   = GetComponent<Animator>();
        rb.gravityScale = 3f;

        // Build hitbox child at runtime
        GameObject hbGO = new GameObject("PlayerHitbox");
        hbGO.transform.SetParent(transform);
        hbGO.transform.localPosition = new Vector3(0.6f, 0f, 0f); // offset to the right by default
        hbGO.layer = gameObject.layer;

        BoxCollider2D box = hbGO.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.8f, 0.8f);

        hitbox = hbGO.AddComponent<CombatHitbox>();
        hitbox.Owner = this;
        hitbox.Damage = attackDamage;
        hitbox.StunDuration = attackStunOnEnemy;

        hbGO.SetActive(false); // starts inactive
    }

    // ================= UPDATE =================
    void Update()
    {
        if (isDead) return;

        LerInputs();
        GerenciarGround();
        GerenciarPulo();
        GerenciarCombo();
        AtualizarAnimacoes();
    }

    void FixedUpdate()
    {
        if (isDead || dashando || atacando || isStunned) return;
        Movimentar();
    }

    // ================= INPUT =================
    void LerInputs()
    {
        if (dashando || isStunned) return;

        inputX = Input.GetAxisRaw("Horizontal");
        correndo = Input.GetKey(teclaCorrida) && noChao && Mathf.Abs(inputX) > 0;

        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J)) && !atacando && !isStunned)
            Attack();

        if (Input.GetKeyDown(teclaDash) && podeDash && !atacando && !isStunned)
            StartCoroutine(Dash());
    }

    // ================= ENTITY OVERRIDES =================
    /// <summary>Player's attack — uses combo system + hitbox activation.</summary>
    public override void Attack()
    {
        bool dentroDoTempo = (Time.time - tempoUltimoAtaque) <= tempoMaximoCombo;

        comboAtual = (comboAtual == 0 || dentroDoTempo)
            ? Mathf.Min(comboAtual + 1, 3)
            : 1;

        tempoUltimoAtaque = Time.time;

        // Update hitbox damage for this combo hit
        if (hitbox)
            hitbox.Damage = attackDamage * comboDmgMult[comboAtual - 1];

        StartCoroutine(ExecutarAtaque());
    }

    protected override void OnDamageReceived(float amount)
    {
        if (anim) anim.SetTrigger("Hit");
        // Interrupt dash / attack on hit
        if (dashando) StopCoroutine(Dash());
        dashando = false;
        if (atacando) { atacando = false; DeactivateHitbox(); }
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    protected override void OnDeath()
    {
        if (anim) anim.SetTrigger("Die");
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        DeactivateHitbox();
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

        if (!atacando)
        {
            if (inputX > 0 && !viradoDireita)      { viradoDireita = true;  sprite.flipX = true;  }
            else if (inputX < 0 && viradoDireita)  { viradoDireita = false; sprite.flipX = false; }
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
        if (isStunned) return;
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
            anim.SetTrigger("Land");
        }

        if (noChao && rb.linearVelocity.y <= 0)
            pulosRestantes = pulosExtras;
    }

    // ================= ATAQUE =================
    IEnumerator ExecutarAtaque()
    {
        atacando = true;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        string trigger = comboAtual switch
        {
            1 => "Ataque",
            2 => "Ataque2",
            3 => "Ataque3",
            _ => "Ataque"
        };

        anim.SetTrigger(trigger);

        // Windup — hitbox inactive
        yield return new WaitForSeconds(0.1f);

        // Active hitbox window
        ActivateHitbox();
        yield return new WaitForSeconds(0.15f);
        DeactivateHitbox();

        yield return new WaitForSeconds(0.1f);

        atacando = false;

        if (comboAtual == 3)
        {
            comboAtual = 0;
            yield return new WaitForSeconds(0.15f);
        }
    }

    private void ActivateHitbox()
    {
        if (!hitbox) return;
        // Position hitbox in front of player based on facing direction
        float dir = viradoDireita ? 1f : -1f;
        hitbox.transform.localPosition = new Vector3(0.6f * dir, 0f, 0f);
        hitbox.Damage = attackDamage * comboDmgMult[comboAtual - 1];
        hitbox.gameObject.SetActive(true);
        Debug.Log($"[HITBOX] Activated | dir={dir} | dmg={hitbox.Damage}");
    }
    private void DeactivateHitbox()
    {
        if (hitbox) hitbox.gameObject.SetActive(false);
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
            anim.SetBool("Queda",   velY < -0.1f);
        }
        else
        {
            anim.SetBool("Pulando", false);
            anim.SetBool("Queda",   false);
        }
    }

    // On-screen HP bar (visible in Game view during Play mode)
    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        float pct = maxHP > 0 ? currentHP / maxHP : 0f;
        int w = 200, h = 20, pad = 10;
        // Border
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(pad - 1, pad - 1, w + 2, h + 2), Texture2D.whiteTexture);
        // Background
        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(pad, pad, w, h), Texture2D.whiteTexture);
        // Fill
        GUI.color = Color.green;
        GUI.DrawTexture(new Rect(pad, pad, w * pct, h), Texture2D.whiteTexture);
        // Label
        GUI.color = Color.white;
        GUI.Label(new Rect(pad, pad, w, h), $" HP: {currentHP:0} / {maxHP:0}");
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