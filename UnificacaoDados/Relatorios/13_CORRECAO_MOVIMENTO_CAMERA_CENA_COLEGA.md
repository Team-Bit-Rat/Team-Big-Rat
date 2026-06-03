# 13 - Correcao de Controle Local do Jogador na Cena Unificada

Data: 2026-05-18
Branch: `ct-teste`
Cena foco: `Assets/Scenes/Unificadas/Unified_ColegaTeste_Scenes_CenaDoColega.unity`

## Contexto relido antes da correcao
Foram relidos os arquivos de acompanhamento criados em 2026-05-18:
- `UNIFICACAO_CT_TESTE_ACOMPANHAMENTO.md`
- `UnificacaoDados/Relatorios/01_INVENTARIO_GERAL.md`
- `UnificacaoDados/Relatorios/02_CONFLITOS_E_RENOMEACOES.md`
- `UnificacaoDados/Relatorios/03_SCRIPTS_REPETIDOS_E_ATUALIZACAO.md`
- `UnificacaoDados/Relatorios/04_VISTORIA_COMPLETA_ASSETS.md`
- `UnificacaoDados/Relatorios/05_CENAS_UNIFICADAS_NA_RAIZ.md`
- `UnificacaoDados/Relatorios/06_ANALISE_FUNCIONALIDADES_E_SCRIPTS.md`
- `UnificacaoDados/Relatorios/07_CHECKLIST_UNITY.md`
- `UnificacaoDados/Relatorios/08_CORRECOES_ERRO_ASSEMBLY_E_VISUAL.md`
- `UnificacaoDados/Relatorios/09_CORRECAO_SPLINES_CORECLR_BOSS.md`
- `UnificacaoDados/Relatorios/10_CORRECAO_WARNINGS_POS_UNIFICACAO.md`
- `UnificacaoDados/Relatorios/11_AVALIACAO_FINAL_CONTINUACAO_UNIFICACAO.md`
- `UnificacaoDados/Relatorios/12_CORRECAO_OOM_WARMUP_SHADERS.md`

## Causa raiz encontrada para "jogador parado"
No script central `Assets/Scripts/Gameplay/Movimentacao.cs` havia bloqueio total de input e fisica local fora de spawn de rede:
- `Update()` retornava em `if (!IsSpawned || !IsOwner) return;`
- `FixedUpdate()` retornava quando `!IsSpawned`

Em cena unificada offline (sem ciclo de spawn de Netcode), `IsSpawned == false`, portanto:
- jogador nao lia teclado/mouse,
- jogador nao processava pulo/dash/ataque,
- movimento ficava travado.

## Correcao aplicada (segura para multiplayer)
Arquivo alterado: `Assets/Scripts/Gameplay/Movimentacao.cs`

1. Foi criado o helper:
- `bool PodeControlarLocalmente() => !IsSpawned || IsOwner;`

2. `Update()` agora usa esse helper para permitir controle local em cena offline.

3. `FixedUpdate()` agora usa esse helper para permitir simulacao local em cena offline.

4. `AtualizarJanelaHitboxAtaque()` tambem usa esse helper, habilitando hitbox em offline sem afetar remoto.

Importante:
- sincronizacao de `NetworkVariable` continua protegida por `IsSpawned && IsOwner`.
- logica de proxies remotos continua intacta.
- em multiplayer, apenas dono continua controlando o proprio jogador.

## Correcao de camera na cena foco
Arquivo alterado: `Assets/Scenes/Unificadas/Unified_ColegaTeste_Scenes_CenaDoColega.unity`

No objeto `CinemachineCamera`, o campo estava vazio:
- `Target.TrackingTarget: {fileID: 0}`

Foi vinculado para o `Transform` do prefab do jogador instanciado na cena:
- `TrackingTarget: {fileID: 1213361198309640248, guid: 540df61514a8e17478f2cea6be2cb29a, type: 3}`

Com isso, a camera passa a acompanhar o jogador dessa cena.

## Resultado esperado
Na `Unified_ColegaTeste_Scenes_CenaDoColega`:
- mover com setas/A-D,
- pular com espaco/W/Seta para cima,
- dash com Alt (incluindo dash para baixo com S/Seta para baixo),
- ataque com clique esquerdo,
- camera seguindo o jogador.
