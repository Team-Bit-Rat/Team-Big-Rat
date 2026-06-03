using System.Collections;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Client : MonoBehaviour
{
    static Client instancia;
    bool tentandoPendente;

    [SerializeField] string cenaHub = "ServerHub";
    [SerializeField] int portaPadrao = 7777;
    [SerializeField] int tentativasPorta = 20;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void GarantirRuntime()
    {
        if (instancia != null) return;
        if (FindFirstObjectByType<Client>() != null) return;

        var go = new GameObject("ClientRuntime");
        instancia = go.AddComponent<Client>();
        DontDestroyOnLoad(go);
        Debug.Log("[Client] Runtime criado.");
    }

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        VincularCallbacksNetcode();
    }

    void Start()
    {
        TentarProcessarPendencia();
    }

    void Update()
    {
        // Se a pendencia foi marcada depois do Start, pega aqui.
        if (!tentandoPendente && (ConexaoPendente.Ativa || ConexaoPendente.CriarHost))
        {
            TentarProcessarPendencia();
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDesconectado;
        }

        if (instancia == this) instancia = null;
    }

    void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        VincularCallbacksNetcode();
        TentarProcessarPendencia();
    }

    void VincularCallbacksNetcode()
    {
        if (NetworkManager.Singleton == null) return;

        RedeBootstrap.GarantirBase(NetworkManager.Singleton);
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDesconectado;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDesconectado;
    }

    void TentarProcessarPendencia()
    {
        if (tentandoPendente) return;
        if (!ConexaoPendente.Ativa && !ConexaoPendente.CriarHost) return;
        StartCoroutine(ProcessarPendencia());
    }

    IEnumerator ProcessarPendencia()
    {
        tentandoPendente = true;

        float limite = Time.realtimeSinceStartup + 4f;
        while (NetworkManager.Singleton == null && Time.realtimeSinceStartup < limite)
        {
            yield return null;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[Client] Pendencia existe, mas sem NetworkManager na cena.");
            tentandoPendente = false;
            yield break;
        }

        RedeBootstrap.GarantirBase(NetworkManager.Singleton);

        if (NetworkManager.Singleton.IsListening)
        {
            ConexaoPendente.Ativa = false;
            ConexaoPendente.CriarHost = false;
            tentandoPendente = false;
            yield break;
        }

        if (ConexaoPendente.CriarHost)
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(DadosSessaoLocal.NomeUsuario ?? "Host");

            bool okHost = RedeBootstrap.IniciarHostNaPrimeiraPortaLivre(
                NetworkManager.Singleton,
                portaPadrao,
                tentativasPorta,
                out int portaUsada);

            if (okHost)
            {
                RedeBootstrap.GarantirAnunciante(NetworkManager.Singleton, DadosSessaoLocal.NomeUsuario, portaUsada);
                Debug.Log($"[Client] Host iniciado na porta {portaUsada}.");
            }
            else
            {
                Debug.LogWarning("[Client] Falha ao iniciar host pendente.");
            }

            ConexaoPendente.CriarHost = false;
            tentandoPendente = false;
            yield break;
        }

        RedeBootstrap.ConfigurarDestino(NetworkManager.Singleton, ConexaoPendente.Ip, ConexaoPendente.Porta);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(DadosSessaoLocal.NomeUsuario ?? "Jogador");

        bool ok = NetworkManager.Singleton.StartClient();
        Debug.Log(ok
            ? $"[Client] Conectando em {ConexaoPendente.Ip}:{ConexaoPendente.Porta}."
            : "[Client] Falha ao iniciar client pendente.");

        if (ok) ConexaoPendente.Ativa = false;
        tentandoPendente = false;
    }

    void OnClientDesconectado(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;
        if (NetworkManager.Singleton.IsServer) return;

        string motivo = NetworkManager.Singleton.DisconnectReason;
        if (!string.IsNullOrWhiteSpace(motivo))
        {
            Debug.LogWarning($"[Client] Conexao recusada: {motivo}");
        }
        else
        {
            Debug.LogWarning("[Client] Desconectado do host.");
        }

        if (SceneManager.GetActiveScene().name != cenaHub)
        {
            SceneManager.LoadScene(cenaHub);
        }
    }
}
