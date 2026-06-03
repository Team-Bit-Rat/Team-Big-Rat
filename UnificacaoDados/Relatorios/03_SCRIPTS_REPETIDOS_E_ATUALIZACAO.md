# 03 - Scripts Repetidos e Atualizacao Segura

Data: 2026-05-18

## Protecao anti-quebra aplicada
Para evitar erro de classes duplicadas na Unity, os scripts importados das branches foram movidos para snapshots fora de `Assets`:
- `UnificacaoDados/ScriptsSnapshot/...`

Assim:
- Todos os scripts foram preservados para leitura/merge.
- A Unity nao compila classes duplicadas dessas branches dentro da ct-teste.

## Scripts repetidos identificados
1. `Movimentacao.cs`
- BranchPedrao: hash `61F1019F...`
- ChegaComJhon: hash `9EFA3D39...`
- ColegaTeste: hash `61F1019F...`

2. `Player.cs`
- BranchPedrao: hash `E9FF6A1F...`
- ChegaComJhon: hash `233CC6B3...`
- ColegaTeste: hash `70451A63...`

## Referencia de atualizacao por branch (scripts)
- Chega-com-jhon: 2026-05-14 09:32:30 -0300
- branch-pedrão: 2026-05-11 19:39:14 -0300
- Colega-teste (ultimo commit em Assets/Scripts): 2026-04-24 23:46:53 -0300

## Regra recomendada para consolidacao futura
- Usar versao mais recente como base.
- Reaplicar diferencas funcionais das outras versoes manualmente.
- Testar cena por cena apos cada merge de script.
