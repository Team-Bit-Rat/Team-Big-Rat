# 07 - Checklist de Verificacao no Unity

## Abrir e confirmar
- Abrir `Assets/Scenes/Unificadas` e carregar as cenas uma por uma.
- Conferir Console para `Missing Script`, `Duplicate GUID` e referencias ausentes.
- Comecar por `Unified_ChegaComJhon_Scenes_ServerHub.unity`, depois `Unified_ColegaTeste_Scenes_CenaDoColega.unity`, depois as cenas `bla`.

## Validar gameplay
- Confirmar que o multiplayer da cena principal `CenaDoCT` continua funcionando.
- Arrastar/prefabicar boss usando `SerberusAI` e conferir Animator com parametros esperados.
- Testar `Enemy` com `Entity` e `CombatHitbox` em cena pequena antes de juntar com multiplayer.

## Integracao recomendada
- Nao substituir `Player.cs` nem `Movimentacao.cs` atuais sem teste isolado.
- Consolidar mapa primeiro, depois inimigos, depois boss, depois sincronizacao multiplayer de inimigos/boss.
- Quando uma cena funcionar, criar prefab limpo do recurso final e mover para pasta definitiva.
