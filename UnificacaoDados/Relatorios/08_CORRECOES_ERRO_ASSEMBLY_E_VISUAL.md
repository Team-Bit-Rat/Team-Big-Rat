# 08 - Correcao de Assembly Duplicada e Diagnostico Visual

Data: 2026-05-18  
Branch: `ct-teste`

## Problemas reportados
- Erro de compilacao:
  - `Assembly with name 'ParrelSync' already exists`
- Recursos visuais aparecendo bugados/invisiveis em parte das cenas unificadas.

## Correcao aplicada agora

### 1) Assembly duplicada (ParrelSync)
- Validacao atual de `.asmdef` duplicadas em `Assets`: **0 duplicatas**.
- Copia duplicada do ParrelSync ja estava fora de `Assets`, em:
  - `UnificacaoDados/Desativados/ParrelSync_duplicado_ChegaComJhon`

### 2) Scripts ausentes em cenas
- Foi restaurado script faltante `Combo` com GUID original esperado pelas cenas:
  - Arquivo: `Assets/Scripts/Legacy/BranchPedrao/Combo.cs`
  - Meta GUID: `a8b9fa25681ef5848b6b2f405266d62f`
- Referencias de `Player` antigas foram remapeadas para o `Player.cs` atual:
  - GUID antigo: `ee606aa30df13714aaec7d0000ed156a`
  - GUID atual: `e7525a2a68786ee4f9b28a7703b827a9`
- Arquivos de cena atualizados:
  - `Assets/Scenes/Unificadas/Unified_BranchPedrao_Scenes_Cena do JHON.unity`
  - `Assets/Scenes/Unificadas/Unified_ColegaTeste_ProjetoPlayerJhonas_Scenes_Cena do JHON.unity`
  - `Assets/RecursosUnificados/BranchPedrao/Scenes/Cena do JHON.unity`
  - `Assets/RecursosUnificados/ColegaTeste/ProjetoPlayerJhonas/Scenes/Cena do JHON.unity`

### 3) Compatibilidade de serializacao no Player
- Ajustado `Assets/Scripts/Player.cs` para preservar dados antigos vindos das cenas:
  - `FormerlySerializedAs("groundCheckRadius")` -> `groundRadius`
  - `FormerlySerializedAs("spriteRenderer")` -> `sprite`
  - `FormerlySerializedAs("animator")` -> `anim`
- Campos antigos tambem preservados para nao perder valor serializado:
  - `velocidadeAr`
  - `bufferPulo`

### 4) Cinemachine faltando (causa principal de camera/visual em 3 cenas)
- GUIDs ausentes restantes em `Main Camera`/`CinemachineCamera`:
  - `72ece51f2901e7445ab60da3685d6b5f` (`CinemachineBrain`)
  - `886251e9a18ece04ea8e61686c173e1b` (`CinemachinePositionComposer`)
  - `f9dfa5b682dcd46bda6128250e975f58` (`CinemachineCamera`)
- Dependencia adicionada em `Packages/manifest.json`:
  - `"com.unity.cinemachine": "3.1.4"`

## Auditoria tecnica (pos-correcao)
- Imagens importadas auditadas: **1139**
- Imagens sem `.meta`: **0**
- Imagens com assinatura binaria invalida: **0**
- GUIDs faltantes reais (total): **11**
  - 3 sao Cinemachine (pendente resolver via Package Manager)
  - 2 sao GUID internos builtin do Unity (`0000...e000`, `0000...f000`)
  - 6 sao referencias de debug do URP Renderer2D (nao bloqueantes para gameplay)

Arquivos de apoio gerados/atualizados:
- `UnificacaoDados/Relatorios/guids_faltantes_reais.csv`
- `UnificacaoDados/Relatorios/missing_scripts_context.csv`
- `UnificacaoDados/Relatorios/imported_images_meta_audit.csv`
- `UnificacaoDados/Relatorios/imagens_assinatura_invalida.csv`

## O que validar no Unity agora
1. Reabrir o projeto para Unity resolver a nova dependencia `com.unity.cinemachine`.
2. Abrir as cenas:
   - `Assets/Scenes/Unificadas/Unified_ChegaComJhon_Scenes_bla.unity`
   - `Assets/Scenes/Unificadas/Unified_ColegaTeste_Scenes_SampleScene.unity`
   - `Assets/Scenes/Unificadas/Unified_ColegaTeste_Scenes_CenaDoColega.unity`
3. Confirmar no Console que sumiram erros de `Missing Script` relacionados a Cinemachine e ParrelSync.
4. Se ainda houver invisibilidade em objetos especificos, validar material do SpriteRenderer:
   - `Sprite-Lit-Default` (com luz 2D), ou
   - `Sprite-Unlit-Default` (sem dependencia de luz 2D).

