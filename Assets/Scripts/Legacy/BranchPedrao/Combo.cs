using UnityEngine;

public class Combo : MonoBehaviour
{
    [SerializeField] private float tempoMaximoCombo = 1f;
    [SerializeField] private float tempoResetCombo = 1.5f;
    [SerializeField] private float tempoEntreAtaques = 0.4f;
    [SerializeField] private Animator animator;

    private int comboAtual;
    private float ultimoAtaqueTempo;

    public int ComboAtual => comboAtual;

    public bool PodeAtacar()
    {
        return Time.time >= (ultimoAtaqueTempo + tempoEntreAtaques);
    }

    public int RegistrarAtaque()
    {
        if (!PodeAtacar())
        {
            return comboAtual;
        }

        if (Time.time - ultimoAtaqueTempo > tempoResetCombo)
        {
            comboAtual = 0;
        }

        comboAtual = (comboAtual % 3) + 1;
        ultimoAtaqueTempo = Time.time;

        if (animator != null)
        {
            animator.SetTrigger($"Ataque{comboAtual}");
        }

        return comboAtual;
    }

    private void Update()
    {
        if (comboAtual > 0 && Time.time - ultimoAtaqueTempo > tempoMaximoCombo)
        {
            comboAtual = 0;
        }
    }
}
