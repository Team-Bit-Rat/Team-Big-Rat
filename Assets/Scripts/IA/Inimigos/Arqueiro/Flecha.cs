using UnityEngine;

/// <summary>
/// Flecha do Arqueiro.
/// IMPORTANTE: o prefab da Flecha precisa ter um Collider2D com isTrigger = true.
/// Este script garante isso no Awake automaticamente.
/// </summary>
public class Flecha : MonoBehaviour
{
    [Header("Flecha")]
    public float velocidade  = 12f;
    public float dano        = 300f;
    public float tempoDeVida = 5f;

    private Vector2     direcao;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        }

        // Garante que o colisor é trigger para OnTriggerEnter2D funcionar
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        Destroy(gameObject, tempoDeVida);
    }

    /// <summary>Chamado pelo ArqueiroAI ao instanciar.</summary>
    public void Inicializar(Vector2 direcaoDisparo)
    {
        direcao = direcaoDisparo.normalized;
        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);
    }

    void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = direcao * velocidade;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Acertou o player — aplica dano e some
        if (other.CompareTag("Player"))
        {
            other.GetComponentInParent<PlayerHealth>()?.ReceberDano(dano);
            Destroy(gameObject);
            return;
        }

        // Acertou chão ou parede — some sem dano
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
            return;
        }

        // Ignora hurtboxes de inimigos e qualquer outro trigger
    }
}
