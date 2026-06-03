# 06 - Analise de Funcionalidades e Scripts

Scripts atuais em Assets/Scripts: 21
Scripts preservados em snapshot: 38

## Funcionalidades principais identificadas
- Multiplayer funcional da `ct-teste`: `Client`, `ServerHubTela`, `RedeBootstrap`, descoberta/anuncio LAN, aprovacao de nome, `ClientNetworkTransform` e `ClientNetworkAnimator`.
- Gameplay atual: `Movimentacao` como `NetworkBehaviour`, movimento/pulo/dash/ataque com estado em rede e `Player` local com combo/dash.
- IA do boss: `SerberusAI` importado para `Assets/Scripts/IA/Boss` com meta original preservado.
- Combate/inimigo da BranchPedrao: `Entity`, `Enemy` e `CombatHitbox` importados para `Assets/Scripts/Combate` com metas originais preservados.
- Movimento legado do ColegaTeste: `PlayerMobiment` importado para `Assets/Scripts/Legacy/ColegaTeste`.

## Scripts com variantes pelo mesmo nome
| Name | Versions | Origins |
| --- | --- | --- |
| Player.cs | 3 | CT-TESTE atual, BranchPedrao, ChegaComJhon, ColegaTeste |
| FlutuarSuaveUI.cs | 2 | CT-TESTE atual, ChegaComJhon |
| PulsarUI.cs | 2 | CT-TESTE atual, ChegaComJhon |
| Movimentacao.cs | 3 | CT-TESTE atual, BranchPedrao, ChegaComJhon, ColegaTeste |
| AnuncianteHostLAN.cs | 2 | CT-TESTE atual, ChegaComJhon |
| DescobridorHostLAN.cs | 2 | CT-TESTE atual, ChegaComJhon |
| ServerHubTela.cs | 2 | CT-TESTE atual, ChegaComJhon |

## Clones exatos detectados por hash
| Count | Names |
| --- | --- |
| 2 | Player.cs |
| 2 | CombatHitbox.cs |
| 2 | Enemy.cs |
| 2 | Entity.cs |
| 2 | DadosSessaoLocal.cs |
| 2 | BlocoColisaoMapa.cs |
| 2 | SerberusAI.cs |
| 2 | PlayerMobiment.cs |
| 2 | Client.cs |
| 2 | ClientNetworkAnimator.cs |
| 2 | ClientNetworkTransform.cs |
| 2 | ConexaoPendente.cs |
| 2 | GerenciadorNomesRede.cs |
| 2 | RedeBootstrap.cs |
| 2 | RedeLanConst.cs |
| 2 | Movimentacao.cs |

## Decisao de substituicao
- Mantida a `ct-teste` como fonte principal para multiplayer e gameplay de rede.
- Scripts unicos foram trazidos para `Assets/Scripts` porque nao colidem e adicionam funcionalidade.
- Scripts duplicados como `Player.cs`, `Movimentacao.cs` e variantes de rede ficaram preservados em snapshot para merge manual, porque substituir automaticamente poderia quebrar o multiplayer atual.

## Validacao de compilacao
Foi executado `dotnet restore Assembly-CSharp.csproj` com sucesso. O `dotnet build` fora da Unity falhou por referencias Unity/Package Manager ausentes no contexto do .NET CLI (`Netcode`, `TMPro`, `UnityEngine.UI`, `InputSystem`). Isso nao aponta erro novo dos scripts importados; a validacao real deve ser feita no Console da Unity.

## Arquivos brutos
- `inventario_scripts.csv`
- `scripts_clones_exatos.csv`
- `scripts_variantes_mesmo_nome.csv`
