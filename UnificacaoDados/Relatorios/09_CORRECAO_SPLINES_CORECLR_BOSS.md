# 09 - Correcao de Splines/CoreCLR e Animator Boss

Data: 2026-05-18  
Branch: `ct-teste`

## Erros reportados
- `Failed to create CoreCLR, HRESULT: 0x8007000E`
- `TypeLoadException: Could not load type 'UnityEngine.Splines.SplineComponent' from assembly 'Unity.Splines'`
- `Controller 'Boss': Transition '' in state 'Idle' uses parameter 'Grito' which is not compatible with condition type.`

## Correcoes aplicadas

### 1) Animator Boss (erro de condicao invalida)
- Corrigido `m_ConditionMode` para transicoes com `m_ConditionEvent: Grito` que estavam com modo invalido para o tipo do parametro.
- Arquivos corrigidos:
  - `Assets/Animacoes/Boss/Boss.controller`
  - `Assets/RecursosUnificados/BranchPedrao/Animacoes/Boss/Boss.controller`
  - `Assets/RecursosUnificados/ColegaTeste/ProjetoPlayerJhonas/Animacoes/Boss/Boss.controller`

### 2) Pacotes Cinemachine/Splines (estabilidade em Unity 6)
- Atualizado:
  - `com.unity.cinemachine`: `3.1.4` -> `3.1.5`
- Fixado explicitamente:
  - `com.unity.splines`: `2.8.3`
- Arquivo:
  - `Packages/manifest.json`

### 3) Rebuild limpo de cache gerado (sem apagar assets do projeto)
- Removidos para forcar recompilacao limpa:
  - `Library/ScriptAssemblies`
  - `Library/Bee`
  - `Library/PackageCache/com.unity.cinemachine@...`
  - `Library/PackageCache/com.unity.splines@...`

## O que fazer no Unity agora
1. Fechar e abrir o projeto novamente.
2. Aguardar o Package Manager baixar/resolver e recompilar tudo (primeira abertura pode demorar).
3. Conferir se os 3 erros acima desapareceram no Console.
4. Se ainda houver erro de `SplineComponent`, enviar as primeiras 20 linhas do novo erro (com o caminho do asset/pacote citado) para patch direcionado.

