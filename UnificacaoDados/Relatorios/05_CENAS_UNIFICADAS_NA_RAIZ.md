# 05 - Cenas Unificadas na Pasta Assets/Scenes

Foram copiadas 16 cenas humanas para `Assets/Scenes/Unificadas` com nomes unicos. Cenas `_Recovery` e templates de `Settings/Scenes` ficaram fora da pasta principal por serem artefatos de editor.

| Scene | SizeKB |
| --- | --- |
| Unified_BranchPedrao_Scenes_bla.unity | 37.94 |
| Unified_BranchPedrao_Scenes_Cena do JHON.unity | 21.19 |
| Unified_BranchPedrao_Scenes_CenaDoCT.unity | 13.07 |
| Unified_BranchPedrao_Scenes_Ceta big Peter.unity | 37.94 |
| Unified_ChegaComJhon_Scenes_bla.unity | 32.13 |
| Unified_ChegaComJhon_Scenes_CenaDoCT.unity | 19.95 |
| Unified_ChegaComJhon_Scenes_ServerHub.unity | 29.09 |
| Unified_ColegaTeste_Decoracao_Background.unity | 5.86 |
| Unified_ColegaTeste_PlatformerSet1_Scene_Demo.unity | 12.07 |
| Unified_ColegaTeste_ProjetoPlayerJhonas_Scenes_bla.unity | 34.3 |
| Unified_ColegaTeste_ProjetoPlayerJhonas_Scenes_Cena do JHON.unity | 21.19 |
| Unified_ColegaTeste_ProjetoPlayerJhonas_Scenes_CenaDoCT.unity | 13.07 |
| Unified_ColegaTeste_Scenes_CenaDoColega.unity | 7313.25 |
| Unified_ColegaTeste_Scenes_CenaDoCT.unity | 56.31 |
| Unified_ColegaTeste_Scenes_Mapa_Base.unity | 5.87 |
| Unified_ColegaTeste_Scenes_SampleScene.unity | 3906.7 |

## Observacao sobre `.meta`
As cenas foram copiadas sem reutilizar `.meta` duplicado na pasta nova. Isso deixa a Unity gerar GUID novo para a cena, enquanto o conteudo interno continua apontando para os GUIDs dos assets referenciados.

## Arquivo bruto
- `UnificacaoDados/Relatorios/cenas_unificadas_manifest.json` contem origem e destino de cada cena copiada.
