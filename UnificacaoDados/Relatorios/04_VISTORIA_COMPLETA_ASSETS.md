# 04 - Vistoria Completa de Assets e Configuracoes

Data: 2026-05-18
Branch alvo: `ct-teste`

## Resumo por origem
| Branch | TotalFiles | UnityScenes | HumanScenes | Prefabs | Animations | Controllers | Images | Assets | TileAssets | SizeMB |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ChegaComJhon | 989 | 10 | 3 | 1 | 30 | 3 | 356 | 14 | 0 | 16,07 |
| ColegaTeste | 2119 | 18 | 9 | 4 | 29 | 3 | 433 | 453 | 360 | 39,83 |
| BranchPedrao | 942 | 10 | 4 | 0 | 29 | 3 | 350 | 11 | 0 | 13,56 |

## Cenas humanas encontradas nas branches
| Branch | Path | SizeKB | GameObjects | PrefabInstances | MonoBehaviours |
| --- | --- | --- | --- | --- | --- |
| BranchPedrao | Scenes\bla.unity | 37,94 | 9 | 0 | 2 |
| BranchPedrao | Scenes\Cena do JHON.unity | 21,19 | 5 | 0 | 5 |
| BranchPedrao | Scenes\CenaDoCT.unity | 13,07 | 3 | 0 | 3 |
| BranchPedrao | Scenes\Ceta big Peter.unity | 37,94 | 9 | 0 | 2 |
| ChegaComJhon | Scenes\bla.unity | 32,13 | 8 | 0 | 6 |
| ChegaComJhon | Scenes\CenaDoCT.unity | 19,95 | 5 | 0 | 7 |
| ChegaComJhon | Scenes\ServerHub.unity | 29,09 | 10 | 0 | 14 |
| ColegaTeste | Decoracao_Background.unity | 5,86 | 1 | 0 | 0 |
| ColegaTeste | PlatformerSet1\Scene\Demo.unity | 12,07 | 4 | 0 | 0 |
| ColegaTeste | ProjetoPlayerJhonas\Scenes\bla.unity | 34,3 | 8 | 0 | 1 |
| ColegaTeste | ProjetoPlayerJhonas\Scenes\Cena do JHON.unity | 21,19 | 5 | 0 | 5 |
| ColegaTeste | ProjetoPlayerJhonas\Scenes\CenaDoCT.unity | 13,07 | 3 | 0 | 3 |
| ColegaTeste | Scenes\CenaDoColega.unity | 7313,25 | 41 | 0 | 5 |
| ColegaTeste | Scenes\CenaDoCT.unity | 56,31 | 7 | 0 | 3 |
| ColegaTeste | Scenes\Mapa_Base.unity | 5,87 | 1 | 0 | 0 |
| ColegaTeste | Scenes\SampleScene.unity | 3906,7 | 11 | 0 | 6 |

## Prefabs encontrados
| Branch | Path | SizeKB | GameObjects | PrefabInstances | MonoBehaviours |
| --- | --- | --- | --- | --- | --- |
| ChegaComJhon | Jogador.prefab | 6,53 | 1 | 0 | 3 |
| ColegaTeste | PlatformerSet1\Map Assets\Decoration\Decoration.prefab | 37,73 | 2 | 0 | 1 |
| ColegaTeste | PlatformerSet1\Map Assets\TilepaletteLevel1\New Tile Palette.prefab | 58,53 | 2 | 0 | 1 |
| ColegaTeste | PlatformerSet1\Map Assets\TileplateLevel1Background\Level1Background.prefab | 59,18 | 2 | 0 | 1 |
| ColegaTeste | ProjetoTesteAssets\TileMap.prefab | 70,9 | 2 | 0 | 1 |

## Configuracao de imagens por importador Unity
Leitura feita a partir dos `.meta` de imagem. `TextureType=8` indica Sprite no Unity.
| Branch | IsSprite | TextureType | SpriteMode | Count |
| --- | --- | --- | --- | --- |
| BranchPedrao | False | 0 | 2 | 1 |
| BranchPedrao | True | 8 | 2 | 349 |
| ChegaComJhon | False | 0 | 2 | 1 |
| ChegaComJhon | True | 8 | 2 | 355 |
| ColegaTeste | False | 0 | 2 | 2 |
| ColegaTeste | True | 8 | 1 | 57 |
| ColegaTeste | True | 8 | 2 | 374 |

## Configuracoes relevantes localizadas
- `DefaultNetworkPrefabs.asset`: presente em ChegaComJhon.
- `InputSystem_Actions.inputactions`: presente em todas as origens, com duplicatas em subprojetos do ColegaTeste.
- `UniversalRP.asset`, `Renderer2D.asset`, `DefaultVolumeProfile.asset`: presentes em todas as origens e preservados dentro de `Assets/RecursosUnificados`.
- `TMP Settings.asset`: presente nas origens importadas, preservado para consulta sem sobrescrever a configuracao atual.

## Decisao de seguranca
As configuracoes globais das branches nao foram aplicadas por cima da `ct-teste`. Elas ficaram preservadas como referencia para evitar quebrar Render Pipeline, Input System, TextMesh Pro e Netcode ja funcionais no projeto atual.
