# Mapa rapido dos scripts

## Rede (principal)
- `Rede/Client.cs`: fluxo dos botoes, host/client, reconexao pendente.
- `Rede/ServerHubTela.cs`: tela do hub (criar partida / entrar numa partida).
- `Rede/RedeBootstrap.cs`: utilitario central de rede (config, porta livre, start host).
- `Rede/GerenciadorNomesRede.cs`: aprova conexao e bloqueia nome repetido.
- `Rede/DescobridorHostLAN.cs`: lista hosts LAN.
- `Rede/AnuncianteHostLAN.cs`: anuncia host na LAN.

## Gameplay
- `Gameplay/Movimentacao.cs`: movimento, gravidade, pulo e nome acima do player.
- `Gameplay/BlocoColisaoMapa.cs`: helper simples pra bloco/plataforma de colisao.

## Core
- `Core/DadosSessaoLocal.cs`: guarda dados locais da sessao (nome).
- `Rede/ConexaoPendente.cs`: estado temporario entre cenas para conectar/criar host.

## Scripts de suporte do Netcode
- `Rede/ClientNetworkTransform.cs`
- `Rede/ClientNetworkAnimator.cs`
