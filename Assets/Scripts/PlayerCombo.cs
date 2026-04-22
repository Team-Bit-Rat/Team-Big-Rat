using UnityEngine;

public class PlayerCombo : MonoBehaviour
{
    [Header("Combo")]
    public float tempoMaximoCombo = 0.8f;
    public float tempoResetCombo = 1.5f;
    public float tempoEntreAtaques = 0.4f;

    [Header("Animação")]
    public Animator animator;

    // Nomes das animações
    private readonly string ANIM_ATAQUE1 = "ataque";
    private readonly string ANIM_ATAQUE2 = "ataque2";
    private readonly string ANIM_ATAQUE3 = "Ataque3";

    // Controle do combo
    private int comboAtual = 0;
    private float tempoUltimoAtaque;
    private bool podeAtacar = true;
    private bool atacando = false;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Input de ataque (botão esquerdo do mouse ou J)
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
        {
            Atacar();
        }

        // Reset do combo se passou muito tempo
        if (comboAtual > 0 && Time.time > tempoUltimoAtaque + tempoResetCombo)
        {
            ResetCombo();
        }

        // Libera movimento quando o ataque acabar
        if (atacando && Time.time > tempoUltimoAtaque + tempoEntreAtaques)
        {
            FinalizarAtaque();
        }
    }

    void Atacar()
    {
        if (!podeAtacar) return;

        bool dentroDoTempo = (Time.time - tempoUltimoAtaque) <= tempoMaximoCombo;

        if (comboAtual == 0 || dentroDoTempo)
        {
            comboAtual++;
            if (comboAtual > 3) comboAtual = 3;
            ExecutarAtaque();
        }
        else
        {
            comboAtual = 1;
            ExecutarAtaque();
        }

        tempoUltimoAtaque = Time.time;
    }

    void ExecutarAtaque()
    {
        atacando = true;
        podeAtacar = false;

        switch (comboAtual)
        {
            case 1:
                animator.Play(ANIM_ATAQUE1, 0, 0);
                Debug.Log("💥 Primeiro Ataque!");
                break;
            case 2:
                animator.Play(ANIM_ATAQUE2, 0, 0);
                Debug.Log("💥💥 Segundo Ataque!");
                break;
            case 3:
                animator.Play(ANIM_ATAQUE3, 0, 0);
                Debug.Log("💥💥💥 TERCEIRO ATAQUE!");
                break;
        }

        Invoke(nameof(AplicarDano), 0.2f);
    }

    void AplicarDano()
    {
        Debug.Log($"💢 Dano aplicado! Combo: {comboAtual}");
        // Aqui você coloca a lógica de dano nos inimigos
    }

    void FinalizarAtaque()
    {
        atacando = false;
        podeAtacar = true;

        if (comboAtual >= 3)
        {
            Invoke(nameof(ResetCombo), 0.3f);
        }
    }

    void ResetCombo()
    {
        comboAtual = 0;
        Debug.Log("🔄 Combo resetado!");
    }

    public bool EstaAtacando()
    {
        return atacando;
    }
}