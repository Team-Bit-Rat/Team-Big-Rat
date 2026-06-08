using UnityEngine;

/// <summary>
/// Script da Flecha do Arqueiro
/// Vai em linha reta, causa dano ao acertar o player e some
/// </summary>
public class Flecha : MonoBehaviour
{
    [Header("Flecha")]
    public float velocidade  = 12f;
    public float dano        = 300f;
    public float tempoDeVida = 5f;   // Some sozinha se não acertar nada

    private Vector2 direcao;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Sem gravidade — linha reta
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        }

        // Some automaticamente após tempoDeVida segundos
        Destroy(gameObject, tempoDeVida);
    }

    /// <summary>
    /// Chamado pelo ArqueiroAI ao instanciar a flecha
    /// </summary>
    public void Inicializar(Vector2 direcaoDisparo)
    {
        direcao = direcaoDisparo.normalized;

        // Rotaciona o sprite da flecha pra apontar na direção certa
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
        // Acertou o player
        if (other.CompareTag("Player"))
        {
            //other.GetComponent<PlayerHealth>()?.ReceberDano(dano);
            Destroy(gameObject);
            return;
        }

        // Acertou chão ou parede (layer Ground)
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
