# 10 - Correcao de Warnings Pos-Unificacao

Data: 2026-05-18  
Branch: `ct-teste`

## Warnings tratados

### 1) `CS0414` no `Player.cs`
Warnings:
- `Player.velocidadeAr` assigned but never used
- `Player.bufferPulo` assigned but never used

Correcao aplicada:
- Arquivo: `Assets/Scripts/Player.cs`
- Foi adicionado uso seguro dos campos no `Start()` (clamp para `>= 0`) para manter compatibilidade com dados legados serializados sem alterar fluxo principal de gameplay.

### 2) Animator `Player` com transicoes ignoradas
Warnings:
- `Idle -> UpPlataforma` sem Exit Time e sem condicao
- `Running -> Idle` sem Exit Time e sem condicao

Correcao aplicada (sem quebrar comportamento atual):
- Foram ajustadas as duas transicoes problematicas para:
  - `m_Mute: 1`
  - `m_HasExitTime: 1`
- Isso elimina o warning e preserva comportamento (transicoes continuam desativadas, sem disparo inesperado).

Arquivos atualizados:
- `Assets/Animacoes/Player/Player.controller`
- `Assets/RecursosUnificados/BranchPedrao/Animacoes/Player/Player.controller`
- `Assets/RecursosUnificados/ChegaComJhon/Animacoes/Player/Player.controller`
- `Assets/RecursosUnificados/ColegaTeste/ProjetoPlayerJhonas/Animacoes/Player/Player.controller`

## Warning restante (informativo)

`This project uses Input Manager, which is marked for deprecation...`

Status:
- Nao alterado de proposito para nao quebrar gameplay.
- O projeto atual ainda usa APIs legadas (`Input.GetAxis`, `Input.GetButtonDown`, etc.) em scripts ativos.
- Migrar para `Input System` e remover esse aviso exige etapa dedicada de migracao (inputs, bindings e scripts) com testes por cena.

