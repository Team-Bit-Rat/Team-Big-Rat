# Mapa rapido dos scripts

## Rede (principal)
- `Rede/Client.cs`: fluxo dos botoes, host/client, reconexao pendente.
- `Rede/ServerHubTela.cs`: tela do hub via Canvas/Image/Button (Main_Join + SERVERS_HUB), lista salas LAN e conecta host/guest.
- `Rede/RedeBootstrap.cs`: utilitario central de rede (config, porta livre, start host).
- `Rede/GerenciadorNomesRede.cs`: aprova conexao e bloqueia nome repetido.
- `Rede/DescobridorHostLAN.cs`: lista hosts LAN (nome, ip, porta, qtd de players e dificuldade).
- `Rede/AnuncianteHostLAN.cs`: anuncia host na LAN com metadados da sala.

## Gameplay
- `Gameplay/Movimentacao.cs`: movimento de rede, pulo, dash, ataque com debounce e animacao por sprites.
- `Gameplay/BlocoColisaoMapa.cs`: helper simples pra bloco/plataforma de colisao.

## Core
- `Core/DadosSessaoLocal.cs`: guarda dados locais da sessao (nome).
- `Rede/ConexaoPendente.cs`: estado temporario entre cenas para conectar/criar host.

## Scripts de suporte do Netcode
- `Rede/ClientNetworkTransform.cs`
- `Rede/ClientNetworkAnimator.cs`

## UI / Animacoes
- `Animacoes/PulsarUI.cs`: componente de pulso visual para Image/Titulo.
- `Animacoes/FlutuarSuaveUI.cs`: flutuacao suave vertical para titulo/elementos de UI.
