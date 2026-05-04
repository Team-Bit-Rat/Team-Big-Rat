using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ServerHubTela : MonoBehaviour
{
    [SerializeField] string cenaJogo = "CenaDoCT";

    DescobridorHostLAN descobridor;
    Vector2 scroll;
    string nome = "";
    bool telaEntrar;

    void Awake()
    {
        nome = DadosSessaoLocal.NomeUsuario;

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
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void GarantirHubNaCena()
    {
        if (SceneManager.GetActiveScene().name != "ServerHub") return;
        if (FindFirstObjectByType<ServerHubTela>() != null) return;

        var go = new GameObject("ServerHub_Controller");
        go.AddComponent<ServerHubTela>();
        Debug.Log("[Hub] Controller criado automaticamente na cena ServerHub.");
    }

    void OnGUI()
    {
        const int largura = 560;
        const int altura = 420;
        var area = new Rect((Screen.width - largura) / 2f, (Screen.height - altura) / 2f, largura, altura);
        GUILayout.BeginArea(area, "ServerHub", GUI.skin.window);

        if (!telaEntrar)
        {
            DesenharTelaInicial();
            GUILayout.EndArea();
            return;
        }

        GUILayout.Space(8);
        GUILayout.Label("Seu nome:");
        nome = GUILayout.TextField(nome ?? string.Empty, 24);

        GUILayout.Space(8);
        GUILayout.Label("Hosts LAN disponiveis:");

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(260));
        if (descobridor == null || descobridor.Hosts.Count == 0)
        {
            GUILayout.Label("Nenhum host encontrado ainda...");
        }
        else
        {
            for (int i = 0; i < descobridor.Hosts.Count; i++)
            {
                var h = descobridor.Hosts[i];
                GUILayout.BeginHorizontal("box");
                GUILayout.Label($"{h.Nome}  [{h.Ip}:{h.Porta}]", GUILayout.Width(350));
                if (GUILayout.Button("Conectar", GUILayout.Width(120)))
                {
                    Conectar(h.Ip, h.Porta);
                }
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndScrollView();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Atualizar")) { }
        if (GUILayout.Button("Voltar"))
        {
            telaEntrar = false;
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    void DesenharTelaInicial()
    {
        GUILayout.Space(14);
        GUILayout.Label("Seu nome (host):");
        nome = GUILayout.TextField(nome ?? string.Empty, 24);

        GUILayout.Space(16);
        if (GUILayout.Button("Criar partida", GUILayout.Height(44)))
        {
            CriarPartida();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Entrar numa partida", GUILayout.Height(44)))
        {
            telaEntrar = true;
        }
    }

    void CriarPartida()
    {
        string nomeFinal = string.IsNullOrWhiteSpace(nome) ? "Host" : nome.Trim();
        if (nomeFinal.Length < 2)
        {
            Debug.LogWarning("[Hub] Digite um nome de host com pelo menos 2 letras.");
            return;
        }

        DadosSessaoLocal.NomeUsuario = nomeFinal;
        ConexaoPendente.CriarHost = true;
        ConexaoPendente.Ativa = false;
        SceneManager.LoadScene(cenaJogo);
    }

    void Conectar(string ip, int porta)
    {
        string nomeFinal = string.IsNullOrWhiteSpace(nome) ? "Jogador" : nome.Trim();
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
}
