using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public static class RedeBootstrap
{
    public static void GarantirBase(NetworkManager nm)
    {
        if (nm == null) return;

        nm.NetworkConfig.ProtocolVersion = 1;
        nm.NetworkConfig.ConnectionApproval = true;

        if (nm.GetComponent<GerenciadorNomesRede>() == null)
        {
            nm.gameObject.AddComponent<GerenciadorNomesRede>();
        }
    }

    public static void ConfigurarDestino(NetworkManager nm, string ip, int porta)
    {
        if (nm == null) return;

        var utp = nm.GetComponent<UnityTransport>();
        if (utp == null) return;

        var dados = utp.ConnectionData;
        dados.Address = ip;
        dados.Port = (ushort)porta;
        dados.ClientBindPort = 0;
        utp.ConnectionData = dados;
    }

    public static void ConfigurarHost(NetworkManager nm, int porta)
    {
        if (nm == null) return;

        var utp = nm.GetComponent<UnityTransport>();
        if (utp == null) return;

        var dados = utp.ConnectionData;
        dados.Address = "127.0.0.1";
        dados.Port = (ushort)porta;
        dados.ServerListenAddress = "0.0.0.0";
        dados.ClientBindPort = 0;
        utp.ConnectionData = dados;
    }

    public static bool PortaLivre(int porta)
    {
        UdpClient udp = null;
        try
        {
            udp = new UdpClient(new IPEndPoint(IPAddress.Any, porta));
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        finally
        {
            udp?.Dispose();
        }
    }

    public static bool IniciarHostNaPrimeiraPortaLivre(NetworkManager nm, int portaInicial, int tentativas, out int portaUsada)
    {
        portaUsada = -1;
        if (nm == null) return false;

        GarantirBase(nm);

        int limite = Mathf.Max(1, tentativas);
        for (int i = 0; i < limite; i++)
        {
            int portaAtual = portaInicial + i;
            if (!PortaLivre(portaAtual))
            {
                Debug.Log($"[Rede] Porta {portaAtual} ocupada, tentando proxima...");
                continue;
            }

            ConfigurarHost(nm, portaAtual);
            if (nm.StartHost())
            {
                portaUsada = portaAtual;
                return true;
            }

            nm.Shutdown();
        }

        return false;
    }

    public static void GarantirAnunciante(NetworkManager nm, string nome, int porta)
    {
        if (nm == null) return;

        var anunciador = nm.GetComponent<AnuncianteHostLAN>();
        if (anunciador == null)
        {
            anunciador = nm.gameObject.AddComponent<AnuncianteHostLAN>();
        }

        anunciador.IniciarAnuncio(nome, porta);
    }
}
