# Acompanhamento de Unificacao - CT-TESTE

Data: 2026-05-18
Branch atual de destino: `ct-teste`

## Objetivo executado
Unificar recursos das branches `Chega-com-jhon`, `Colega-teste` e `branch-pedrão` sem quebrar o multiplayer funcional da `ct-teste`.

## Estrategia aplicada
- Todos os recursos das branches foram clonados para pastas separadas em `ImportsBranches/`.
- Estrutura criada:
  - `ImportsBranches/Chega-com-jhon`
  - `ImportsBranches/Colega-teste`
  - `ImportsBranches/branch-pedrao`
- Arquivos de configuracao/sistema da Unity foram excluidos da clonagem (`ProjectSettings`, `Packages`, `Library`, `Temp`, `Logs`, `UserSettings`, etc.).
- Com isso, arquivos com mesmo nome coexistem sem sobrescrever seu projeto base.

## Inventario importado
- `Chega-com-jhon`: 1057 arquivos
- `Colega-teste`: 2128 arquivos
- `branch-pedrao`: 956 arquivos

## Commits mais recentes por branch (referencia de atualizacao)
- `Chega-com-jhon` -> `df5ba3a` -> 2026-05-14 09:32:30 -0300 -> "IA do boss imcompleta"
- `Colega-teste` -> `d3d7c6c` -> 2026-05-14 21:40:30 -0300 -> "atualização do segundo mapa"
- `branch-pedrão` -> `33f010e` -> 2026-05-11 19:39:14 -0300 -> "Init commit"
- `ct-teste` -> `bfb8b92` -> 2026-05-05 09:52:09 -0300 -> "VERSÃO MULTIPLAYER FUNCIONAL COM PLAYER"

Criterio de versao permanente adotado:
- Recursos equivalentes entre branches devem priorizar o mais novo por data de commit.
- Neste recorte, `Colega-teste` e `Chega-com-jhon` sao mais novos que `branch-pedrão` para os principais blocos de mapa/inimigos.

## Cena "bla" localizada
- `ImportsBranches/Chega-com-jhon/Assets/Scenes/bla.unity`
- `ImportsBranches/Colega-teste/Assets/ProjetoPlayerJhonas/Scenes/bla.unity`
- `ImportsBranches/branch-pedrao/Assets/Scenes/bla.unity`

Historico (ultimo commit por arquivo `bla.unity`):
- Chega-com-jhon: `df5ba3a` em 2026-05-14 09:32:30 -0300
- Colega-teste (ProjetoPlayerJhonas): `d3d7c6c` em 2026-05-14 21:40:30 -0300
- branch-pedrão: `207d54a` em 2026-04-29 23:16:26 -0300

## Recursos relevantes encontrados (prefab / IA / NPC / inimigos / boss / multiplayer)
### Multiplayer
- `ImportsBranches/Chega-com-jhon/Assets/DefaultNetworkPrefabs.asset`
- `ImportsBranches/Chega-com-jhon/Assets/Scripts/Rede/AnuncianteHostLAN.cs`
- `ImportsBranches/Chega-com-jhon/Assets/Scripts/Rede/ClientNetworkAnimator.cs`
- `ImportsBranches/Chega-com-jhon/Assets/Scripts/Rede/ClientNetworkTransform.cs`
- `ImportsBranches/Chega-com-jhon/Assets/Scripts/Rede/GerenciadorNomesRede.cs`
- `ImportsBranches/Chega-com-jhon/Assets/Sprites/Interface/Multiplayer/*`

### IA / inimigos / boss
- `ImportsBranches/Chega-com-jhon/Assets/Scripts/IA?s/IA Do Boss/SerberusAI.cs`
- `ImportsBranches/branch-pedrao/Assets/Scripts/Enemy.cs`
- `ImportsBranches/Chega-com-jhon/Assets/Animacoes/Boss/*`
- `ImportsBranches/Chega-com-jhon/Assets/Sprites/Objetos/Boss/*`
- `ImportsBranches/Colega-teste/Assets/ProjetoPlayerJhonas/Animacoes/Boss/*`
- `ImportsBranches/Colega-teste/Assets/ProjetoPlayerJhonas/Sprites/Boss/*`

### Prefabs
- `ImportsBranches/Chega-com-jhon/Assets/Jogador.prefab`
- `ImportsBranches/Colega-teste/Assets/ProjetoTesteAssets/TileMap.prefab`
- `ImportsBranches/Colega-teste/Assets/PlatformerSet1/Map Assets/Decoration/Decoration.prefab`
- `ImportsBranches/Colega-teste/Assets/PlatformerSet1/Map Assets/TilepaletteLevel1/New Tile Palette.prefab`
- `ImportsBranches/Colega-teste/Assets/PlatformerSet1/Map Assets/TileplateLevel1Background/Level1Background.prefab`

## Recapitulacao dos arquivos .md lidos
Arquivos lidos:
- `README.md` (raiz)
- `Assets/Scripts/README.md`
- `ImportsBranches/Chega-com-jhon/README.md`
- `ImportsBranches/Chega-com-jhon/Assets/Scripts/README.md`
- `ImportsBranches/Colega-teste/README.md`
- `ImportsBranches/branch-pedrao/README.md`

Resumo:
- Todos os `README.md` de raiz descrevem o repositorio do projeto Team Big Rat.
- `Assets/Scripts/README.md` (tambem presente em Chega-com-jhon) mapeia arquitetura de rede/multiplayer, gameplay, core e scripts Netcode.

## Resultado final
- Unificacao por coexistencia concluida na branch `ct-teste`.
- Todos os recursos solicitados das outras branches agora estao dentro do seu projeto atual, organizados por origem e sem conflito de sobrescrita.
- A base multiplayer funcional da `ct-teste` foi preservada.

## Atualizacao complementar (2026-05-23)
- `git fetch --all --prune` executado com sucesso e sincronizado com o remoto.
- Nova branch remota detectada e importada: `origin/Gabiru-scene` (`3ef6186`).
- Refresh completo de `ImportsBranches/Chega-com-jhon` a partir de `origin/Chega-com-jhon` (`df5ba3a`).
- Nova pasta espelho criada: `ImportsBranches/Gabiru-scene`.
- Cena/menu do Gabiru adicionados para unificacao sem sobrescrever a base principal:
  - `Assets/Scenes/Unificadas/Unified_GabiruScene_Menu.unity`
  - `Assets/RecursosUnificados/GabiruScene/*`
- Validado que o dash principal permanece no padrao novo (`S`/`Seta para baixo`) em:
  - `Assets/Scripts/Gameplay/Movimentacao.cs`
  - `Assets/Scripts/Player.cs`

## Atualizacao complementar (2026-05-24)
- Relatorio detalhado criado:
  - `UnificacaoDados/Relatorios/18_IDLE_PULO_MENU_GABIRU_MULTIPLAYER.md`
- Player:
  - idle separado e mais lento (`fpsIdle = 6`),
  - pulo mais alto (`forcaPulo = 12`),
  - animacao de pulo mais rapida (`fpsPulo = 32`),
  - pulo sem loop no ar, segurando ultimo frame ate pousar.
- Menu Gabiru:
  - `Unified_GabiruScene_Menu` agora liga `CRIAR` e `JOIN` ao fluxo multiplayer consolidado da `ct-teste`.
  - Criar sala abre campo de nome e carrega `Unified_ColegaTeste_Scenes_CenaDoColega` como host.
  - Entrar mostra lista LAN, botao `Entrar`, campo de nome e carrega a mesma cena como cliente.
  - Texto do botao central corrigido para `Voltar`.
- Cena alvo:
  - `Unified_ColegaTeste_Scenes_CenaDoColega` recebeu `NetworkManager` + `UnityTransport`.
  - Build Settings agora inclui `Unified_GabiruScene_Menu` e `Unified_ColegaTeste_Scenes_CenaDoColega`.
  - Spawn multiplayer configurado no `Jogador.prefab`: jogador 1 em `(0.52, 0.35, 0)` e jogador 2 em `(137, 0.35, 0)`.
- Validacao:
  - `dotnet build Assembly-CSharp.csproj -nologo -v minimal`
  - resultado: 0 erros, 0 warnings.

## Atualizacao complementar (2026-06-03)
- Todos os `.md` do projeto foram relidos antes da alteracao.
- `git fetch --all --prune` executado; `origin/Gabiru-scene` avancou para `c8011f9`.
- `ImportsBranches/Gabiru-scene` recriada a partir de `origin/Gabiru-scene` (`c8011f9`, "versao visual final do Menu").
- Relatorio detalhado criado:
  - `UnificacaoDados/Relatorios/19_REFRESH_GABIRU_VISUAL_FINAL_CENAS_UNIFICADAS.md`
- Cenas humanas da Gabiru foram copiadas novamente para `Assets/Scenes/Unificadas` com prefixo novo `Unified_GabiruSceneRefresh_*`, sem sobrescrever `Unified_GabiruScene_Menu`.
- Cena principal nova adicionada:
  - `Assets/Scenes/Unificadas/Unified_GabiruSceneRefresh_Menu_VisualFinal.unity`
- Novos recursos visuais do menu/HUD adicionados em:
  - `Assets/RecursosUnificados/GabiruScene/hud-sprites/*`
  - `Assets/RecursosUnificados/GabiruScene/Animacoes-HUD/*`
- `Unified_GabiruSceneRefresh_Menu_VisualFinal` foi adicionada ao Build Settings.
- Validacao:
  - assets reais da branch referenciados pelas cenas refresh e ausentes em `Assets`: 0
  - grupos de GUID duplicado em `.meta` dentro de `Assets`: 0
  - `dotnet build Assembly-CSharp.csproj -nologo -v minimal`
  - resultado: 0 erros, 0 warnings.
