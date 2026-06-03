using UnityEngine;

[DisallowMultipleComponent]
public sealed class PulsarUI : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float escalaMin = 0.96f;
    [SerializeField, Min(0.01f)] float escalaMax = 1.04f;
    [SerializeField, Min(0.05f)] float velocidade = 1.6f;
    [SerializeField] bool usarTempoNaoEscalado = true;

    Vector3 escalaBase;
    bool escalaCapturada;

    void Awake()
    {
        CapturarEscalaBaseSeNecessario();
    }

    void OnEnable()
    {
        CapturarEscalaBaseSeNecessario();
    }

    void Update()
    {
        if (!escalaCapturada) CapturarEscalaBaseSeNecessario();

        float tempo = usarTempoNaoEscalado ? Time.unscaledTime : Time.time;
        float oscilacao = (Mathf.Sin(tempo * velocidade * Mathf.PI * 2f) + 1f) * 0.5f;
        float escala = Mathf.Lerp(escalaMin, escalaMax, oscilacao);
        transform.localScale = escalaBase * escala;
    }

    void OnDisable()
    {
        if (escalaCapturada)
        {
            transform.localScale = escalaBase;
        }
    }

    void OnValidate()
    {
        if (escalaMax < escalaMin)
        {
            escalaMax = escalaMin;
        }
    }

    void CapturarEscalaBaseSeNecessario()
    {
        if (escalaCapturada) return;
        escalaBase = transform.localScale;
        escalaCapturada = true;
    }
}
