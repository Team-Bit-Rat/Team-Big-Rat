# 20 - Animator do Jogador em ColegaMapa / GabsMenu

Data: 2026-06-04

## Pedido
Garantir que as cenas ativas `GabsMenu` e `ColegaMapa`, dentro de `Assets/Scenes/Unificadas`, usem o `Jogador.prefab` com animacao conduzida pelo Animator existente do player, sem alterar o `Player.controller` e sem misturar recursos de backup/importacao.

## Documentacao e estrutura verificadas
- `.md` lidos/reconsultados: `README.md`, `Assets/Scripts/README.md`, `UNIFICACAO_CT_TESTE_ACOMPANHAMENTO.md`, relatorios `01` a `19` em `UnificacaoDados/Relatorios`, READMEs de `ImportsBranches` e READMEs de recursos unificados relevantes.
- Pastas ativas principais em `Assets`: `Animacoes`, `Assets`, `ParrelSync`, `Plugins`, `RecursosUnificados`, `Resources`, `Scenes`, `Scripts`, `Settings`, `Sprites`, `TextMesh Pro`, `_Recovery`.
- Pastas de unificacao/backups identificadas: `Assets/RecursosUnificados/*`, `Assets/Scenes/Unificadas`, `ImportsBranches/*`, `UnificacaoDados/*`, `Assets/_Recovery`.
- Recursos ativos usados para esta alteracao:
  - `Assets/Jogador.prefab`
  - `Assets/Scripts/Gameplay/Movimentacao.cs`
  - `Assets/Animacoes/Player/Player.controller`
  - `Assets/Scenes/Unificadas/GabsMenu.unity`
  - `Assets/Scenes/Unificadas/ColegaMapa.unity`

## Resultado aplicado
- `Assets/Jogador.prefab` agora possui componente `Animator` apontando para `Assets/Animacoes/Player/Player.controller`.
- O controller, clips e sprites do Animator nao foram alterados.
- `Movimentacao.cs` preserva movimento, spawn, nome, dash, ataque e rede.
- Quando o prefab tem Animator, `Movimentacao.cs` passa a dirigir os parametros/triggers do Animator:
  - bool/float: `Andando`, `Correndo`, `Pulando`, `Queda`, `NoChao`, `VelocidadeY`
  - triggers: `Dash`, `Ataque`, `Ataque2`, `Ataque3`
- A animacao por troca manual de sprites continua no script apenas como fallback para objetos antigos sem Animator/controller.
- A sincronizacao multiplayer continua via `NetworkVariable`s de estado do dono:
  - velocidade X/Y, corrida, dash, chao, flip e etapa de ataque
  - um pulso de dash foi adicionado para replicar o trigger `Dash` nos proxies
  - ataques remotos disparam o trigger correto quando `etapaAtaqueRede` muda

## Ligacao das cenas
- `ProjectSettings/EditorBuildSettings.asset` contem:
  - `Assets/Scenes/Unificadas/GabsMenu.unity`
  - `Assets/Scenes/Unificadas/ColegaMapa.unity`
- `GabsMenu.unity` aponta `cenaJogo: ColegaMapa`.
- `ColegaMapa.unity` usa o `PlayerPrefab` com GUID de `Assets/Jogador.prefab` (`540df61514a8e17478f2cea6be2cb29a`).

## Validacao
Comando executado:

```powershell
dotnet build Assembly-CSharp.csproj -nologo -v minimal
```

Resultado:
- Compilacao com exito.
- 0 erros.
- 0 avisos.

## Observacao para proximas IAs
Nao substituir o `Player.controller` ativo nem copiar controller/sprites de `ImportsBranches`, `RecursosUnificados` ou `_Recovery` sem novo pedido explicito. Para o fluxo atual, `GabsMenu` -> `ColegaMapa` -> `Assets/Jogador.prefab` e o Animator ativo e `Assets/Animacoes/Player/Player.controller`.
