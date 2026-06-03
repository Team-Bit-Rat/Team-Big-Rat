# 02 - Conflitos e Renomeacoes

Data: 2026-05-18

## Estrategia de conflito
- Em vez de sobrescrever arquivos iguais no mesmo caminho, foi aplicado isolamento por origem:
  - `Assets/RecursosUnificados/ChegaComJhon/...`
  - `Assets/RecursosUnificados/ColegaTeste/...`
  - `Assets/RecursosUnificados/BranchPedrao/...`
- Resultado: coexistencia total, sem perda de versoes.

## Caso citado: cena `bla`
- Nao houve conflito destrutivo porque as cenas ficaram em pastas separadas por branch.
- As 3 versoes de `bla` foram mantidas.

## Por que nao sobrescrever automatico
- Sobrescrita cega poderia quebrar referencias de GUID/meta e comportamento em runtime.
- Isolamento por origem preserva 100% do historico e permite consolidacao assistida depois.
