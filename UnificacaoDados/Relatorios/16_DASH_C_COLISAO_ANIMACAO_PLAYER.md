# 16 - Dash no C, Animacao Mais Rapida e Colisao do Player

Data: 2026-05-23
Branch atual: `ct-teste`

## Contexto relido antes da alteracao
- `UNIFICACAO_CT_TESTE_ACOMPANHAMENTO.md`
- `README.md`
- `Assets/Scripts/README.md`
- READMEs em `ImportsBranches/*`
- Relatorios `01` ate `15` em `UnificacaoDados/Relatorios`

## Objetivo desta etapa
- Corrigir o dash para ser frontal, ativado pela tecla `C`.
- Remover o comportamento de dash para baixo por `S`/`Seta para baixo`.
- Manter a velocidade base do jogador igual.
- Acelerar a animacao visual para nao parecer lenta com o movimento atual.
- Melhorar colisao do player contra quinas/degraus.
- Manter recursos avancados de dash/sprites sendo usados quando existirem.

## Alteracoes aplicadas

### `Assets/Scripts/Gameplay/Movimentacao.cs`
- Dash agora usa `Keyboard.current.cKey`.
- `IniciarDash` agora calcula direcao para frente:
  - usa input horizontal quando o player esta segurando lado,
  - senao usa a direcao visual atual do sprite.
- Dash vertical foi removido do fluxo principal.
- Adicionado cache `framesDash` para usar sprites/frames com nome contendo `dash`.
- `fpsAnimacao` default: `10` -> `16`.
- `fpsAtaque` default: `14` -> `20`.
- Dash usa `multiplicadorFpsDash = 2.2`.
- `vel` nao foi alterado.
- Adicionada configuracao de colisao:
  - `CapsuleCollider2D` vertical em runtime,
  - material sem atrito,
  - `Rigidbody2D` com rotacao travada, interpolacao e colisao continua.
- Adicionada assistencia de degrau para reduzir travamento em quinas e permitir subir pequenos degraus sem pular.

### `Assets/Scripts/Player.cs`
- Dash legado tambem alinhado para tecla `C`.
- Dash legado agora sempre vai para frente, sem dash vertical.
- Animador legado recebe `anim.speed = 1.45`.
- Colisao legada tambem ganhou `CapsuleCollider2D`, material sem atrito e assistencia de degrau.

### `Assets/Jogador.prefab`
- Mantido `vel: 5`.
- Ajustado:
  - `fpsAnimacao: 16`
  - `fpsAtaque: 20`
  - `multiplicadorFpsDash: 2.2`
  - `forcaDash: 18`
  - `duracaoDash: 0.16`
  - parametros de colisao/degrau do player
  - `Rigidbody2D` com gravidade `3`, interpolacao, colisao continua e freeze rotation

## Validacao
Comando executado:
- `dotnet build Assembly-CSharp.csproj -nologo -v minimal`

Resultado:
- 0 erros
- 0 warnings

## Resultado esperado no Unity
- `C`: dash frontal com boost.
- `S` e `Seta para baixo`: nao disparam mais dash.
- Movimento horizontal continua com a mesma velocidade base.
- Animacao deve parecer mais compativel com o deslocamento.
- Player deve prender menos em quinas e subir pequenos degraus sem precisar pular.
