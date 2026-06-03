using UnityEngine;

[DisallowMultipleComponent]
public sealed class FlutuarSuaveUI : MonoBehaviour
{
    [SerializeField, Min(1f)] float amplitudePixels = 5f;
    [SerializeField, Min(0.05f)] float velocidade = 0.28f;
    [SerializeField] bool usarTempoNaoEscalado = true;
    [SerializeField] bool iniciarComOffsetAleatorio = true;

    RectTransform rect;
    Vector2 posicaoBase;
    bool baseCapturada;
    float offsetFase;

    public void ConfigurarPadrao()
    {
        amplitudePixels = 5f;
        velocidade = 0.28f;
        usarTempoNaoEscalado = true;
    }

    void Awake()
    {
        CapturarBase();
    }

    void OnEnable()
    {
        CapturarBase();
    }

    void Update()
    {
        if (!baseCapturada) CapturarBase();
        if (rect == null) return;

        float tempo = usarTempoNaoEscalado ? Time.unscaledTime : Time.time;
        float y = Mathf.Sin((tempo + offsetFase) * velocidade * Mathf.PI * 2f) * amplitudePixels;
        rect.anchoredPosition = posicaoBase + new Vector2(0f, y);
    }

    void OnDisable()
    {
        if (rect != null && baseCapturada)
        {
            rect.anchoredPosition = posicaoBase;
        }
    }

    void CapturarBase()
    {
        if (baseCapturada) return;
        rect = transform as RectTransform;
        if (rect == null) return;
        posicaoBase = rect.anchoredPosition;
        offsetFase = iniciarComOffsetAleatorio ? Random.Range(0f, 1f) : 0f;
        baseCapturada = true;
    }
}
