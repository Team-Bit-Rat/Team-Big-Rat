# 19 - Refresh Gabiru Visual Final + Cenas Unificadas

Data: 2026-06-03
Branch atual: `ct-teste`

## Contexto relido antes da alteracao
Foram lidos todos os arquivos `.md` do projeto localizados por `rg --files -g "*.md"`, incluindo:
- `UNIFICACAO_CT_TESTE_ACOMPANHAMENTO.md`
- `README.md`
- `Assets/Scripts/README.md`
- READMEs em `ImportsBranches/*`
- todos os relatorios em `UnificacaoDados/Relatorios`
- READMEs em `UnificacaoDados/Temp/*`

## Sync remoto
Comando executado:
- `git fetch --all --prune`

Resultado:
- `origin/Gabiru-scene` avancou de `3ef6186` para `c8011f9`.
- Commit usado como fonte:
  - `c8011f926faafe41995d0cc597961ba213f7535b`
  - data: `2026-05-27 14:58:29 -0300`
  - mensagem: `versao visual final do Menu`

## Refresh do espelho
- `ImportsBranches/Gabiru-scene` foi recriada a partir de `origin/Gabiru-scene`.
- Total no espelho apos refresh: 2220 arquivos.
- A primeira tentativa com pipe `git archive | tar` falhou por limitacao do PowerShell/tar; o refresh foi refeito com `git archive --format=zip` + `Expand-Archive`.

## Cenas novas em `Assets/Scenes/Unificadas`
As cenas humanas da branch Gabiru foram copiadas com prefixo novo `Unified_GabiruSceneRefresh_`, sem sobrescrever a cena anterior `Unified_GabiruScene_Menu`.

Cenas adicionadas:
- `Assets/Scenes/Unificadas/Unified_GabiruSceneRefresh_Decoracao_Background.unity`
- `Assets/Scenes/Unificadas/Unified_GabiruSceneRefresh_Menu_VisualFinal.unity`
- `Assets/Scenes/Unificadas/Unified_GabiruSceneRefresh_PlatformerSet1_Scene_Demo.unity`
- `Assets/Scenes/Unificadas/Unified_GabiruSceneRefresh_ProjetoPlayerJhonas_Scenes_CenaDoCT.unity`
- `Assets/Scenes/Unificadas/Unified_GabiruSceneRefresh_ProjetoPlayerJhonas_Scenes_bla.unity`
- `Assets/Scenes/Unificadas/Unified_GabiruSceneRefresh_Scenes_CenaDoCT.unity`
- `Assets/Scenes/Unificadas/Unified_GabiruSceneRefresh_Scenes_CenaDoColega.unity`
- `Assets/Scenes/Unificadas/Unified_GabiruSceneRefresh_Scenes_Mapa_Base.unity`
- `Assets/Scenes/Unificadas/Unified_GabiruSceneRefresh_Scenes_SampleScene.unity`

Ficaram fora de `Unificadas`:
- cenas `_Recovery`
- templates de `Settings/Scenes`

Motivo:
- os relatorios anteriores tratam esses caminhos como artefatos de editor/template, nao como cenas humanas principais.

## Recursos adicionados
Novas imagens do menu/HUD copiadas com `.meta` preservado para resolver GUIDs da cena:
- `Assets/RecursosUnificados/GabiruScene/hud-sprites/CAEDUS (2).png`
- `Assets/RecursosUnificados/GabiruScene/hud-sprites/JOGAR (2) 1.png`
- `Assets/RecursosUnificados/GabiruScene/hud-sprites/SAIR 1.png`
- `Assets/RecursosUnificados/GabiruScene/hud-sprites/config 1.png`
- `Assets/RecursosUnificados/GabiruScene/hud-sprites/config-removebg-preview.png`
- `Assets/RecursosUnificados/GabiruScene/hud-sprites/mainmenu.png`
- `Assets/RecursosUnificados/GabiruScene/hud-sprites/multihud.png`

Recursos de animacao/HUD adicionados:
- `Assets/RecursosUnificados/GabiruScene/Animacoes-HUD/Dano-no-olho.anim`
- `Assets/RecursosUnificados/GabiruScene/Animacoes-HUD/Dano.anim`
- `Assets/RecursosUnificados/GabiruScene/Animacoes-HUD/HUD.controller`
- `Assets/RecursosUnificados/GabiruScene/Animacoes-HUD/ZOI.controller`

## Ajustes de seguranca
- A cena `Unified_GabiruSceneRefresh_Menu_VisualFinal` teve o GUID antigo do `InputSystem_Actions.inputactions` da branch Gabiru substituido pelo GUID consolidado do projeto atual:
  - de `ca9f5fa95ffab41fb9a615ab714db018`
  - para `2bcd2660ca9b64942af0de543d8d7100`
- Nenhum script duplicado da branch Gabiru foi trazido para compilacao.
- As cenas novas receberam `.meta` com GUID novo para evitar conflito com `Unified_GabiruScene_Menu`.
- `ProjectSettings/EditorBuildSettings.asset` recebeu a nova cena:
  - `Assets/Scenes/Unificadas/Unified_GabiruSceneRefresh_Menu_VisualFinal.unity`

## Validacao
Comandos/checagens executados:
- assets reais da branch referenciados pelas cenas refresh e ausentes em `Assets`: `0`
- grupos de GUID duplicado em `.meta` dentro de `Assets`: `0`
- `dotnet build Assembly-CSharp.csproj -nologo -v minimal`

Resultado do build:
- 0 erros
- 0 warnings

## Resultado esperado no Unity
- `Unified_GabiruSceneRefresh_Menu_VisualFinal` abre com o visual final do menu Gabiru.
- As novas imagens de menu/HUD devem estar resolvidas.
- A cena antiga `Unified_GabiruScene_Menu` permanece preservada.
- As cenas refresh ficam disponiveis para comparacao/uso em `Assets/Scenes/Unificadas`.
