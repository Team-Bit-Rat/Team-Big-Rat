# 17 - Refresh Gabiru Menu + Escada, Pulo e Animacoes

Data: 2026-05-23
Branch atual: `ct-teste`

## Contexto relido antes da alteracao
- `UNIFICACAO_CT_TESTE_ACOMPANHAMENTO.md`
- `UnificacaoDados/Relatorios/16_DASH_C_COLISAO_ANIMACAO_PLAYER.md`
- Relatorios anteriores de unificacao envolvendo Gabiru, movimento, dash, colisao e cena.

## Refresh da branch Gabiru
Fonte atualizada:
- `FETCH_HEAD`: `3ef6186` (2026-05-19 20:24:17 -0300)
- Mensagem: `Menu e Hud com animações funcionais (sem os scripts)`

Resultado:
- `ImportsBranches/Gabiru-scene` recriada a partir do remoto.
- Total no espelho: 2206 arquivos.
- A pasta continua fora de `Assets`, servindo como referencia completa de comparacao sem compilar scripts duplicados.

## Menu do Gabiru
Arquivos/recursos reforcados:
- `Assets/Scenes/Unificadas/Unified_GabiruScene_Menu.unity`
- `Assets/RecursosUnificados/GabiruScene/MenuPrincipalManager.cs`
- `Assets/RecursosUnificados/GabiruScene/hud-sprites/*`

Validacao de dependencias da cena `Menu.unity`:
- Artes principais localizadas e copiadas com `.meta`:
  - `CAEDUS.png`
  - `JUSTICE.png`
  - `JOGAR (2).png`
  - `JOIN.png`
  - `CRIAR.png`
  - `MULTIPLAYER-DEF.jpeg`
  - `SAIR.png`
  - `CONFIG.png`
- A cena unificada foi ajustada para usar o `InputSystem_Actions.inputactions` atual do projeto (`guid: 2bcd2660ca9b64942af0de543d8d7100`).
- Scripts conflitantes de gameplay da Gabiru nao foram trazidos para compilacao em `Assets`.

## Player: escada/degrau parado
Problema observado:
- Em escada/degrau, parado, o player podia continuar deslizando lentamente ate sair da escada.

Correcao aplicada:
- Quando nao ha input horizontal e o player esta no chao, o controlador zera o deslizamento horizontal.
- Pequenos valores verticais residuais tambem sao zerados para impedir escorregamento parado.
- A assistencia de degrau continua ativa somente quando existe input horizontal real.

## Animacoes
Sem mudar a velocidade base do movimento:
- `fpsAnimacao`: 16 -> 20.
- `fpsAtaque`: 20 -> 24.
- `multiplicadorFpsDash`: 2.2 -> 2.5.
- `Player.cs` legado: `anim.speed`: 1.45 -> 1.65.

## Pulo
Melhorias aplicadas no controlador principal:
- `tempoCoyote = 0.12`
- `bufferPulo = 0.12`
- corte de pulo ao soltar tecla (`multiplicadorCortePulo = 0.55`)
- gravidade de queda um pouco maior (`multiplicadorGravidadeQueda = 1.35`)

Objetivo:
- pulo mais responsivo,
- melhor tolerancia quando aperta perto da borda/plataforma,
- queda menos flutuante.

## Arquivos alterados
- `Assets/Scripts/Gameplay/Movimentacao.cs`
- `Assets/Scripts/Player.cs`
- `Assets/Jogador.prefab`
- `Assets/Scenes/Unificadas/Unified_GabiruScene_Menu.unity`
- `Assets/RecursosUnificados/GabiruScene/hud-sprites/*`

## Validacao
Comando executado:
- `dotnet build Assembly-CSharp.csproj -nologo -v minimal`

Resultado:
- 0 erros
- 0 warnings

## Resultado esperado no Unity
- Menu do Gabiru deve abrir com artes/imagens principais resolvidas.
- Player parado em escada/degrau nao deve continuar andando sozinho.
- Subida de pequenos degraus deve continuar suave com input.
- Animacoes de correr, dash e pulo devem parecer mais rapidas.
- Pulo deve responder melhor perto de bordas e ao apertar um pouco antes de tocar o chao.
