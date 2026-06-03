using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

public sealed class GerenciadorNomesRede : MonoBehaviour
{
    static readonly Dictionary<ulong, string> nomesPorClient = new();
    static readonly HashSet<string> nomesEmUso = new(System.StringComparer.OrdinalIgnoreCase);

    NetworkManager nm;

    void Awake()
    {
        nomesPorClient.Clear();
        nomesEmUso.Clear();

        nm = GetComponent<NetworkManager>();
        if (nm == null) return;

        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = AprovarConexao;
        nm.OnClientDisconnectCallback += OnClientSaiu;
        Debug.Log("[NomesRede] Aprovador de conexao ativo.");
    }

    void OnDestroy()
    {
        if (nm != null)
        {
            nm.OnClientDisconnectCallback -= OnClientSaiu;
        }
    }

    void AprovarConexao(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse resp)
    {
        string nome = Encoding.UTF8.GetString(req.Payload ?? System.Array.Empty<byte>()).Trim();
        if (string.IsNullOrWhiteSpace(nome) || nome.Length < 2)
        {
            Reprovar(resp, "Nome invalido. Use pelo menos 2 letras.");
            Debug.LogWarning($"[NomesRede] Cliente {req.ClientNetworkId} recusado: nome invalido.");
            return;
        }

        if (nome.Length > 20) nome = nome.Substring(0, 20);

        if (nomesEmUso.Contains(nome))
        {
            Reprovar(resp, "Nome ja em uso. Escolha outro.");
            Debug.LogWarning($"[NomesRede] Cliente {req.ClientNetworkId} recusado: nome repetido ({nome}).");
            return;
        }

        nomesEmUso.Add(nome);
        nomesPorClient[req.ClientNetworkId] = nome;

        resp.Approved = true;
        resp.CreatePlayerObject = true;
        resp.Pending = false;
        resp.Reason = string.Empty;
        Debug.Log($"[NomesRede] Cliente {req.ClientNetworkId} aprovado como '{nome}'.");
    }

    void OnClientSaiu(ulong clientId)
    {
        if (!nomesPorClient.TryGetValue(clientId, out string nome)) return;
        nomesPorClient.Remove(clientId);
        nomesEmUso.Remove(nome);
    }

    static void Reprovar(NetworkManager.ConnectionApprovalResponse resp, string motivo)
    {
        resp.Approved = false;
        resp.CreatePlayerObject = false;
        resp.Pending = false;
        resp.Reason = motivo;
    }

    public static string PegarNomeDoClient(ulong clientId)
    {
        if (nomesPorClient.TryGetValue(clientId, out string nome)) return nome;
        return "Jogador";
    }
}
