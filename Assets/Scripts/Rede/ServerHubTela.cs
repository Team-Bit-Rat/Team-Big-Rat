using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class ServerHubTela : MonoBehaviour
{
    [Header("Cenas")]
    [SerializeField] string cenaJogo = "CenaDoCT";

    [Header("Hierarquia opcional (se ja existir na cena)")]
    [SerializeField, HideInInspector] Canvas canvasRaiz;
    [SerializeField, HideInInspector] GameObject mainJoinCanvas;
    [SerializeField, HideInInspector] GameObject serversHubCanvas;
    [SerializeField, HideInInspector] Button botaoHost;
    [SerializeField, HideInInspector] Button botaoGuest;
    [SerializeField, HideInInspector] Button botaoVoltar;
    [SerializeField, HideInInspector] Button botaoConfirmarNome;
    [SerializeField] Image imagemBotaoHost;
    [SerializeField] Image imagemBotaoGuest;
    [SerializeField] Image imagemTitulo;
    [SerializeField, HideInInspector] RectTransform containerSalas;
    [SerializeField, HideInInspector] RectTransform painelNome;
    [SerializeField, HideInInspector] RectTransform raizListaSalas;
    [SerializeField, HideInInspector] TMP_InputField campoNome;
    [SerializeField] bool animarTituloFlutuando = true;

    [Header("Sprites de Interface Multiplayer")]
    [SerializeField, HideInInspector] Sprite sprTitle;
    [SerializeField, HideInInspector] Sprite sprServerHub;
    [SerializeField, HideInInspector] Sprite sprCriarSala;
    [SerializeField, HideInInspector] Sprite sprEntrarSala;
    [SerializeField, HideInInspector] Sprite sprPlayers;
    [SerializeField, HideInInspector] Sprite sprOnButton;
    [SerializeField, HideInInspector] Sprite sprOffButton;
    [SerializeField, HideInInspector] Sprite sprEasyDiff;

    DescobridorHostLAN descobridor;
    readonly List<GameObject> linhasSalas = new();
    float proximaAtualizacaoLista;
    bool listaSalasAtiva;
    ModoFluxo modoFluxoAtual = ModoFluxo.Nenhum;
    string ipSalaSelecionada = string.Empty;
    int portaSalaSelecionada = -1;

    enum ModoFluxo
    {
        Nenhum = 0,
        Host = 1,
        Guest = 2,
    }

    void Awake()
    {
        if (!EhCenaHub(SceneManager.GetActiveScene().name))
        {
            return;
        }

        if (NetworkManager.Singleton != null)
        {
            RedeBootstrap.GarantirBase(NetworkManager.Singleton);
            DontDestroyOnLoad(NetworkManager.Singleton.gameObject);
        }

        descobridor = GetComponent<DescobridorHostLAN>();
        if (descobridor == null)
        {
            descobridor = gameObject.AddComponent<DescobridorHostLAN>();
        }

        GarantirEventSystem();
        CarregarSpritesAutomaticamenteSePreciso();
        GarantirUI();
        LigarEventosBotoes();
        MostrarTelaInicial();
    }

    void Update()
    {
        if (serversHubCanvas == null || !serversHubCanvas.activeInHierarchy) return;
        if (!listaSalasAtiva) return;
        if (Time.unscaledTime < proximaAtualizacaoLista) return;
        proximaAtualizacaoLista = Time.unscaledTime + 0.35f;
        ReconstruirListaSalas();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void GarantirHubNaCena()
    {
        if (!EhCenaHub(SceneManager.GetActiveScene().name)) return;
        if (FindFirstObjectByType<ServerHubTela>() != null) return;

        var go = new GameObject("ServerHub_Controller");
        go.AddComponent<ServerHubTela>();
        Debug.Log("[Hub] Controller criado automaticamente na cena ServerHub.");
    }

    static bool EhCenaHub(string nomeCena)
    {
        return string.Equals(nomeCena, "ServerHub", StringComparison.OrdinalIgnoreCase)
            || string.Equals(nomeCena, "Server Hub", StringComparison.OrdinalIgnoreCase);
    }

    void LigarEventosBotoes()
    {
        if (botaoHost != null)
        {
            botaoHost.onClick.RemoveListener(IniciarFluxoHost);
            botaoHost.onClick.AddListener(IniciarFluxoHost);
        }

        if (botaoGuest != null)
        {
            botaoGuest.onClick.RemoveListener(IniciarFluxoGuest);
            botaoGuest.onClick.AddListener(IniciarFluxoGuest);
        }

        if (botaoVoltar != null)
        {
            botaoVoltar.onClick.RemoveListener(MostrarTelaInicial);
            botaoVoltar.onClick.AddListener(MostrarTelaInicial);
        }

        if (botaoConfirmarNome != null)
        {
            botaoConfirmarNome.onClick.RemoveListener(ConfirmarFluxoComNome);
            botaoConfirmarNome.onClick.AddListener(ConfirmarFluxoComNome);
        }
    }

    void MostrarTelaInicial()
    {
        modoFluxoAtual = ModoFluxo.Nenhum;
        listaSalasAtiva = false;
        LimparSalaSelecionada();
        if (mainJoinCanvas != null) mainJoinCanvas.SetActive(true);
        if (serversHubCanvas != null) serversHubCanvas.SetActive(false);
        if (botaoVoltar != null) botaoVoltar.gameObject.SetActive(false);
        MostrarPainelNome(false, string.Empty);
        if (raizListaSalas != null) raizListaSalas.gameObject.SetActive(false);
        OcultarTodosInputsDoHub();
    }

    void IniciarFluxoHost()
    {
        modoFluxoAtual = ModoFluxo.Host;
        LimparSalaSelecionada();
        if (mainJoinCanvas != null) mainJoinCanvas.SetActive(false);
        if (serversHubCanvas != null) serversHubCanvas.SetActive(false);
        if (botaoVoltar != null) botaoVoltar.gameObject.SetActive(true);
        MostrarPainelNome(true, "CRIAR SALA");
        MostrarListaSalas(false);
    }

    void IniciarFluxoGuest()
    {
        modoFluxoAtual = ModoFluxo.Guest;
        LimparSalaSelecionada();
        if (mainJoinCanvas != null) mainJoinCanvas.SetActive(false);
        if (serversHubCanvas != null) serversHubCanvas.SetActive(true);
        if (botaoVoltar != null) botaoVoltar.gameObject.SetActive(true);
        MostrarPainelNome(false, string.Empty);
        MostrarListaSalas(true);
        ReconstruirListaSalas();
    }

    void ConfirmarFluxoComNome()
    {
        if (modoFluxoAtual == ModoFluxo.Host)
        {
            CriarPartida();
            return;
        }

        if (modoFluxoAtual == ModoFluxo.Guest)
        {
            if (string.IsNullOrWhiteSpace(ipSalaSelecionada) || portaSalaSelecionada <= 0)
            {
                Debug.LogWarning("[Hub] Selecione uma sala antes de confirmar entrada.");
                return;
            }

            Conectar(ipSalaSelecionada, portaSalaSelecionada);
        }
    }

    void MostrarListaSalas(bool visivel)
    {
        listaSalasAtiva = visivel;
        if (raizListaSalas != null) raizListaSalas.gameObject.SetActive(visivel);
        if (!visivel) return;
        ConfigurarVisualListaSalas();
        proximaAtualizacaoLista = Time.unscaledTime + 0.35f;
    }

    void LimparSalaSelecionada()
    {
        ipSalaSelecionada = string.Empty;
        portaSalaSelecionada = -1;
    }

    void PrepararEntradaNaSala(string ip, int porta)
    {
        if (string.IsNullOrWhiteSpace(ip) || porta <= 0)
        {
            Debug.LogWarning("[Hub] Sala selecionada invalida.");
            return;
        }

        modoFluxoAtual = ModoFluxo.Guest;
        ipSalaSelecionada = ip;
        portaSalaSelecionada = porta;

        if (mainJoinCanvas != null) mainJoinCanvas.SetActive(false);
        if (serversHubCanvas != null) serversHubCanvas.SetActive(false);
        if (botaoVoltar != null) botaoVoltar.gameObject.SetActive(true);
        MostrarListaSalas(false);
        MostrarPainelNome(true, "ENTRAR");
    }

    void CriarPartida()
    {
        string nomeFinal = ObterNomeValido(host: true);
        DadosSessaoLocal.NomeUsuario = nomeFinal;

        ConexaoPendente.CriarHost = true;
        ConexaoPendente.Ativa = false;
        SceneManager.LoadScene(cenaJogo);
    }

    string ObterNomeValido(bool host)
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

    void MostrarPainelNome(bool visivel, string textoBotao)
    {
        OcultarTodosInputsDoHub();
        if (painelNome != null) painelNome.gameObject.SetActive(visivel);
        if (campoNome != null) campoNome.gameObject.SetActive(visivel);
        if (botaoConfirmarNome != null) botaoConfirmarNome.gameObject.SetActive(visivel);

        if (visivel)
        {
            ConfigurarVisualPainelNome();
        }

        if (botaoConfirmarNome != null)
        {
            Transform txt = BuscarTransformFilhoPorNome(botaoConfirmarNome.transform, "TXT_CONFIRMAR");
            if (txt != null && txt.TryGetComponent<TextMeshProUGUI>(out var tmp))
            {
                tmp.text = textoBotao;
                tmp.fontSize = 15f;
                tmp.alignment = TextAlignmentOptions.Right;
            }
        }
    }

    void ConfigurarVisualPainelNome()
    {
        bool fluxoHost = modoFluxoAtual == ModoFluxo.Host;

        float painelCentroY = -176f;
        float painelLargura = 1240f;
        float painelAltura = fluxoHost ? 232f : 232f;

        float inputPosY = fluxoHost ? 74f : -12f;
        float inputLargura = fluxoHost ? 860f : 900f;
        float inputAltura = 68f;

        float botaoPosX = fluxoHost ? 500f : 250f;
        float botaoPosY = fluxoHost ? 74f : -12f;
        float botaoTamanho = fluxoHost ? 34f : 28f;

        if (painelNome != null)
        {
            painelNome.anchorMin = new Vector2(0.5f, 0.5f);
            painelNome.anchorMax = new Vector2(0.5f, 0.5f);
            painelNome.pivot = new Vector2(0.5f, 0.5f);
            painelNome.anchoredPosition = new Vector2(0f, painelCentroY);
            painelNome.sizeDelta = new Vector2(painelLargura, painelAltura);
        }

        if (campoNome != null)
        {
            var inputRt = campoNome.GetComponent<RectTransform>();
            if (inputRt != null)
            {
                inputRt.anchorMin = new Vector2(0.5f, 0.5f);
                inputRt.anchorMax = new Vector2(0.5f, 0.5f);
                inputRt.pivot = new Vector2(0.5f, 0.5f);
                inputRt.anchoredPosition = new Vector2(0f, inputPosY);
                inputRt.sizeDelta = new Vector2(inputLargura, inputAltura);
            }

            if (campoNome.textComponent != null)
            {
                campoNome.textComponent.fontSize = 21f;
                campoNome.textComponent.alignment = TextAlignmentOptions.Left;
            }

            if (campoNome.placeholder is TextMeshProUGUI placeholder)
            {
                placeholder.fontSize = 19f;
                placeholder.alignment = TextAlignmentOptions.Left;
            }
        }

        if (botaoConfirmarNome != null)
        {
            var rt = botaoConfirmarNome.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(botaoPosX, botaoPosY);
                rt.sizeDelta = new Vector2(botaoTamanho, botaoTamanho);
            }

            var txt = BuscarTransformFilhoPorNome(botaoConfirmarNome.transform, "TXT_CONFIRMAR");
            if (txt != null && txt.TryGetComponent<TextMeshProUGUI>(out var tmp))
            {
                var txtRt = tmp.GetComponent<RectTransform>();
                txtRt.anchorMin = new Vector2(0.5f, 0.5f);
                txtRt.anchorMax = new Vector2(0.5f, 0.5f);
                txtRt.pivot = new Vector2(1f, 0.5f);
                txtRt.anchoredPosition = new Vector2(-74f, 0f);
                txtRt.sizeDelta = new Vector2(240f, 44f);
                tmp.fontSize = 15f;
                tmp.alignment = TextAlignmentOptions.Right;
            }
        }
    }

    void OcultarTodosInputsDoHub()
    {
        if (serversHubCanvas == null) return;

        TMP_InputField[] inputs = serversHubCanvas.GetComponentsInChildren<TMP_InputField>(true);
        for (int i = 0; i < inputs.Length; i++)
        {
            if (inputs[i] == null) continue;
            inputs[i].gameObject.SetActive(false);
        }
    }

    void ReconstruirListaSalas()
    {
        if (containerSalas == null) return;

        for (int i = 0; i < linhasSalas.Count; i++)
        {
            if (linhasSalas[i] != null) Destroy(linhasSalas[i]);
        }
        linhasSalas.Clear();

        if (descobridor == null || descobridor.Hosts.Count == 0)
        {
            CriarLinhaSemSala();
            return;
        }

        for (int i = 0; i < descobridor.Hosts.Count; i++)
        {
            CriarLinhaSala(descobridor.Hosts[i]);
        }
    }

    void CriarLinhaSemSala()
    {
        CriarEspacadorLista("SemSalaSpacer", 360f);
        GameObject linha = CriarLinhaBase("SalaVazia");
        if (linha.TryGetComponent<LayoutElement>(out var layout))
        {
            layout.preferredHeight = 320f;
        }

        CriarTexto(
            "SemSalas",
            linha.transform,
            "Nenhuma sala encontrada na rede local ainda...",
            new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(0f, -24f),
            new Vector2(260f, 132f),
            34f,
            TextAlignmentOptions.Center);
    }

    void CriarEspacadorLista(string nome, float altura)
    {
        if (containerSalas == null) return;

        var go = new GameObject(nome, typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(containerSalas, false);

        var layout = go.GetComponent<LayoutElement>();
        layout.preferredHeight = Mathf.Max(0f, altura);
        layout.flexibleHeight = 0f;

        linhasSalas.Add(go);
    }

    void CriarLinhaSala(HostLanInfo host)
    {
        GameObject linha = CriarLinhaBase($"Sala_{host.Id}");

        var iconePlayers = CriarImagem(
            "PlayersIcon",
            linha.transform,
            sprPlayers,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(88f, 0f),
            new Vector2(98f, 30f));

        if (iconePlayers != null)
        {
            CriarTexto(
                "PlayersCount",
                iconePlayers.transform,
                Mathf.Max(1, host.QuantidadeJogadores).ToString(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                24f,
                TextAlignmentOptions.Center);
        }

        CriarTexto(
            "NomeSala",
            linha.transform,
            host.Nome,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(220f, 16f),
            new Vector2(460f, 40f),
            31f,
            TextAlignmentOptions.Left);

        CriarTexto(
            "EnderecoSala",
            linha.transform,
            $"{host.Ip}:{host.Porta}",
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(220f, -18f),
            new Vector2(460f, 30f),
            22f,
            TextAlignmentOptions.Left);

        CriarImagem(
            "DifficultyEasy",
            linha.transform,
            sprEasyDiff,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-240f, 0f),
            new Vector2(165f, 70f));

        string ip = host.Ip;
        int porta = host.Porta;
        var btnConectar = CriarBotaoComImagem(
            "BtnConectar",
            linha.transform,
            sprOnButton,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-92f, 0f),
            new Vector2(72f, 72f));

        if (btnConectar != null)
        {
            var rt = btnConectar.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(78f, 78f);
            btnConectar.onClick.AddListener(() => PrepararEntradaNaSala(ip, porta));
        }
    }

    GameObject CriarLinhaBase(string nome)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(containerSalas, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 120f);

        var layout = go.GetComponent<LayoutElement>();
        layout.preferredHeight = 120f;
        layout.flexibleHeight = 0f;

        var img = go.GetComponent<Image>();
        if (sprServerHub != null)
        {
            img.sprite = sprServerHub;
            img.type = Image.Type.Simple;
            img.color = new Color(1f, 1f, 1f, 0.12f);
        }
        else
        {
            img.color = new Color(0f, 0f, 0f, 0.4f);
        }

        linhasSalas.Add(go);
        return go;
    }

    void Conectar(string ip, int porta)
    {
        string nomeFinal = ObterNomeValido(host: false);
        DadosSessaoLocal.NomeUsuario = nomeFinal;

        if (NetworkManager.Singleton == null)
        {
            ConexaoPendente.Ativa = true;
            ConexaoPendente.CriarHost = false;
            ConexaoPendente.Ip = ip;
            ConexaoPendente.Porta = porta;
            Debug.Log("[Hub] Sem NetworkManager nessa cena. Indo para CenaDoCT e conectando automaticamente...");
            SceneManager.LoadScene(cenaJogo);
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[Hub] Ja existe rede ativa.");
            return;
        }

        RedeBootstrap.GarantirBase(NetworkManager.Singleton);
        RedeBootstrap.ConfigurarDestino(NetworkManager.Singleton, ip, porta);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(nomeFinal);

        bool ok = NetworkManager.Singleton.StartClient();
        Debug.Log(ok ? $"[Hub] Conectando em {ip}:{porta}" : "[Hub] Falha ao iniciar client.");
    }

    void GarantirUI()
    {
        canvasRaiz = GarantirCanvasRaiz();
        if (canvasRaiz == null) return;

        if (mainJoinCanvas == null)
        {
            var mainJoinT = BuscarTransformPorNome("Main_Join");
            if (mainJoinT != null) mainJoinCanvas = mainJoinT.gameObject;
        }
        if (mainJoinCanvas == null)
        {
            mainJoinCanvas = CriarMainJoinCanvas(canvasRaiz.transform);
        }

        if (serversHubCanvas == null)
        {
            var serversHubT = BuscarTransformPorNome("SERVERS_HUB");
            if (serversHubT != null) serversHubCanvas = serversHubT.gameObject;
        }
        if (serversHubCanvas == null)
        {
            serversHubCanvas = CriarServersHubCanvas(canvasRaiz.transform);
        }

        if (imagemBotaoHost == null && mainJoinCanvas != null)
        {
            Transform hostT = BuscarTransformFilhoPorNome(mainJoinCanvas.transform, "Host");
            if (hostT != null) imagemBotaoHost = hostT.GetComponent<Image>();
        }

        if (imagemBotaoGuest == null && mainJoinCanvas != null)
        {
            Transform guestT = BuscarTransformFilhoPorNome(mainJoinCanvas.transform, "Guest");
            if (guestT != null) imagemBotaoGuest = guestT.GetComponent<Image>();
        }

        if (imagemTitulo == null && canvasRaiz != null)
        {
            Transform tituloT = BuscarTransformFilhoPorNome(canvasRaiz.transform, "Titulo");
            if (tituloT == null) tituloT = BuscarTransformFilhoPorNome(canvasRaiz.transform, "TITLE");
            if (tituloT != null) imagemTitulo = tituloT.GetComponent<Image>();
        }

        if (botaoHost == null && imagemBotaoHost != null)
        {
            botaoHost = GarantirBotaoEmImagem(imagemBotaoHost, "BTN_HOST", sprCriarSala);
        }

        if (botaoGuest == null && imagemBotaoGuest != null)
        {
            botaoGuest = GarantirBotaoEmImagem(imagemBotaoGuest, "BTN_GUEST", sprEntrarSala);
        }

        if (botaoHost == null || botaoGuest == null)
        {
            var botoes = mainJoinCanvas.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < botoes.Length; i++)
            {
                if (botaoHost == null && NomePareceHost(botoes[i].name)) botaoHost = botoes[i];
                if (botaoGuest == null && NomePareceGuest(botoes[i].name)) botaoGuest = botoes[i];
            }
        }

        if (botaoHost == null && mainJoinCanvas != null)
        {
            botaoHost = CriarBotaoComImagem(
                "BTN_HOST",
                mainJoinCanvas.transform,
                sprCriarSala,
                new Vector2(0.5f, 0.56f),
                new Vector2(0.5f, 0.56f),
                Vector2.zero,
                new Vector2(470f, 130f));
        }

        if (botaoGuest == null && mainJoinCanvas != null)
        {
            botaoGuest = CriarBotaoComImagem(
                "BTN_GUEST",
                mainJoinCanvas.transform,
                sprEntrarSala,
                new Vector2(0.5f, 0.39f),
                new Vector2(0.5f, 0.39f),
                Vector2.zero,
                new Vector2(470f, 130f));
        }

        if (imagemBotaoHost == null && botaoHost != null) imagemBotaoHost = botaoHost.GetComponent<Image>();
        if (imagemBotaoGuest == null && botaoGuest != null) imagemBotaoGuest = botaoGuest.GetComponent<Image>();

        if (botaoVoltar == null && serversHubCanvas != null)
        {
            var botoes = serversHubCanvas.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < botoes.Length; i++)
            {
                if (botoes[i].name.IndexOf("back", StringComparison.OrdinalIgnoreCase) >= 0
                    || botoes[i].name.IndexOf("voltar", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    botaoVoltar = botoes[i];
                    break;
                }
            }
        }

        if (botaoVoltar == null && serversHubCanvas != null)
        {
            botaoVoltar = CriarBotaoComImagem(
                "BTN_BACK",
                serversHubCanvas.transform,
                sprOffButton,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(34f, -34f),
                new Vector2(84f, 84f));
        }

        if (containerSalas == null && serversHubCanvas != null)
        {
            Transform achado = BuscarTransformFilhoPorNome(serversHubCanvas.transform, "RoomsContent");
            if (achado == null) achado = BuscarTransformFilhoPorNome(serversHubCanvas.transform, "RoomsContainer");
            if (achado != null) containerSalas = achado as RectTransform;
        }

        if (containerSalas == null && serversHubCanvas != null)
        {
            containerSalas = CriarEstruturaScroll(serversHubCanvas.transform);
        }

        if (raizListaSalas == null && containerSalas != null)
        {
            var scroll = containerSalas.GetComponentInParent<ScrollRect>(true);
            if (scroll != null && scroll.transform is RectTransform rt)
            {
                raizListaSalas = rt;
            }
        }

        GarantirPainelNome();
        ConfigurarVisualListaSalas();
        ConfigurarBotaoVoltarTopoEsquerdo();
        GarantirAnimacaoTituloFlutuando();
    }

    Canvas GarantirCanvasRaiz()
    {
        if (canvasRaiz != null) return canvasRaiz;

        var canvasExistente = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvasExistente != null)
        {
            canvasRaiz = canvasExistente.rootCanvas != null ? canvasExistente.rootCanvas : canvasExistente;
            return canvasRaiz;
        }

        var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasRaiz = go.GetComponent<Canvas>();
        canvasRaiz.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvasRaiz;
    }

    GameObject CriarMainJoinCanvas(Transform parent)
    {
        var go = new GameObject("Main_Join", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        EsticarTela(rt);

        var bg = go.GetComponent<Image>();
        if (sprServerHub != null)
        {
            bg.sprite = sprServerHub;
            bg.color = Color.white;
        }
        else
        {
            bg.color = new Color(0.05f, 0.07f, 0.12f, 0.85f);
        }

        var titulo = CriarImagem(
            "TITLE",
            go.transform,
            sprTitle,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -120f),
            new Vector2(720f, 126f));
        if (titulo != null) titulo.gameObject.name = "TITLE";

        botaoHost = CriarBotaoComImagem(
            "BTN_HOST",
            go.transform,
            sprCriarSala,
            new Vector2(0.5f, 0.56f),
            new Vector2(0.5f, 0.56f),
            Vector2.zero,
            new Vector2(470f, 130f));

        botaoGuest = CriarBotaoComImagem(
            "BTN_GUEST",
            go.transform,
            sprEntrarSala,
            new Vector2(0.5f, 0.39f),
            new Vector2(0.5f, 0.39f),
            Vector2.zero,
            new Vector2(470f, 130f));

        return go;
    }

    GameObject CriarServersHubCanvas(Transform parent)
    {
        var go = new GameObject("SERVERS_HUB", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        EsticarTela(rt);

        var bg = go.GetComponent<Image>();
        if (sprServerHub != null)
        {
            bg.sprite = sprServerHub;
            bg.color = Color.white;
        }
        else
        {
            bg.color = new Color(0.05f, 0.07f, 0.12f, 0.92f);
        }

        CriarImagem(
            "TITLE",
            go.transform,
            sprTitle,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -120f),
            new Vector2(720f, 126f));

        containerSalas = CriarEstruturaScroll(go.transform);

        botaoVoltar = CriarBotaoComImagem(
            "BTN_BACK",
            go.transform,
            sprOffButton,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(34f, -34f),
            new Vector2(84f, 84f));

        go.SetActive(false);
        return go;
    }

    RectTransform CriarEstruturaScroll(Transform parent)
    {
        var scrollGo = new GameObject("RoomsScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.SetParent(parent, false);
        scrollRt.anchorMin = new Vector2(0.14f, 0.2f);
        scrollRt.anchorMax = new Vector2(0.86f, 0.76f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        var scrollImage = scrollGo.GetComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0.34f);

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.SetParent(scrollGo.transform, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;

        var viewportImg = viewportGo.GetComponent<Image>();
        viewportImg.color = new Color(0f, 0f, 0f, 0.02f);
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;

        var contentGo = new GameObject("RoomsContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.SetParent(viewportGo.transform, false);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.offsetMin = new Vector2(12f, 0f);
        contentRt.offsetMax = new Vector2(-12f, 0f);

        var layout = contentGo.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(8, 8, 10, 10);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.viewport = viewportRt;
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 16f;

        return contentRt;
    }

    void GarantirPainelNome()
    {
        if (canvasRaiz == null) return;

        if (painelNome == null)
        {
            var achado = BuscarTransformFilhoPorNome(canvasRaiz.transform, "PainelNome");
            if (achado is RectTransform rtAchado) painelNome = rtAchado;
        }

        if (painelNome == null)
        {
            var go = new GameObject("PainelNome", typeof(RectTransform), typeof(Image));
            painelNome = go.GetComponent<RectTransform>();
            painelNome.SetParent(canvasRaiz.transform, false);
            painelNome.anchorMin = new Vector2(0.5f, 0.5f);
            painelNome.anchorMax = new Vector2(0.5f, 0.5f);
            painelNome.pivot = new Vector2(0.5f, 0.5f);
            painelNome.anchoredPosition = new Vector2(0f, -56f);
            painelNome.sizeDelta = new Vector2(960f, 240f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.38f);
        }
        else if (painelNome.transform.parent != canvasRaiz.transform)
        {
            painelNome.SetParent(canvasRaiz.transform, false);
        }

        if (campoNome == null)
        {
            campoNome = CriarCampoNomeInput(painelNome);
        }
        else
        {
            campoNome.transform.SetParent(painelNome, false);
        }

        if (botaoConfirmarNome == null)
        {
            botaoConfirmarNome = CriarBotaoComImagem(
                "BTN_CONFIRMAR_NOME",
                painelNome,
                sprOnButton,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(132f, 34f),
                new Vector2(18f, 18f));
        }
        else
        {
            var rt = botaoConfirmarNome.GetComponent<RectTransform>();
            rt.SetParent(painelNome, false);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(132f, 34f);
            rt.sizeDelta = new Vector2(18f, 18f);
            var img = botaoConfirmarNome.GetComponent<Image>();
            if (img != null && sprOnButton != null) img.sprite = sprOnButton;
        }

        if (BuscarTransformFilhoPorNome(botaoConfirmarNome.transform, "TXT_CONFIRMAR") == null)
        {
            CriarTexto(
                "TXT_CONFIRMAR",
                botaoConfirmarNome.transform,
                "ENTRAR",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-28f, 0f),
                new Vector2(240f, 44f),
                20f,
                TextAlignmentOptions.Right);
        }

        ConfigurarVisualPainelNome();
    }

    TMP_InputField CriarCampoNomeInput(Transform parent)
    {
        var go = new GameObject("CampoNome", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -8f);
        rt.sizeDelta = new Vector2(700f, 76f);

        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.1f, 0.12f, 0.16f, 0.88f);

        var input = go.GetComponent<TMP_InputField>();
        input.characterLimit = 20;

        var textAreaGo = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        var textAreaRt = textAreaGo.GetComponent<RectTransform>();
        textAreaRt.SetParent(go.transform, false);
        textAreaRt.anchorMin = Vector2.zero;
        textAreaRt.anchorMax = Vector2.one;
        textAreaRt.offsetMin = new Vector2(20f, 14f);
        textAreaRt.offsetMax = new Vector2(-20f, -14f);

        var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        var placeholderRt = placeholderGo.GetComponent<RectTransform>();
        placeholderRt.SetParent(textAreaGo.transform, false);
        placeholderRt.anchorMin = Vector2.zero;
        placeholderRt.anchorMax = Vector2.one;
        placeholderRt.offsetMin = Vector2.zero;
        placeholderRt.offsetMax = Vector2.zero;

        var placeholder = placeholderGo.GetComponent<TextMeshProUGUI>();
        placeholder.text = "Digite seu nome...";
        placeholder.fontSize = 22f;
        placeholder.color = new Color(1f, 1f, 1f, 0.45f);
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.textWrappingMode = TextWrappingModes.NoWrap;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.SetParent(textAreaGo.transform, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = string.IsNullOrWhiteSpace(DadosSessaoLocal.NomeUsuario) ? string.Empty : DadosSessaoLocal.NomeUsuario;
        text.fontSize = 24f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        input.textViewport = textAreaRt;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.text = text.text;

        return input;
    }

    void ConfigurarBotaoVoltarTopoEsquerdo()
    {
        if (botaoVoltar == null) return;

        var rt = botaoVoltar.GetComponent<RectTransform>();
        if (canvasRaiz != null && rt != null && rt.parent != canvasRaiz.transform)
        {
            rt.SetParent(canvasRaiz.transform, false);
        }

        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(136f, -182f);
            rt.sizeDelta = new Vector2(26f, 26f);
        }

        var img = botaoVoltar.GetComponent<Image>();
        if (img != null)
        {
            if (sprOffButton != null) img.sprite = sprOffButton;
            img.preserveAspect = true;
            img.color = Color.white;
        }

        if (BuscarTransformFilhoPorNome(botaoVoltar.transform, "TXT_VOLTAR") == null)
        {
            CriarTexto(
                "TXT_VOLTAR",
                botaoVoltar.transform,
                "VOLTAR",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(94f, 0f),
                new Vector2(240f, 42f),
                15f,
                TextAlignmentOptions.Left);
        }
        else
        {
            Transform txt = BuscarTransformFilhoPorNome(botaoVoltar.transform, "TXT_VOLTAR");
            if (txt != null && txt.TryGetComponent<TextMeshProUGUI>(out var tmp))
            {
                var txtRt = tmp.GetComponent<RectTransform>();
                txtRt.anchorMin = new Vector2(0.5f, 0.5f);
                txtRt.anchorMax = new Vector2(0.5f, 0.5f);
                txtRt.pivot = new Vector2(0f, 0.5f);
                txtRt.anchoredPosition = new Vector2(94f, 0f);
                txtRt.sizeDelta = new Vector2(240f, 42f);
                tmp.fontSize = 15f;
                tmp.alignment = TextAlignmentOptions.Left;
            }
        }
    }

    void ConfigurarVisualListaSalas()
    {
        if (raizListaSalas == null) return;

        raizListaSalas.anchorMin = new Vector2(0.08f, 0.1f);
        raizListaSalas.anchorMax = new Vector2(0.92f, 0.68f);
        raizListaSalas.offsetMin = Vector2.zero;
        raizListaSalas.offsetMax = Vector2.zero;
    }

    static void EsticarTela(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    bool NomePareceHost(string nomeObjeto)
    {
        return nomeObjeto.IndexOf("host", StringComparison.OrdinalIgnoreCase) >= 0
            || nomeObjeto.IndexOf("criar", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    bool NomePareceGuest(string nomeObjeto)
    {
        return nomeObjeto.IndexOf("guest", StringComparison.OrdinalIgnoreCase) >= 0
            || nomeObjeto.IndexOf("entrar", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static Transform BuscarTransformPorNome(string nome)
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

    static Transform BuscarTransformFilhoPorNome(Transform raiz, string nome)
    {
        if (raiz == null) return null;
        var filhos = raiz.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < filhos.Length; i++)
        {
            if (string.Equals(filhos[i].name, nome, StringComparison.OrdinalIgnoreCase))
            {
                return filhos[i];
            }
        }

        return null;
    }

    Button GarantirBotaoEmImagem(Image imagem, string nomeBotao, Sprite spriteDesejado)
    {
        if (imagem == null) return null;

        if (!string.IsNullOrWhiteSpace(nomeBotao))
        {
            imagem.gameObject.name = nomeBotao;
        }

        imagem.raycastTarget = true;
        if (spriteDesejado != null)
        {
            imagem.sprite = spriteDesejado;
            imagem.preserveAspect = true;
        }

        var botao = imagem.GetComponent<Button>();
        if (botao == null)
        {
            botao = imagem.gameObject.AddComponent<Button>();
        }

        if (botao.targetGraphic == null)
        {
            botao.targetGraphic = imagem;
        }

        var cores = botao.colors;
        cores.normalColor = Color.white;
        cores.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
        cores.pressedColor = new Color(0.9f, 0.9f, 0.9f, 0.92f);
        cores.selectedColor = Color.white;
        cores.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
        botao.colors = cores;

        return botao;
    }

    void GarantirAnimacaoTituloFlutuando()
    {
        if (!animarTituloFlutuando || imagemTitulo == null) return;

        var flutuar = imagemTitulo.GetComponent<FlutuarSuaveUI>();
        if (flutuar == null)
        {
            flutuar = imagemTitulo.gameObject.AddComponent<FlutuarSuaveUI>();
        }

        flutuar.ConfigurarPadrao();
    }

    Button CriarBotaoComImagem(
        string nome,
        Transform parent,
        Sprite sprite,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        var image = go.GetComponent<Image>();
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        else
        {
            image.color = new Color(0.14f, 0.34f, 0.2f, 0.95f);
        }

        return go.GetComponent<Button>();
    }

    Image CriarImagem(
        string nome,
        Transform parent,
        Sprite sprite,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        var image = go.GetComponent<Image>();
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
        }
        else
        {
            image.color = new Color(1f, 1f, 1f, 0.15f);
        }

        return image;
    }

    TextMeshProUGUI CriarTexto(
        string nome,
        Transform parent,
        string texto,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        float fontSize,
        TextAlignmentOptions alinhamento)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = fontSize;
        tmp.alignment = alinhamento;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        return tmp;
    }

    void GarantirEventSystem()
    {
        if (EventSystem.current != null) return;

        var eventSystem = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem != null)
        {
            if (!eventSystem.gameObject.activeInHierarchy) eventSystem.gameObject.SetActive(true);
            return;
        }

        var go = new GameObject("EventSystem", typeof(EventSystem));

#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    void CarregarSpritesAutomaticamenteSePreciso()
    {
        var spritesTitle = CarregarSprites("Interface/Multiplayer/spr_title", "Assets/Sprites/Interface/Multiplayer/spr_title.png");
        var spritesHub = CarregarSprites("Interface/Multiplayer/spr_serverhub", "Assets/Sprites/Interface/Multiplayer/spr_serverhub.png");
        var spritesRoomsKind = CarregarSprites("Interface/Multiplayer/spr_rooms_kind", "Assets/Sprites/Interface/Multiplayer/spr_rooms_kind.png");
        var spritesPlayers = CarregarSprites("Interface/Multiplayer/spr_players", "Assets/Sprites/Interface/Multiplayer/spr_players.png");
        var spritesButtons = CarregarSprites("Interface/Multiplayer/spr_buttons", "Assets/Sprites/Interface/Multiplayer/spr_buttons.png");
        var spritesDiff = CarregarSprites("Interface/Multiplayer/difficulty_sprites", "Assets/Sprites/Interface/Multiplayer/difficulty_sprites.png");

        if (sprTitle == null) sprTitle = EscolherSprite(spritesTitle, "7_0");
        if (sprServerHub == null) sprServerHub = EscolherSprite(spritesHub, "8_0");
        if (sprCriarSala == null) sprCriarSala = EscolherSprite(spritesRoomsKind, "criarSala");
        if (sprEntrarSala == null) sprEntrarSala = EscolherSprite(spritesRoomsKind, "entrarSala", fallbackIndex: 1);
        if (sprPlayers == null) sprPlayers = EscolherSprite(spritesPlayers, "9_0");
        if (sprOnButton == null) sprOnButton = EscolherSprite(spritesButtons, "spr_on_button");
        if (sprOffButton == null) sprOffButton = EscolherSprite(spritesButtons, "spr_off_button");
        if (sprEasyDiff == null) sprEasyDiff = EscolherSprite(spritesDiff, "spr_easy_diff");
    }

    static Sprite[] CarregarSprites(string caminhoResources, string caminhoAssetEditor)
    {
        Sprite[] viaResources = Resources.LoadAll<Sprite>(caminhoResources);
        if (viaResources != null && viaResources.Length > 0) return viaResources;

#if UNITY_EDITOR
        return AssetDatabase.LoadAllAssetsAtPath(caminhoAssetEditor).OfType<Sprite>().ToArray();
#else
        return Array.Empty<Sprite>();
#endif
    }

    static Sprite EscolherSprite(Sprite[] sprites, string nomePreferido, int fallbackIndex = 0)
    {
        if (sprites == null || sprites.Length == 0) return null;

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && string.Equals(sprites[i].name, nomePreferido, StringComparison.OrdinalIgnoreCase))
            {
                return sprites[i];
            }
        }

        int idx = Mathf.Clamp(fallbackIndex, 0, sprites.Length - 1);
        return sprites[idx];
    }
}
