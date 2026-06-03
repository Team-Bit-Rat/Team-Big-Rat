using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class MenuPrincipalManager : MonoBehaviour
{
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelMultiplayer;

    [Header("Multiplayer")]
    [SerializeField] private string cenaJogo = "Unified_ColegaTeste_Scenes_CenaDoColega";
    [SerializeField] private float intervaloAtualizacaoLista = 0.35f;

    private Canvas canvasRaiz;
    private DescobridorHostLAN descobridor;
    private RectTransform painelNome;
    private RectTransform painelLista;
    private RectTransform conteudoLista;
    private TMP_InputField campoNome;
    private TextMeshProUGUI tituloPainelNome;
    private TextMeshProUGUI tituloPainelLista;
    private Button botaoConfirmarNome;
    private readonly List<GameObject> linhasLista = new();

    private float proximaAtualizacaoLista;
    private bool listaAtiva;
    private ModoFluxo modoAtual = ModoFluxo.Nenhum;
    private string ipSalaSelecionada = string.Empty;
    private int portaSalaSelecionada = -1;

    private enum ModoFluxo
    {
        Nenhum = 0,
        Host = 1,
        Guest = 2,
    }

    private void Awake()
    {
        GarantirEventSystem();
        canvasRaiz = GetComponentInParent<Canvas>();
        if (canvasRaiz == null) canvasRaiz = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        descobridor = GetComponent<DescobridorHostLAN>();
        if (descobridor == null) descobridor = gameObject.AddComponent<DescobridorHostLAN>();

        LigarBotoesExistentes();
        RenomearBotaoVoltarDoGabiru();
        GarantirPaineisMultiplayer();
        OcultarFluxosMultiplayer();
    }

    private void Update()
    {
        if (!listaAtiva || painelLista == null || !painelLista.gameObject.activeInHierarchy) return;
        if (Time.unscaledTime < proximaAtualizacaoLista) return;

        proximaAtualizacaoLista = Time.unscaledTime + Mathf.Max(0.1f, intervaloAtualizacaoLista);
        ReconstruirListaSalas();
    }

    public void Jogar()
    {
        OcultarFluxosMultiplayer();
        if (painelMenuInicial != null) painelMenuInicial.SetActive(false);
        if (painelMultiplayer != null) painelMultiplayer.SetActive(true);
    }

    public void Fechar()
    {
        bool estavaDentroDoFluxo = PainelEstaAtivo(painelNome) || PainelEstaAtivo(painelLista);
        OcultarFluxosMultiplayer();

        if (estavaDentroDoFluxo)
        {
            if (painelMenuInicial != null) painelMenuInicial.SetActive(false);
            if (painelMultiplayer != null) painelMultiplayer.SetActive(true);
            return;
        }

        if (painelMultiplayer != null) painelMultiplayer.SetActive(false);
        if (painelMenuInicial != null) painelMenuInicial.SetActive(true);
    }

    public void Sair()
    {
        Debug.Log("Sair do Jogo");
        Application.Quit();
    }

    public void IniciarFluxoCriarSala()
    {
        modoAtual = ModoFluxo.Host;
        ipSalaSelecionada = string.Empty;
        portaSalaSelecionada = -1;
        listaAtiva = false;

        if (painelMultiplayer != null) painelMultiplayer.SetActive(false);
        MostrarPainelLista(false);
        MostrarPainelNome(true, "Criar sala", "Criar");
    }

    public void IniciarFluxoEntrarSala()
    {
        modoAtual = ModoFluxo.Guest;
        ipSalaSelecionada = string.Empty;
        portaSalaSelecionada = -1;

        if (painelMultiplayer != null) painelMultiplayer.SetActive(false);
        MostrarPainelNome(false, string.Empty, string.Empty);
        MostrarPainelLista(true);
        ReconstruirListaSalas();
    }

    public void ConfirmarNomeMultiplayer()
    {
        if (modoAtual == ModoFluxo.Host)
        {
            CriarPartida();
            return;
        }

        if (modoAtual == ModoFluxo.Guest)
        {
            if (string.IsNullOrWhiteSpace(ipSalaSelecionada) || portaSalaSelecionada <= 0)
            {
                Debug.LogWarning("[GabiruMenu] Nenhuma sala selecionada.");
                return;
            }

            EntrarNaPartidaSelecionada();
        }
    }

    private void LigarBotoesExistentes()
    {
        LigarBotaoPorNomes(Jogar, "JOGARButton", "JOGAR");
        LigarBotaoPorNomes(IniciarFluxoEntrarSala, "JOINButton", "JOIN");
        LigarBotaoPorNomes(IniciarFluxoCriarSala, "CREATE", "CRIARButton", "CRIAR", "Button");
        LigarBotaoPorNomes(Fechar, "voltar", "BACK");
        LigarBotaoPorNomes(Sair, "SAIRButton", "SAIR");
    }

    private void LigarBotaoPorNomes(UnityEngine.Events.UnityAction acao, params string[] nomes)
    {
        for (int i = 0; i < nomes.Length; i++)
        {
            LigarBotaoPorNome(nomes[i], acao);
        }
    }

    private void LigarBotaoPorNome(string nome, UnityEngine.Events.UnityAction acao)
    {
        Transform t = BuscarTransformPorNome(nome);
        if (t == null) return;

        Button botao = t.GetComponent<Button>();
        if (botao == null) botao = t.gameObject.AddComponent<Button>();

        botao.onClick.RemoveListener(acao);
        botao.onClick.AddListener(acao);
    }

    private void RenomearBotaoVoltarDoGabiru()
    {
        Transform voltar = BuscarTransformPorNome("voltar");
        if (voltar == null) return;

        var textos = voltar.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < textos.Length; i++)
        {
            textos[i].text = "Voltar";
            textos[i].fontSize = Mathf.Max(22f, textos[i].fontSize);
        }
    }

    private void GarantirPaineisMultiplayer()
    {
        if (canvasRaiz == null) return;

        if (painelNome == null)
        {
            painelNome = CriarPainelOverlay("GabiruPainelNome", new Vector2(780f, 360f));
            tituloPainelNome = CriarTexto("TituloNome", painelNome, "Criar sala", 34f, TextAlignmentOptions.Center);
            Posicionar(tituloPainelNome.rectTransform, new Vector2(0f, 118f), new Vector2(680f, 54f));
            campoNome = CriarCampoNome(painelNome);
            botaoConfirmarNome = CriarBotaoTexto("BotaoConfirmarNome", painelNome, "Criar", new Vector2(150f, -112f), new Vector2(220f, 64f));
            botaoConfirmarNome.onClick.AddListener(ConfirmarNomeMultiplayer);
            Button voltarNome = CriarBotaoTexto("BotaoVoltarNome", painelNome, "Voltar", new Vector2(-150f, -112f), new Vector2(220f, 64f));
            voltarNome.onClick.AddListener(Fechar);
        }

        if (painelLista == null)
        {
            painelLista = CriarPainelOverlay("GabiruPainelListaSalas", new Vector2(980f, 640f));
            tituloPainelLista = CriarTexto("TituloLista", painelLista, "Salas encontradas", 34f, TextAlignmentOptions.Center);
            Posicionar(tituloPainelLista.rectTransform, new Vector2(0f, 250f), new Vector2(860f, 54f));

            conteudoLista = CriarContainerLista(painelLista);

            Button voltarLista = CriarBotaoTexto("BotaoVoltarLista", painelLista, "Voltar", new Vector2(-160f, -258f), new Vector2(220f, 60f));
            voltarLista.onClick.AddListener(Fechar);

            Button atualizarLista = CriarBotaoTexto("BotaoAtualizarLista", painelLista, "Atualizar", new Vector2(160f, -258f), new Vector2(220f, 60f));
            atualizarLista.onClick.AddListener(ReconstruirListaSalas);
        }
    }

    private RectTransform CriarPainelOverlay(string nome, Vector2 tamanho)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvasRaiz.transform, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = tamanho;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.02f, 0.025f, 0.035f, 0.94f);
        return rt;
    }

    private TMP_InputField CriarCampoNome(Transform parent)
    {
        var go = new GameObject("CampoNomeJogador", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Posicionar(rt, new Vector2(0f, 10f), new Vector2(620f, 74f));

        var imagem = go.GetComponent<Image>();
        imagem.color = new Color(1f, 1f, 1f, 0.12f);

        var input = go.GetComponent<TMP_InputField>();
        input.characterLimit = 20;

        var areaGo = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        var areaRt = areaGo.GetComponent<RectTransform>();
        areaRt.SetParent(go.transform, false);
        areaRt.anchorMin = Vector2.zero;
        areaRt.anchorMax = Vector2.one;
        areaRt.offsetMin = new Vector2(22f, 12f);
        areaRt.offsetMax = new Vector2(-22f, -12f);

        var placeholder = CriarTexto("Placeholder", areaRt, "Digite seu nome...", 24f, TextAlignmentOptions.Left);
        placeholder.color = new Color(1f, 1f, 1f, 0.42f);
        Esticar(placeholder.rectTransform);

        var texto = CriarTexto("Text", areaRt, string.Empty, 26f, TextAlignmentOptions.Left);
        Esticar(texto.rectTransform);

        input.textViewport = areaRt;
        input.placeholder = placeholder;
        input.textComponent = texto;
        input.text = string.IsNullOrWhiteSpace(DadosSessaoLocal.NomeUsuario) ? string.Empty : DadosSessaoLocal.NomeUsuario;
        return input;
    }

    private RectTransform CriarContainerLista(Transform parent)
    {
        var scrollGo = new GameObject("SalasScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.SetParent(parent, false);
        Posicionar(scrollRt, new Vector2(0f, -8f), new Vector2(840f, 430f));

        var scrollImg = scrollGo.GetComponent<Image>();
        scrollImg.color = new Color(1f, 1f, 1f, 0.08f);

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.SetParent(scrollGo.transform, false);
        Esticar(viewportRt);
        viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.08f);
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;

        var contentGo = new GameObject("ConteudoSalas", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.SetParent(viewportGo.transform, false);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.offsetMin = new Vector2(14f, 0f);
        contentRt.offsetMax = new Vector2(-14f, 0f);

        var layout = contentGo.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 12, 12);
        layout.spacing = 12f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.viewport = viewportRt;
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 18f;

        return contentRt;
    }

    private void MostrarPainelNome(bool visivel, string titulo, string textoConfirmar)
    {
        if (painelNome == null) return;
        painelNome.gameObject.SetActive(visivel);
        if (!visivel) return;

        if (tituloPainelNome != null) tituloPainelNome.text = titulo;
        if (botaoConfirmarNome != null)
        {
            var tmp = botaoConfirmarNome.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = textoConfirmar;
        }

        if (campoNome != null)
        {
            campoNome.text = string.IsNullOrWhiteSpace(DadosSessaoLocal.NomeUsuario) ? string.Empty : DadosSessaoLocal.NomeUsuario;
            campoNome.ActivateInputField();
        }
    }

    private void MostrarPainelLista(bool visivel)
    {
        listaAtiva = visivel;
        if (painelLista != null) painelLista.gameObject.SetActive(visivel);
        if (visivel && tituloPainelLista != null) tituloPainelLista.text = "Salas encontradas";
    }

    private void OcultarFluxosMultiplayer()
    {
        modoAtual = ModoFluxo.Nenhum;
        listaAtiva = false;
        ipSalaSelecionada = string.Empty;
        portaSalaSelecionada = -1;
        if (painelNome != null) painelNome.gameObject.SetActive(false);
        if (painelLista != null) painelLista.gameObject.SetActive(false);
    }

    private void ReconstruirListaSalas()
    {
        if (conteudoLista == null) return;

        for (int i = 0; i < linhasLista.Count; i++)
        {
            if (linhasLista[i] != null) Destroy(linhasLista[i]);
        }
        linhasLista.Clear();

        if (descobridor == null || descobridor.Hosts.Count == 0)
        {
            CriarLinhaMensagem("Nenhuma sala encontrada na rede local ainda...");
            return;
        }

        for (int i = 0; i < descobridor.Hosts.Count; i++)
        {
            CriarLinhaSala(descobridor.Hosts[i]);
        }
    }

    private void CriarLinhaMensagem(string texto)
    {
        var linha = CriarLinhaBase("SemSala", 110f);
        var tmp = CriarTexto("Mensagem", linha.transform, texto, 24f, TextAlignmentOptions.Center);
        Esticar(tmp.rectTransform);
    }

    private void CriarLinhaSala(HostLanInfo host)
    {
        var linha = CriarLinhaBase($"Sala_{host.Id}", 116f);

        string resumo = $"{host.Nome}\n{host.Ip}:{host.Porta}  |  Players: {Mathf.Max(1, host.QuantidadeJogadores)}";
        var texto = CriarTexto("ResumoSala", linha.transform, resumo, 23f, TextAlignmentOptions.Left);
        texto.textWrappingMode = TextWrappingModes.Normal;
        Posicionar(texto.rectTransform, new Vector2(-110f, 0f), new Vector2(560f, 82f));

        string ip = host.Ip;
        int porta = host.Porta;
        Button entrar = CriarBotaoTexto("EntrarSala", linha.transform, "Entrar", new Vector2(300f, 0f), new Vector2(170f, 58f));
        entrar.onClick.AddListener(() => PrepararEntradaNaSala(ip, porta));
    }

    private GameObject CriarLinhaBase(string nome, float altura)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(conteudoLista, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, altura);

        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.1f);

        var layout = go.GetComponent<LayoutElement>();
        layout.preferredHeight = altura;
        layout.flexibleHeight = 0f;

        linhasLista.Add(go);
        return go;
    }

    private void PrepararEntradaNaSala(string ip, int porta)
    {
        if (string.IsNullOrWhiteSpace(ip) || porta <= 0)
        {
            Debug.LogWarning("[GabiruMenu] Sala invalida.");
            return;
        }

        modoAtual = ModoFluxo.Guest;
        ipSalaSelecionada = ip;
        portaSalaSelecionada = porta;
        MostrarPainelLista(false);
        MostrarPainelNome(true, "Entrar na sala", "Entrar");
    }

    private void CriarPartida()
    {
        DadosSessaoLocal.NomeUsuario = ObterNomeValido(host: true);
        ConexaoPendente.CriarHost = true;
        ConexaoPendente.Ativa = false;
        ConexaoPendente.Ip = string.Empty;
        ConexaoPendente.Porta = 7777;
        SceneManager.LoadScene(cenaJogo);
    }

    private void EntrarNaPartidaSelecionada()
    {
        DadosSessaoLocal.NomeUsuario = ObterNomeValido(host: false);
        ConexaoPendente.CriarHost = false;
        ConexaoPendente.Ativa = true;
        ConexaoPendente.Ip = ipSalaSelecionada;
        ConexaoPendente.Porta = portaSalaSelecionada;
        SceneManager.LoadScene(cenaJogo);
    }

    private string ObterNomeValido(bool host)
    {
        string nome = campoNome != null ? campoNome.text : DadosSessaoLocal.NomeUsuario;
        nome = string.IsNullOrWhiteSpace(nome) ? string.Empty : nome.Trim();

        bool nomePadrao = string.Equals(nome, "Jogador", StringComparison.OrdinalIgnoreCase)
            || string.Equals(nome, "Host", StringComparison.OrdinalIgnoreCase);

        if (nome.Length < 2 || nomePadrao)
        {
            int sufixo = Mathf.Abs(Environment.TickCount % 9000) + 1000;
            nome = host ? $"Host-{sufixo}" : $"Jogador-{sufixo}";
        }

        if (nome.Length > 20) nome = nome.Substring(0, 20);
        return nome;
    }

    private Button CriarBotaoTexto(string nome, Transform parent, string texto, Vector2 posicao, Vector2 tamanho)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        Posicionar(rt, posicao, tamanho);

        var imagem = go.GetComponent<Image>();
        imagem.color = new Color(0.86f, 0.18f, 0.14f, 0.92f);

        var botao = go.GetComponent<Button>();
        var cores = botao.colors;
        cores.highlightedColor = new Color(1f, 0.34f, 0.28f, 1f);
        cores.pressedColor = new Color(0.58f, 0.08f, 0.06f, 1f);
        botao.colors = cores;

        var label = CriarTexto("Texto", go.transform, texto, 23f, TextAlignmentOptions.Center);
        Esticar(label.rectTransform);
        return botao;
    }

    private TextMeshProUGUI CriarTexto(string nome, Transform parent, string texto, float tamanhoFonte, TextAlignmentOptions alinhamento)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(300f, 60f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tamanhoFonte;
        tmp.alignment = alinhamento;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    private static void Posicionar(RectTransform rt, Vector2 posicao, Vector2 tamanho)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
    }

    private static void Esticar(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static bool PainelEstaAtivo(RectTransform painel)
    {
        return painel != null && painel.gameObject.activeInHierarchy;
    }

    private static Transform BuscarTransformPorNome(string nome)
    {
        var todos = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < todos.Length; i++)
        {
            if (string.Equals(todos[i].name, nome, StringComparison.OrdinalIgnoreCase))
            {
                return todos[i];
            }
        }

        return null;
    }

    private static void GarantirEventSystem()
    {
        if (EventSystem.current != null) return;

        var existente = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (existente != null)
        {
            if (!existente.gameObject.activeInHierarchy) existente.gameObject.SetActive(true);
            return;
        }

        var go = new GameObject("EventSystem", typeof(EventSystem));

#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }
}
