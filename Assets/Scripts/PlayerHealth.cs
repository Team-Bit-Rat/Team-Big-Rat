using System.Collections;
using UnityEngine;

/// <summary>
/// Saúde do player — coloque no mesmo GameObject que Player.cs.
/// Configure os nomes dos triggers no Inspector para bater com seu Animator.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] public float vidaMaxima = 500f;
    private float vidaAtual;

    [Header("Animação — nomes exatos dos parâmetros no Animator")]
    [SerializeField] private string triggerHurt  = "Hit";
    [SerializeField] private string triggerMorte = "Death";

    [Header("I-frames pós-hit (evita spam de dano)")]
    [SerializeField] private float tempoInvencivel = 0.5f;
    private float timerInvencivel = 0f;

    // Referências no mesmo GameObject
    private Animator anim;
    private Rigidbody2D rb;
    private Player playerScript;   // ← corrigido: era Movimentacao

    private bool estaMorto = false;

    public float VidaAtual  => vidaAtual;
    public float VidaMaxima => vidaMaxima;
    public bool  EstaMorto  => estaMorto;

    void Awake()
    {
        vidaAtual    = vidaMaxima;
        anim         = GetComponent<Animator>();
        rb           = GetComponent<Rigidbody2D>();
        playerScript = GetComponent<Player>();

        if (playerScript == null)
            Debug.LogWarning("[PlayerHealth] Player.cs não encontrado no mesmo GameObject!");
    }

    void Update()
    {
        if (timerInvencivel > 0f) timerInvencivel -= Time.deltaTime;
    }

    public void ReceberDano(float dano)
    {
        if (estaMorto || timerInvencivel > 0f) return;

        vidaAtual       = Mathf.Max(0f, vidaAtual - dano);
        timerInvencivel = tempoInvencivel;

        Debug.Log($"[Player] -{dano} HP  |  {vidaAtual:0}/{vidaMaxima:0}");

        // Dispara animação de hurt
        if (anim != null && !string.IsNullOrEmpty(triggerHurt))
            anim.SetTrigger(triggerHurt);

        if (vidaAtual <= 0f)
            StartCoroutine(Morrer());
    }

    public void Curar(float quantidade)
    {
        if (estaMorto) return;
        vidaAtual = Mathf.Min(vidaMaxima, vidaAtual + quantidade);
    }

    private IEnumerator Morrer()
    {
        estaMorto = true;
        Debug.Log("[Player] Morreu!");

        // Para física imediatamente
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType       = RigidbodyType2D.Kinematic;
        }

        // Desativa inputs e movimento (Player.cs)
        if (playerScript != null)
            playerScript.enabled = false;

        // Animação de morte
        if (anim != null && !string.IsNullOrEmpty(triggerMorte))
            anim.SetTrigger(triggerMorte);

        yield return new WaitForSeconds(2f);

        // ── PLACEHOLDER GAME OVER ──────────────────────────────────────────
        // Troque pelo seu sistema de game over / respawn:
        // UnityEngine.SceneManagement.SceneManager.LoadScene(
        //     UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        Debug.Log("[Player] Game Over — implemente o respawn/tela aqui.");
    }
}
