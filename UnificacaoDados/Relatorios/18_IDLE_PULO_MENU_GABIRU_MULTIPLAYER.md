# 18 - Idle Lento, Pulo Alto e Menu Gabiru Multiplayer

Data: 2026-05-24
Branch atual: `ct-teste`

## Contexto relido antes da alteracao
- `UNIFICACAO_CT_TESTE_ACOMPANHAMENTO.md`
- `README.md`
- `Assets/Scripts/README.md`
- `UnificacaoDados/Relatorios/06_ANALISE_FUNCIONALIDADES_E_SCRIPTS.md`
- `UnificacaoDados/Relatorios/13_CORRECAO_MOVIMENTO_CAMERA_CENA_COLEGA.md`
- `UnificacaoDados/Relatorios/14_CAMERA_ZOOM_NOME_FRENTE_REFRESH_JHON.md`
- `UnificacaoDados/Relatorios/16_DASH_C_COLISAO_ANIMACAO_PLAYER.md`
- `UnificacaoDados/Relatorios/17_REFRESH_GABIRU_MENU_ESCADA_PULO_ANIMACOES.md`

## Decisao tecnica
- O multiplayer funcional continua vindo da base `ct-teste`.
- O menu do Gabiru foi ligado aos mesmos pontos consolidados:
  - `ClientRuntime`
  - `ConexaoPendente`
  - `DadosSessaoLocal`
  - `DescobridorHostLAN`
  - `RedeBootstrap`
  - `GerenciadorNomesRede`
  - `AnuncianteHostLAN`

## Player: idle e pulo
Arquivo principal:
- `Assets/Scripts/Gameplay/Movimentacao.cs`

Alteracoes:
- `forcaPulo`: `9` -> `12`.
- Novo controle separado de idle:
  - `fpsIdle = 6`
  - somente a animacao de idle fica mais lenta.
- Novo controle separado de pulo:
  - `fpsPulo = 32`
  - animacao de pulo nao fica mais em loop no ar.
  - o pulo passa rapidamente pelos frames iniciais e fica segurando o ultimo frame ate tocar o chao.
- Ao pousar, a animacao troca para idle/walk/run e reseta naturalmente.

Arquivo legado:
- `Assets/Scripts/Player.cs`

Alteracoes:
- `forcaPulo`: `10` -> `12`.
- `velocidadeAnimacaoIdle = 0.65`, aplicada somente quando parado no chao.
- As animacoes de correr, dash, ataque e pulo mantem a velocidade visual anterior.

Prefab:
- `Assets/Jogador.prefab`

Valores principais:
- `forcaPulo: 12`
- `fpsAnimacao: 20`
- `fpsIdle: 6`
- `fpsPulo: 32`
- `fpsAtaque: 24`
- `multiplicadorFpsDash: 2.5`

## Multiplayer na cena do Gabiru
Arquivo:
- `Assets/RecursosUnificados/GabiruScene/MenuPrincipalManager.cs`

Fluxo implementado:
- Botao `CRIAR`:
  - abre campo para nome do jogador,
  - confirma nome,
  - marca `ConexaoPendente.CriarHost = true`,
  - carrega `Unified_ColegaTeste_Scenes_CenaDoColega`.
- Botao `JOIN`:
  - abre lista de salas LAN detectadas por `DescobridorHostLAN`,
  - cada sala tem botao `Entrar`,
  - apos escolher sala, abre campo para nome,
  - confirma nome,
  - marca `ConexaoPendente.Ativa = true` com IP/porta da sala,
  - carrega `Unified_ColegaTeste_Scenes_CenaDoColega`.
- Botao central do painel multiplayer foi tratado como `Voltar`.

Cena:
- `Assets/Scenes/Unificadas/Unified_GabiruScene_Menu.unity`

Alteracao:
- Texto do botao central `voltar`: `Button` -> `Voltar`.

## Cena alvo multiplayer
Cena:
- `Assets/Scenes/Unificadas/Unified_ColegaTeste_Scenes_CenaDoColega.unity`

Alteracoes:
- Adicionado `Gerenciador da Rede` com:
  - `UnityTransport`
  - `NetworkManager`
  - `PlayerPrefab` apontando para `Assets/Jogador.prefab`
  - lista `DefaultNetworkPrefabs`
- Isso permite que o `ClientRuntime` processe `ConexaoPendente` depois do carregamento da cena.

## Spawn dos jogadores
Arquivo:
- `Assets/Scripts/Gameplay/Movimentacao.cs`

Regras:
- Em `Unified_ColegaTeste_Scenes_CenaDoColega`, player de rede real usa spawn por `OwnerClientId`.
- Host / jogador 1:
  - `spawnJogador1 = (0.52, 0.35, 0)`
- Cliente / jogador 2:
  - `spawnJogador2 = (137, 0.35, 0)`
- Esses valores ficaram serializados no `Assets/Jogador.prefab` para ajuste posterior no Inspector.

Protecao contra duplicidade:
- A cena unificada ja tinha um `Jogador` colocado manualmente para teste offline.
- Quando a cena roda online, esse jogador de cena e desativado visual/fisicamente se nao for o `PlayerObject` real da rede.
- Offline, ele continua funcionando normalmente para teste local.

## Build Settings
Arquivo:
- `ProjectSettings/EditorBuildSettings.asset`

Cenas adicionadas:
- `Assets/Scenes/Unificadas/Unified_GabiruScene_Menu.unity`
- `Assets/Scenes/Unificadas/Unified_ColegaTeste_Scenes_CenaDoColega.unity`

## Validacao
Comando executado:
- `dotnet build Assembly-CSharp.csproj -nologo -v minimal`

Resultado:
- 0 erros
- 0 warnings

## Resultado esperado no Unity
- Idle do jogador roda bem mais devagar.
- Correr, dash e demais animacoes continuam no ritmo rapido atual.
- Pulo fica mais alto.
- Animacao de pulo chega rapido ao ultimo frame e segura esse frame enquanto o jogador esta no ar.
- No pouso, o pulo reseta e volta para animacao de chao.
- Menu `Unified_GabiruScene_Menu` permite criar sala e entrar em sala LAN.
- Criar/entrar direciona para `Unified_ColegaTeste_Scenes_CenaDoColega`.
- Jogador 1 nasce no spawn configurado do host.
- Jogador 2 nasce no spawn configurado de cliente.
