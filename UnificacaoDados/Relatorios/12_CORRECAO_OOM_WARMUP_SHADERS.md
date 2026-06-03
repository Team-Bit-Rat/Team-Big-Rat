# 12 - Correcao Rapida do Crash OOM no Startup (Warmup de Shaders)

Data: 2026-05-18  
Branch: `ct-teste`

## Sintoma
Ao abrir o projeto, Unity caiu com:
- `Could not allocate memory: System out of memory!`
- tentativa absurda de alocacao: `604018203685964070B`
- `MemoryLabel: TempOverflow`

## Diagnostico rapido (log real)
Fonte: `C:\Users\Marcos\AppData\Local\Unity\Editor\Editor.log`

Stack principal apontou para warmup de shader (nao gameplay/script):
- `keywords::LocalKeywordState::BuildIncompatibleKeywordSpaceMessage`
- `keywords::LocalKeywordState::ReportIncompatibleKeywordSpace`
- `ShaderVariantCollection::CompileShaders`
- `WarmupEditorShaders`

Interpretacao tecnica:
- Nao era falta real de RAM do PC.
- Era alocacao invalida gerada durante construcao de mensagem/estado de keywords de shader (conflito de shader keyword space), causando OOM fatal.

## Causa mais provavel no projeto
Havia copias extras de shaders do TextMesh Pro dentro de `Assets/RecursosUnificados` (BranchPedrao, ChegaComJhon e ColegaTeste), alem da copia oficial em `Assets/TextMesh Pro/Shaders`.

Essas copias extras (especialmente `shadergraph`) sao candidatas diretas ao conflito de keyword space no warmup.

## Correcao aplicada (segura e reversivel)
Foram **desativadas** (movidas para fora de `Assets`, sem apagar):
- `Assets/RecursosUnificados/BranchPedrao/TextMesh Pro/Shaders`
- `Assets/RecursosUnificados/ChegaComJhon/TextMesh Pro/Shaders`
- `Assets/RecursosUnificados/ColegaTeste/TextMesh Pro/Shaders`
- `Assets/RecursosUnificados/ColegaTeste/ProjetoPlayerJhonas/TextMesh Pro/Shaders`

Novo destino (backup):
- `UnificacaoDados/Desativados/ShaderWarmupRisco/...`

Manifesto da movimentacao:
- `UnificacaoDados/Relatorios/shader_warmup_desativados.csv`

## Verificacao pos-correcao
- `Assets/RecursosUnificados` ficou com **0** arquivos `.shader/.shadergraph`.
- No projeto (`Assets`) restou somente a copia principal oficial de TMP shaders.
- Build C# ok: `0 erros / 0 warnings`.

## Conclusao
A origem do crash foi isolada no warmup de shaders e mitigada sem perda de recurso (apenas desativacao com backup).

## Proximo passo recomendado
1. Reabrir o Unity.
2. Confirmar que o projeto abre sem o crash OOM.
3. Se abrir, manter essa desativacao dos shaders duplicados.
4. Se ainda ocorrer, enviar as primeiras 60 linhas do novo `Editor.log` para patch direcionado final.
