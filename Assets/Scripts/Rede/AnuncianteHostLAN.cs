using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using UnityEngine;

public sealed class AnuncianteHostLAN : MonoBehaviour
{
    UdpClient udp;
    IPEndPoint destino;
    float proximoEnvio;
    string nomeHost = "Host";
    int portaJogo = 7777;
    string dificuldadeAtual = "easy";
    NetworkManager nm;

    public void IniciarAnuncio(string nome, int porta, string dificuldade = "easy")
    {
        nomeHost = string.IsNullOrWhiteSpace(nome) ? "Host" : nome.Trim();
        portaJogo = porta;
        dificuldadeAtual = string.IsNullOrWhiteSpace(dificuldade) ? "easy" : dificuldade.Trim().ToLowerInvariant();
        nm = NetworkManager.Singleton;

        if (udp != null) return;

        try
        {
            udp = new UdpClient();
            udp.EnableBroadcast = true;
            destino = new IPEndPoint(IPAddress.Broadcast, RedeLanConst.PortaDescoberta);
            Debug.Log("[LAN] Anunciante de host ligado.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LAN] Falha ao iniciar anunciante: {ex.Message}");
        }
    }

    void Update()
    {
        if (udp == null) return;
        if (Time.unscaledTime < proximoEnvio) return;

        proximoEnvio = Time.unscaledTime + 1.0f;
        try
        {
            string ip = PegarIpLocal();
            int jogadores = PegarQuantidadeJogadores();
            string payload = $"{RedeLanConst.AssinaturaHost}|{nomeHost}|{ip}|{portaJogo}|{jogadores}|{dificuldadeAtual}";
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            udp.Send(bytes, bytes.Length, destino);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LAN] Erro no anuncio: {ex.Message}");
        }
    }

    void OnDestroy()
    {
        udp?.Close();
        udp = null;
    }

    int PegarQuantidadeJogadores()
    {
        if (nm == null) nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return 1;
        return Mathf.Max(1, nm.ConnectedClientsIds.Count);
    }

    static string PegarIpLocal()
    {
        try
        {
            string host = Dns.GetHostName();
            var ips = Dns.GetHostAddresses(host);
            foreach (var ip in ips)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch { }

        return "127.0.0.1";
    }
}
