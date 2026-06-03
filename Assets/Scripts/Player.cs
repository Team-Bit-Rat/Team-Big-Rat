using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float velocidadeAndando = 5f;
    [SerializeField] private float velocidadeCorrendo = 10f;
    [SerializeField] private float aceleracao = 10f;

    [Header("Pulo")]
    [SerializeField] private float forcaPulo = 12f;
    [SerializeField] private int pulosExtras = 0;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float velocidadeAr = 8f;
    [SerializeField] private float bufferPulo = 0.1f;
    [SerializeField] private float multiplicadorCortePulo = 0.55f;
    [SerializeField] private float multiplicadorGravidadeQueda = 1.35f;

    [Header("Dash")]
    [SerializeField] private float dashForca = 18f;
    [SerializeField] private float dashDuracao = 0.16f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float dashForcaVertical = 15f;

    [Header("Ground")]
    [SerializeField] private Transform groundCheck;
    [FormerlySerializedAs("groundCheckRadius")]
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Teclas")]
    [SerializeField] private KeyCode teclaCorrida = KeyCode.LeftShift;
    [SerializeField] private KeyCode teclaDash = KeyCode.C;

    [Header("Componentes")]
    [FormerlySerializedAs("spriteRenderer")]
    [SerializeField] private SpriteRenderer sprite;
    [FormerlySerializedAs("animator")]
    [SerializeField] private Animator anim;

    [Header("Animacao")]
    [SerializeField] private float velocidadeAnimacao = 1.65f;
    [SerializeField] private float velocidadeAnimacaoIdle = 0.65f;

    [Header("Colisao / Degraus")]
    [SerializeField] private bool configurarColisaoPlayer = true;
    [SerializeField] private Vector2 tamanhoCapsuleColisor = new(0.72f, 1.05f);
    [SerializeField] private Vector2 offsetCapsuleColisor = new(0f, 0.05f);
    [SerializeField] private bool usarMaterialSemAtrito = true;
    [SerializeField] private float alturaMaximaDegrau = 0.28f;
    [SerializeField] private float distanciaChecarDegrau = 0.16f;

    [Header("Combate (Pedrao + Jhon)")]
    [SerializeField] private float attackDamage = 50f;
    [SerializeField] private float attackStunOnEnemy = 0.2f;
    [SerializeField] private Vector2 hitboxSize = new(0.8f, 0.8f);
    [SerializeField] private float hitboxOffsetX = 0.6f;
    [SerializeField] private float attackWindup = 0.1f;
    [SerializeField] private float attackActiveTime = 0.15f;
    [SerializeField] private LayerMask combatLayerMask = ~0;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private PhysicsMaterial2D materialSemAtritoRuntime;

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
    private float bufferPuloTimer;
    private bool soltouPulo;

    private int comboAtual;
    private float tempoUltimoAtaque;
    private readonly float tempoMaximoCombo = 0.8f;
    private readonly List<Collider2D> combatHits = new(16);
    private readonly HashSet<int> alvosAtingidosNoAtaque = new();
    private static readonly float[] comboDmgMult = { 1f, 1.2f, 1.8f };

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (!sprite) sprite = GetComponent<SpriteRenderer>();
        if (!anim) anim = GetComponent<Animator>();
        ConfigurarColisorDoPlayer();

        // Keep compatibility with legacy serialized fields.
        velocidadeAr = Mathf.Max(0f, velocidadeAr);
        bufferPulo = Mathf.Max(0f, bufferPulo);

        dashForca = Mathf.Max(0f, dashForca);
        dashForcaVertical = Mathf.Max(0f, dashForcaVertical);
        dashDuracao = Mathf.Max(0.02f, dashDuracao);
        dashCooldown = Mathf.Max(0f, dashCooldown);

        attackDamage = Mathf.Max(0f, attackDamage);
        attackStunOnEnemy = Mathf.Max(0f, attackStunOnEnemy);
        attackWindup = Mathf.Max(0f, attackWindup);
        attackActiveTime = Mathf.Max(0.01f, attackActiveTime);
        hitboxSize = new Vector2(Mathf.Max(0.05f, hitboxSize.x), Mathf.Max(0.05f, hitboxSize.y));
        hitboxOffsetX = Mathf.Max(0.05f, hitboxOffsetX);
        velocidadeAnimacao = Mathf.Max(0.1f, velocidadeAnimacao);
        velocidadeAnimacaoIdle = Mathf.Max(0.1f, velocidadeAnimacaoIdle);
        multiplicadorCortePulo = Mathf.Clamp(multiplicadorCortePulo, 0.1f, 1f);
        multiplicadorGravidadeQueda = Mathf.Clamp(multiplicadorGravidadeQueda, 1f, 3f);
        tamanhoCapsuleColisor = new Vector2(
            Mathf.Max(0.1f, tamanhoCapsuleColisor.x),
            Mathf.Max(tamanhoCapsuleColisor.x, tamanhoCapsuleColisor.y));
        alturaMaximaDegrau = Mathf.Clamp(alturaMaximaDegrau, 0.02f, 0.75f);
        distanciaChecarDegrau = Mathf.Clamp(distanciaChecarDegrau, 0.02f, 0.5f);

        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (anim != null)
        {
            anim.speed = Mathf.Max(0.1f, velocidadeAnimacao);
        }
    }

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

    void LerInputs()
    {
        if (dashando) return;

        inputX = Input.GetAxisRaw("Horizontal");
        correndo = Input.GetKey(teclaCorrida) && noChao && Mathf.Abs(inputX) > 0;

        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J)) && !atacando)
            Atacar();

        bool dashPressionado = Input.GetKeyDown(teclaDash) || Input.GetKeyDown(KeyCode.C);
        if (dashPressionado && podeDash && !atacando)
            StartCoroutine(Dash());
    }

    void Movimentar()
    {
        float velocidadeAlvo = correndo ? velocidadeCorrendo : velocidadeAndando;
        float alvo = inputX * velocidadeAlvo;

        velocidadeAtual = Mathf.Lerp(velocidadeAtual, alvo, aceleracao * Time.fixedDeltaTime);

        if (Mathf.Abs(inputX) < 0.01f)
            velocidadeAtual = noChao ? 0f : Mathf.Lerp(velocidadeAtual, 0, aceleracao * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(velocidadeAtual, rb.linearVelocity.y);

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

        if (Mathf.Abs(inputX) > 0.01f)
        {
            AplicarAssistenciaDeDegrau(inputX);
        }
        else if (noChao)
        {
            rb.linearVelocity = new Vector2(0f, Mathf.Abs(rb.linearVelocity.y) < 0.25f ? 0f : rb.linearVelocity.y);
        }
    }

    IEnumerator Dash()
    {
        podeDash = false;
        dashando = true;

        Vector2 direcao = sprite != null && sprite.flipX ? Vector2.right : Vector2.left;

        rb.linearVelocity = Vector2.zero;
        rb.linearVelocity = direcao * dashForca;

        if (anim != null) anim.SetTrigger("Dash");

        yield return new WaitForSeconds(dashDuracao);

        dashando = false;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        yield return new WaitForSeconds(dashCooldown);
        podeDash = true;
    }

    void GerenciarPulo()
    {
        coyoteTimer -= Time.deltaTime;
        bufferPuloTimer -= Time.deltaTime;

        if (Input.GetButtonDown("Jump"))
        {
            bufferPuloTimer = bufferPulo;
        }

        if (bufferPuloTimer > 0f && (coyoteTimer > 0 || pulosRestantes > 0))
        {
            if (!noChao)
            {
                pulosRestantes--;
                if (anim != null) anim.SetBool("Pulando Denovo", true);
            }
            else if (anim != null)
            {
                anim.SetBool("Pulando Denovo", false);
            }

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaPulo);
            bufferPuloTimer = 0f;
            coyoteTimer = 0f;
        }

        if (Input.GetButtonUp("Jump"))
            soltouPulo = true;

        if (soltouPulo && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * multiplicadorCortePulo);
            soltouPulo = false;
        }

        rb.gravityScale = rb.linearVelocity.y < -0.1f
            ? 3f * multiplicadorGravidadeQueda
            : 3f;
    }

    void GerenciarGround()
    {
        bool estavaNoChao = noChao;

        if (groundCheck != null)
        {
            noChao = Physics2D.OverlapCircle(groundCheck.position, groundRadius, MascaraChao());
        }
        else if (bodyCollider != null)
        {
            Bounds b = bodyCollider.bounds;
            Vector2 origem = new Vector2(b.center.x, b.min.y - 0.02f);
            Vector2 tamanho = new Vector2(b.size.x * 0.9f, 0.02f);
            noChao = Physics2D.BoxCast(origem, tamanho, 0f, Vector2.down, groundRadius, MascaraChao()).collider != null;
        }

        if (noChao && !estavaNoChao)
        {
            coyoteTimer = coyoteTime;
            pulosRestantes = pulosExtras;
            if (anim != null) anim.SetBool("Pulando Denovo", false);
        }

        if (noChao && rb.linearVelocity.y <= 0)
            pulosRestantes = pulosExtras;
    }

    void ConfigurarColisorDoPlayer()
    {
        bodyCollider = GetComponent<Collider2D>();
        if (!configurarColisaoPlayer)
        {
            if (bodyCollider == null) bodyCollider = gameObject.AddComponent<BoxCollider2D>();
            return;
        }

        var capsule = GetComponent<CapsuleCollider2D>();
        if (capsule == null) capsule = gameObject.AddComponent<CapsuleCollider2D>();

        capsule.direction = CapsuleDirection2D.Vertical;
        capsule.size = tamanhoCapsuleColisor;
        capsule.offset = offsetCapsuleColisor;
        capsule.isTrigger = false;
        capsule.enabled = true;
        bodyCollider = capsule;

        var colisores = GetComponents<Collider2D>();
        for (int i = 0; i < colisores.Length; i++)
        {
            var c = colisores[i];
            if (c != null && c != bodyCollider && !c.isTrigger) c.enabled = false;
        }

        if (usarMaterialSemAtrito)
        {
            if (materialSemAtritoRuntime == null)
            {
                materialSemAtritoRuntime = new PhysicsMaterial2D("Player_SemAtrito_Runtime")
                {
                    friction = 0f,
                    bounciness = 0f
                };
            }

            bodyCollider.sharedMaterial = materialSemAtritoRuntime;
        }
    }

    void AplicarAssistenciaDeDegrau(float eixoX)
    {
        if (!configurarColisaoPlayer || !noChao || bodyCollider == null || rb == null) return;
        if (Mathf.Abs(rb.linearVelocity.y) > 0.05f) return;

        float dir = Mathf.Sign(eixoX);
        Bounds b = bodyCollider.bounds;
        Vector2 direcao = new Vector2(dir, 0f);
        float skin = 0.02f;
        float alcance = distanciaChecarDegrau + Mathf.Abs(rb.linearVelocity.x) * Time.fixedDeltaTime;
        float xFrente = dir > 0f ? b.max.x + skin : b.min.x - skin;
        float yBaixo = b.min.y + Mathf.Min(alturaMaximaDegrau * 0.45f, b.extents.y * 0.8f);
        float yLivre = b.min.y + alturaMaximaDegrau + 0.06f;

        int mascaraChao = MascaraChao();
        var obstaculoBaixo = Physics2D.Raycast(new Vector2(xFrente, yBaixo), direcao, alcance, mascaraChao);
        if (obstaculoBaixo.collider == null) return;

        var obstaculoLivre = Physics2D.Raycast(new Vector2(xFrente, yLivre), direcao, alcance, mascaraChao);
        if (obstaculoLivre.collider != null) return;

        Vector2 origemTopo = new Vector2(obstaculoBaixo.point.x + dir * 0.05f, b.min.y + alturaMaximaDegrau + 0.12f);
        var topoDegrau = Physics2D.Raycast(origemTopo, Vector2.down, alturaMaximaDegrau + 0.18f, mascaraChao);
        if (topoDegrau.collider == null || topoDegrau.collider == bodyCollider || topoDegrau.normal.y < 0.6f) return;

        float subida = topoDegrau.point.y - b.min.y + 0.015f;
        if (subida <= 0.001f || subida > alturaMaximaDegrau) return;

        rb.position += Vector2.up * subida;
    }

    int MascaraChao()
    {
        return groundLayer.value != 0 ? groundLayer.value : Physics2D.DefaultRaycastLayers;
    }

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
        alvosAtingidosNoAtaque.Clear();

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        string trigger = comboAtual switch
        {
            1 => "Ataque",
            2 => "Ataque2",
            3 => "Ataque3",
            _ => "Ataque"
        };

        if (anim != null) anim.SetTrigger(trigger);

        yield return new WaitForSeconds(Mathf.Max(0f, attackWindup));

        float ataqueAtivoAte = Time.time + Mathf.Max(0.01f, attackActiveTime);
        while (Time.time <= ataqueAtivoAte)
        {
            AplicarHitboxDeCombate();
            yield return null;
        }

        float recuperacao = Mathf.Max(0.05f, 0.35f - attackWindup - attackActiveTime);
        yield return new WaitForSeconds(recuperacao);

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

    void AplicarHitboxDeCombate()
    {
        float dir = sprite != null && sprite.flipX ? 1f : -1f;
        Vector2 centro = (Vector2)transform.position + new Vector2(hitboxOffsetX * dir, 0f);

        var filtro = new ContactFilter2D();
        filtro.useLayerMask = true;
        filtro.layerMask = combatLayerMask;
        filtro.useTriggers = true;

        combatHits.Clear();
        int hits = Physics2D.OverlapBox(centro, hitboxSize, 0f, filtro, combatHits);
        if (hits <= 0) return;

        float dano = attackDamage * comboDmgMult[Mathf.Clamp(comboAtual - 1, 0, comboDmgMult.Length - 1)];
        for (int i = 0; i < hits; i++)
        {
            var col = combatHits[i];
            if (col == null) continue;

            var alvo = col.GetComponentInParent<Entity>();
            if (alvo == null || alvo.gameObject == gameObject) continue;

            int id = alvo.GetInstanceID();
            if (!alvosAtingidosNoAtaque.Add(id)) continue;
            alvo.ReceiveDmg(dano, attackStunOnEnemy);
        }
    }

    void AtualizarAnimacoes()
    {
        if (!anim) return;

        float velX = Mathf.Abs(rb.linearVelocity.x);
        float velY = rb.linearVelocity.y;
        bool paradoNoChao = noChao && velX <= 0.1f && !atacando && !dashando;
        anim.speed = paradoNoChao ? velocidadeAnimacaoIdle : velocidadeAnimacao;

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

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        float dir = sprite != null && sprite.flipX ? 1f : -1f;
        Vector2 centro = (Vector2)transform.position + new Vector2(hitboxOffsetX * dir, 0f);
        Gizmos.DrawWireCube(centro, hitboxSize);
    }
}
