using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public sealed class DescobridorHostLAN : MonoBehaviour
{
    public readonly List<HostLanInfo> Hosts = new();

    UdpClient udp;
    IPEndPoint origem;

    void Awake()
    {
        try
        {
            udp = new UdpClient();
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, RedeLanConst.PortaDescoberta));
            udp.Client.Blocking = false;
            origem = new IPEndPoint(IPAddress.Any, 0);
            Debug.Log("[LAN] Descobridor de hosts ligado.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LAN] Falha ao ligar descobridor: {ex.Message}");
        }
    }

    void Update()
    {
        if (udp == null) return;

        try
        {
            while (udp.Available > 0)
            {
                byte[] dados = udp.Receive(ref origem);
                LerPacote(dados);
            }
        }
        catch (SocketException)
        {
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LAN] Erro no recebimento: {ex.Message}");
        }

        LimparHostsAntigos();
    }

    void LerPacote(byte[] dados)
    {
        string texto = Encoding.UTF8.GetString(dados);
        string[] p = texto.Split('|');
        if (p.Length < 4) return;
        if (p[0] != RedeLanConst.AssinaturaHost) return;

        string nome = p[1];
        string ip = p[2];
        if (!int.TryParse(p[3], out int porta)) return;
        int quantidadeJogadores = 1;
        if (p.Length >= 5 && int.TryParse(p[4], out int qtdLida))
        {
            quantidadeJogadores = Mathf.Max(1, qtdLida);
        }

        string dificuldade = (p.Length >= 6 && !string.IsNullOrWhiteSpace(p[5]))
            ? p[5]
            : "easy";

        string id = $"{ip}:{porta}";
        float agora = Time.unscaledTime;

        for (int i = 0; i < Hosts.Count; i++)
        {
            if (Hosts[i].Id == id)
            {
                var atualizado = Hosts[i];
                atualizado.Nome = nome;
                atualizado.QuantidadeJogadores = quantidadeJogadores;
                atualizado.Dificuldade = dificuldade;
                atualizado.UltimoPing = agora;
                Hosts[i] = atualizado;
                return;
            }
        }

        Hosts.Add(new HostLanInfo
        {
            Id = id,
            Nome = nome,
            Ip = ip,
            Porta = porta,
            QuantidadeJogadores = quantidadeJogadores,
            Dificuldade = dificuldade,
            UltimoPing = agora,
        });
    }

    void LimparHostsAntigos()
    {
        float limite = Time.unscaledTime - 3.5f;
        for (int i = Hosts.Count - 1; i >= 0; i--)
        {
            if (Hosts[i].UltimoPing < limite)
            {
                Hosts.RemoveAt(i);
            }
        }
    }

    void OnDestroy()
    {
        udp?.Close();
        udp = null;
    }
}

[Serializable]
public struct HostLanInfo
{
    public string Id;
    public string Nome;
    public string Ip;
    public int Porta;
    public int QuantidadeJogadores;
    public string Dificuldade;
    public float UltimoPing;
}
