using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using System.Text;

#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class Movimentacao : NetworkBehaviour
{
    readonly NetworkVariable<FixedString64Bytes> nomeRede = new(
        "Jogador",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    readonly NetworkVariable<float> velXRede = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<float> velYRede = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<bool> correndoRede = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<bool> dashandoRede = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<bool> noChaoRede = new(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<bool> flipXRede = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    readonly NetworkVariable<byte> etapaAtaqueRede = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    [SerializeField] float vel = 5f;
    [SerializeField] float forcaPulo = 9f;
    [SerializeField] float gravidade = 3f;
    [SerializeField] float multiplicadorCorrida = 1.4f;
    [SerializeField] float distanciaChecarChao = 0.08f;
    [SerializeField] LayerMask camadaChao = ~0;
    [SerializeField] Vector3 deslocamentoNome = new(0f, 0.85f, 0f);
    [Header("Visual / Spritesheet")]
    [SerializeField] Texture2D tilesetJogador1;
    [SerializeField] Texture2D tilesetJogador2;
    [SerializeField] bool usarTilesetJogador2;
    [SerializeField] bool inverterSpritePeloMovimento = true;
    [SerializeField] bool inverterDirecaoDoFlipX = true;
    [SerializeField] float fpsAnimacao = 10f;
    [SerializeField] float fpsAtaque = 14f;
    [SerializeField] bool bloquearMovimentoDuranteAtaque = true;
    [Header("Rede / Otimizacao")]
    [SerializeField] float intervaloSyncEstadoRede = 0.05f;
    [SerializeField] float limiarSyncVelocidadeRede = 0.08f;
    [Header("Dash e Ataque")]
    [SerializeField] float forcaDash = 14f;
    [SerializeField] float duracaoDash = 0.14f;
    [SerializeField] float cooldownDash = 0.65f;
    [SerializeField] float tempoEntreAtaques = 0.2f;
    [SerializeField] float janelaCombo = 0.45f;
    [SerializeField, HideInInspector] Sprite[] spritesJogador1 = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] Sprite[] spritesJogador2 = System.Array.Empty<Sprite>();

    Rigidbody2D corpo;
    Collider2D colisor;
    SpriteRenderer sprite;
    TextMeshPro textoNome;
    bool pediuPulo;
    bool pediuAtaque;
    bool pediuDash;
    bool estaAtacando;
    bool estaDashando;
    bool correndo;
    float momentoFimDash;
    float proximoDashDisponivel;
    float proximoAtaqueDisponivel;
    float direcaoDashAtual = 1f;
    float momentoUltimoCliqueAtaque;
    int etapaAtaqueAtual;
    int cliquesAtaqueEmFila;
    readonly List<Sprite> framesIdle = new();
    readonly List<Sprite> framesWalk = new();
    readonly List<Sprite> framesRun = new();
    readonly List<Sprite> framesJump = new();
    readonly List<Sprite> framesJumpFallback = new();
    readonly List<Sprite> framesAttack1 = new();
    readonly List<Sprite> framesAttack2 = new();
    readonly List<Sprite> framesAttack3 = new();
    readonly List<Sprite> framesAttackFallback = new();
    List<Sprite> framesAtaqueAtivos;
    List<Sprite> framesAtivos;
    int indiceFrameAtual;
    float acumuladorAnimacao;
    bool loopAnimacaoAtual = true;
    bool estaNoChaoCache = true;
    float proximoSyncEstadoRede;
    bool estadoRedeSincronizado;
    float ultimoVelXSync;
    float ultimoVelYSync;
    bool ultimoCorrendoSync;
    bool ultimoDashSync;
    bool ultimoNoChaoSync;
    bool ultimoFlipXSync;

#if UNITY_EDITOR
    const string PastaSpritesPlayer1 = "Assets/Sprites/Objetos/Player 1";
    const string CaminhoSpriteInicialPlayer1 = "Assets/Sprites/Objetos/Player 1/Idle/idle1.png";
#endif

    void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        colisor = GetComponent<Collider2D>();
        if (colisor == null) colisor = gameObject.AddComponent<BoxCollider2D>();

        sprite = GetComponent<SpriteRenderer>();
        CriarTextoSePreciso();

        // Movimento de plataforma: mantem rotacao estavel e garante gravidade esperada.
        corpo.freezeRotation = true;
        corpo.gravityScale = gravidade;

#if UNITY_EDITOR
        TentarPreencherCacheDeSpritesNoEditor();
#endif

        ConfigurarSpritesDoTilesetSelecionado();
        AplicarFrameInicial();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        nomeRede.OnValueChanged += OnNomeMudou;
        etapaAtaqueRede.OnValueChanged += OnEtapaAtaqueRedeMudou;
        AtualizarTextoNome(nomeRede.Value.ToString());
        ConfigurarFisicaPorOwnership();

        if (IsServer)
        {
            string nome = GerenciadorNomesRede.PegarNomeDoClient(OwnerClientId);
            if (string.IsNullOrWhiteSpace(nome)) nome = "Jogador";
            nomeRede.Value = new FixedString64Bytes(nome);
        }

        if (IsOwner)
        {
            SincronizarEstadoRedeDoDono(
                corpo != null ? corpo.linearVelocity.x : 0f,
                corpo != null ? corpo.linearVelocity.y : 0f,
                EstaNoChao(),
                true);
        }
        else
        {
            OnEtapaAtaqueRedeMudou(0, etapaAtaqueRede.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        nomeRede.OnValueChanged -= OnNomeMudou;
        etapaAtaqueRede.OnValueChanged -= OnEtapaAtaqueRedeMudou;
        estaDashando = false;
        estaAtacando = false;
        etapaAtaqueAtual = 0;
        cliquesAtaqueEmFila = 0;
        corpo.linearVelocity = Vector2.zero;
        if (corpo != null)
        {
            corpo.bodyType = RigidbodyType2D.Dynamic;
            corpo.gravityScale = gravidade;
        }
    }

    void Update()
    {
        AtualizarAnimacaoVisual();

        if (!IsSpawned || !IsOwner) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        correndo = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;

        if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
        {
            pediuPulo = true;
        }

        if (kb.leftAltKey.wasPressedThisFrame || kb.rightAltKey.wasPressedThisFrame)
        {
            pediuDash = true;
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            momentoUltimoCliqueAtaque = Time.time;
            if (estaAtacando)
            {
                if (Time.time <= proximoAtaqueDisponivel + janelaCombo && etapaAtaqueAtual < 3)
                {
                    int maxFila = Mathf.Max(0, 3 - etapaAtaqueAtual);
                    cliquesAtaqueEmFila = Mathf.Min(maxFila, cliquesAtaqueEmFila + 1);
                }
            }
            else
            {
                pediuAtaque = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (!IsSpawned)
        {
            return;
        }

        if (!IsOwner)
        {
            return;
        }

        var t = Keyboard.current; if (t == null) return;
        if (corpo.bodyType != RigidbodyType2D.Dynamic)
        {
            corpo.bodyType = RigidbodyType2D.Dynamic;
            corpo.gravityScale = gravidade;
        }
        float eixoX = (t.dKey.isPressed || t.rightArrowKey.isPressed ? 1 : 0) - (t.aKey.isPressed || t.leftArrowKey.isPressed ? 1 : 0);
        float velFinal = vel * (correndo ? multiplicadorCorrida : 1f);

        Vector2 v = corpo.linearVelocity;
        estaNoChaoCache = EstaNoChao();

        if (estaDashando)
        {
            if (Time.time >= momentoFimDash)
            {
                estaDashando = false;
            }
            else
            {
                v.x = direcaoDashAtual * forcaDash;
                corpo.linearVelocity = v;
                SincronizarEstadoRedeDoDono(v.x, v.y, estaNoChaoCache);
                pediuPulo = false;
                pediuAtaque = false;
                pediuDash = false;
                return;
            }
        }

        if (pediuDash && !estaAtacando && Time.time >= proximoDashDisponivel)
        {
            IniciarDash(eixoX);
            SincronizarEstadoRedeDoDono(
                corpo != null ? corpo.linearVelocity.x : 0f,
                corpo != null ? corpo.linearVelocity.y : 0f,
                false,
                true);
            pediuPulo = false;
            pediuAtaque = false;
            pediuDash = false;
            return;
        }

        bool travarMovimento = estaAtacando && bloquearMovimentoDuranteAtaque;
        v.x = travarMovimento ? 0f : eixoX * velFinal;

        if (pediuPulo && estaNoChaoCache)
        {
            v.y = forcaPulo;
            estaNoChaoCache = false;
        }

        if (pediuAtaque && !estaAtacando && Time.time >= proximoAtaqueDisponivel)
        {
            IniciarAtaque(1);
        }

        corpo.linearVelocity = v;
        bool noChaoParaSync = estaNoChaoCache && Mathf.Abs(v.y) < 0.05f;
        SincronizarEstadoRedeDoDono(v.x, v.y, noChaoParaSync);
        pediuPulo = false;
        pediuAtaque = false;
        pediuDash = false;
    }

    void OnNomeMudou(FixedString64Bytes anterior, FixedString64Bytes atual)
    {
        AtualizarTextoNome(atual.ToString());
    }

    bool EstaNoChao()
    {
        Bounds b = colisor.bounds;
        Vector2 origem = new Vector2(b.center.x, b.min.y - 0.02f);
        Vector2 tamanho = new Vector2(b.size.x * 0.9f, 0.02f);
        var hit = Physics2D.BoxCast(origem, tamanho, 0f, Vector2.down, distanciaChecarChao, camadaChao);
        return hit.collider != null;
    }

    void CriarTextoSePreciso()
    {
        if (textoNome != null) return;

        var go = new GameObject("NomeJogador");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = deslocamentoNome;

        textoNome = go.AddComponent<TextMeshPro>();
        textoNome.alignment = TextAlignmentOptions.Center;
        textoNome.fontSize = 2.5f;
        textoNome.color = Color.white;
        textoNome.outlineWidth = 0.2f;
        textoNome.text = "Jogador";
    }

    void AtualizarTextoNome(string valor)
    {
        if (textoNome == null) return;
        textoNome.text = valor;
        if (sprite != null) sprite.enabled = true;
        textoNome.gameObject.SetActive(true);
    }

    void AtualizarAnimacaoVisual()
    {
        if (sprite == null) return;
        if (framesIdle.Count == 0 && framesWalk.Count == 0 && framesRun.Count == 0 && framesJump.Count == 0 && framesJumpFallback.Count == 0) return;

        bool usarEstadoRede = IsSpawned && !IsOwner;
        float velX = usarEstadoRede ? velXRede.Value : (corpo != null ? corpo.linearVelocity.x : 0f);
        float velY = usarEstadoRede ? velYRede.Value : (corpo != null ? corpo.linearVelocity.y : 0f);
        float velocidadeX = Mathf.Abs(velX);
        bool estaMovendo = velocidadeX > 0.05f;
        bool estaNoChao = usarEstadoRede ? noChaoRede.Value : (IsSpawned ? estaNoChaoCache : EstaNoChao());
        bool correndoVisual = usarEstadoRede ? correndoRede.Value : correndo;
        bool dashandoVisual = usarEstadoRede ? dashandoRede.Value : estaDashando;
        bool atacandoVisual = usarEstadoRede ? etapaAtaqueRede.Value > 0 : estaAtacando;

        if (inverterSpritePeloMovimento)
        {
            if (usarEstadoRede)
            {
                sprite.flipX = flipXRede.Value;
            }
            else
            {
                if (velX > 0.05f) sprite.flipX = inverterDirecaoDoFlipX;
                else if (velX < -0.05f) sprite.flipX = !inverterDirecaoDoFlipX;
            }
        }

        if (atacandoVisual && framesAtaqueAtivos != null && framesAtaqueAtivos.Count > 0)
        {
            TrocarFramesAtivos(framesAtaqueAtivos, false);
            bool terminou = AvancarAnimacao(fpsAtaque);
            if (terminou && (!IsSpawned || IsOwner))
            {
                AvancarComboOuFinalizarAtaque();
            }
            return;
        }

        if (dashandoVisual)
        {
            List<Sprite> framesDash = framesRun.Count > 0 ? framesRun : (framesWalk.Count > 0 ? framesWalk : framesIdle);
            TrocarFramesAtivos(framesDash, true);
            AvancarAnimacao(fpsAnimacao * 1.35f);
            return;
        }

        if (!estaNoChao)
        {
            List<Sprite> framesNoAr = framesJump.Count > 0
                ? framesJump
                : (framesJumpFallback.Count > 0 ? framesJumpFallback : (framesIdle.Count > 0 ? framesIdle : framesWalk));
            TrocarFramesAtivos(framesNoAr, true);
            AvancarAnimacao(fpsAnimacao);
            return;
        }

        if (!estaMovendo)
        {
            TrocarFramesAtivos(framesIdle.Count > 0 ? framesIdle : (framesWalk.Count > 0 ? framesWalk : framesRun), true);
        }
        else if (correndoVisual && framesRun.Count > 0)
        {
            TrocarFramesAtivos(framesRun, true);
        }
        else
        {
            TrocarFramesAtivos(framesWalk.Count > 0 ? framesWalk : (framesRun.Count > 0 ? framesRun : framesIdle), true);
        }

        AvancarAnimacao(fpsAnimacao);
    }

    void TrocarFramesAtivos(List<Sprite> novosFrames, bool loop)
    {
        if (novosFrames == null || novosFrames.Count == 0) return;
        if (ReferenceEquals(framesAtivos, novosFrames) && loopAnimacaoAtual == loop) return;

        framesAtivos = novosFrames;
        loopAnimacaoAtual = loop;
        indiceFrameAtual = 0;
        acumuladorAnimacao = 0f;
        sprite.sprite = framesAtivos[0];
    }

    bool AvancarAnimacao(float fps)
    {
        if (framesAtivos == null || framesAtivos.Count == 0) return false;

        acumuladorAnimacao += Time.deltaTime;
        float frameDuracao = 1f / Mathf.Max(1f, fps);
        if (acumuladorAnimacao < frameDuracao) return false;

        acumuladorAnimacao -= frameDuracao;
        indiceFrameAtual++;

        if (indiceFrameAtual >= framesAtivos.Count)
        {
            if (loopAnimacaoAtual)
            {
                indiceFrameAtual = 0;
            }
            else
            {
                indiceFrameAtual = framesAtivos.Count - 1;
                sprite.sprite = framesAtivos[indiceFrameAtual];
                return true;
            }
        }

        sprite.sprite = framesAtivos[indiceFrameAtual];
        return false;
    }

    void IniciarDash(float eixoX)
    {
        float dir;
        if (Mathf.Abs(eixoX) > 0.01f)
        {
            dir = Mathf.Sign(eixoX);
        }
        else
        {
            bool olhandoDireita = sprite != null
                ? (inverterDirecaoDoFlipX ? sprite.flipX : !sprite.flipX)
                : true;
            dir = olhandoDireita ? 1f : -1f;
        }

        direcaoDashAtual = dir;
        estaDashando = true;
        momentoFimDash = Time.time + duracaoDash;
        proximoDashDisponivel = Time.time + cooldownDash;

        Vector2 v = corpo.linearVelocity;
        v.x = direcaoDashAtual * forcaDash;
        corpo.linearVelocity = v;
    }

    void IniciarAtaque(int etapa)
    {
        List<Sprite> framesDaEtapa = ObterFramesAtaquePorEtapa(etapa);
        if (framesDaEtapa == null || framesDaEtapa.Count == 0) return;

        etapaAtaqueAtual = Mathf.Clamp(etapa, 1, 3);
        framesAtaqueAtivos = framesDaEtapa;
        estaAtacando = true;
        proximoAtaqueDisponivel = Time.time + tempoEntreAtaques;
        if (IsSpawned && IsOwner)
        {
            etapaAtaqueRede.Value = (byte)etapaAtaqueAtual;
        }

        TrocarFramesAtivos(framesAtaqueAtivos, false);
    }

    List<Sprite> ObterFramesAtaquePorEtapa(int etapa)
    {
        if (etapa == 1 && framesAttack1.Count > 0) return framesAttack1;
        if (etapa == 2 && framesAttack2.Count > 0) return framesAttack2;
        if (etapa == 3 && framesAttack3.Count > 0) return framesAttack3;

        if (framesAttack1.Count > 0) return framesAttack1;
        if (framesAttackFallback.Count > 0) return framesAttackFallback;
        if (framesAttack2.Count > 0) return framesAttack2;
        if (framesAttack3.Count > 0) return framesAttack3;
        return null;
    }

    void AvancarComboOuFinalizarAtaque()
    {
        bool podeCombinar = cliquesAtaqueEmFila > 0
            && etapaAtaqueAtual < 3
            && (Time.time - momentoUltimoCliqueAtaque) <= janelaCombo;

        if (podeCombinar)
        {
            cliquesAtaqueEmFila = Mathf.Max(0, cliquesAtaqueEmFila - 1);
            IniciarAtaque(etapaAtaqueAtual + 1);
            return;
        }

        estaAtacando = false;
        etapaAtaqueAtual = 0;
        cliquesAtaqueEmFila = 0;
        framesAtaqueAtivos = null;
        if (IsSpawned && IsOwner)
        {
            etapaAtaqueRede.Value = 0;
        }
    }

    void OnEtapaAtaqueRedeMudou(byte anterior, byte atual)
    {
        if (!IsSpawned || IsOwner) return;

        int etapa = Mathf.Clamp(atual, 0, 3);
        if (etapa <= 0)
        {
            estaAtacando = false;
            etapaAtaqueAtual = 0;
            cliquesAtaqueEmFila = 0;
            framesAtaqueAtivos = null;
            return;
        }

        etapaAtaqueAtual = etapa;
        framesAtaqueAtivos = ObterFramesAtaquePorEtapa(etapaAtaqueAtual);
        estaAtacando = framesAtaqueAtivos != null && framesAtaqueAtivos.Count > 0;
        if (estaAtacando)
        {
            TrocarFramesAtivos(framesAtaqueAtivos, false);
        }
    }

    void SincronizarEstadoRedeDoDono(float velXAtual, float velYAtual, bool noChaoAtual)
    {
        SincronizarEstadoRedeDoDono(velXAtual, velYAtual, noChaoAtual, false);
    }

    void SincronizarEstadoRedeDoDono(float velXAtual, float velYAtual, bool noChaoAtual, bool forcar)
    {
        if (!IsSpawned || !IsOwner) return;
        if (!forcar && Time.unscaledTime < proximoSyncEstadoRede) return;

        bool flipAtual = sprite != null && sprite.flipX;
        bool precisaSync = forcar || !estadoRedeSincronizado;
        if (!precisaSync)
        {
            if (Mathf.Abs(velXAtual - ultimoVelXSync) >= limiarSyncVelocidadeRede) precisaSync = true;
            else if (Mathf.Abs(velYAtual - ultimoVelYSync) >= limiarSyncVelocidadeRede) precisaSync = true;
            else if (correndo != ultimoCorrendoSync) precisaSync = true;
            else if (estaDashando != ultimoDashSync) precisaSync = true;
            else if (noChaoAtual != ultimoNoChaoSync) precisaSync = true;
            else if (flipAtual != ultimoFlipXSync) precisaSync = true;
        }

        proximoSyncEstadoRede = Time.unscaledTime + Mathf.Max(0.02f, intervaloSyncEstadoRede);
        if (!precisaSync) return;

        velXRede.Value = velXAtual;
        velYRede.Value = velYAtual;
        correndoRede.Value = correndo;
        dashandoRede.Value = estaDashando;
        noChaoRede.Value = noChaoAtual;
        flipXRede.Value = flipAtual;

        ultimoVelXSync = velXAtual;
        ultimoVelYSync = velYAtual;
        ultimoCorrendoSync = correndo;
        ultimoDashSync = estaDashando;
        ultimoNoChaoSync = noChaoAtual;
        ultimoFlipXSync = flipAtual;
        estadoRedeSincronizado = true;
    }

    void ConfigurarFisicaPorOwnership()
    {
        if (corpo == null) return;

        if (!IsSpawned || IsOwner)
        {
            corpo.bodyType = RigidbodyType2D.Dynamic;
            corpo.gravityScale = gravidade;
            return;
        }

        corpo.bodyType = RigidbodyType2D.Kinematic;
        corpo.gravityScale = 0f;
        corpo.linearVelocity = Vector2.zero;
    }

    void ConfigurarSpritesDoTilesetSelecionado()
    {
        framesIdle.Clear();
        framesWalk.Clear();
        framesRun.Clear();
        framesJump.Clear();
        framesJumpFallback.Clear();
        framesAttack1.Clear();
        framesAttack2.Clear();
        framesAttack3.Clear();
        framesAttackFallback.Clear();
        framesAtaqueAtivos = null;
        estaAtacando = false;
        etapaAtaqueAtual = 0;
        cliquesAtaqueEmFila = 0;
        framesAtivos = null;
        loopAnimacaoAtual = true;
        indiceFrameAtual = 0;
        acumuladorAnimacao = 0f;

        Sprite[] baseSprites = usarTilesetJogador2 && spritesJogador2 != null && spritesJogador2.Length > 0
            ? spritesJogador2
            : spritesJogador1;

        if (baseSprites == null || baseSprites.Length == 0) return;

        foreach (var s in baseSprites)
        {
            if (s == null) continue;
            string nome = s.name.ToLowerInvariant();
            if (EhNomeIdle(nome))
            {
                framesIdle.Add(s);
                continue;
            }

            if (EhNomeWalk(nome))
            {
                framesWalk.Add(s);
                continue;
            }

            if (EhNomeRun(nome))
            {
                framesRun.Add(s);
                continue;
            }

            if (EhNomeAtaque(nome))
            {
                if (EhNomeAtaqueEtapa(nome, 3))
                {
                    framesAttack3.Add(s);
                }
                else if (EhNomeAtaqueEtapa(nome, 2))
                {
                    framesAttack2.Add(s);
                }
                else if (EhNomeAtaqueEtapa(nome, 1))
                {
                    framesAttack1.Add(s);
                }
                else if (nome.Contains("jump"))
                {
                    framesJumpFallback.Add(s);
                }
                else
                {
                    framesAttackFallback.Add(s);
                }
                continue;
            }

            if (EhNomeJump(nome))
            {
                framesJump.Add(s);
            }
        }

        OrdenarFramesPorIndice(framesIdle);
        OrdenarFramesPorIndice(framesWalk);
        OrdenarFramesPorIndice(framesRun);
        OrdenarFramesPorIndice(framesJump);
        OrdenarFramesPorIndice(framesJumpFallback);
        OrdenarFramesPorIndice(framesAttack1);
        OrdenarFramesPorIndice(framesAttack2);
        OrdenarFramesPorIndice(framesAttack3);
        OrdenarFramesPorIndice(framesAttackFallback);

        if (framesRun.Count == 0 && framesWalk.Count > 0) framesRun.AddRange(framesWalk);
        if (framesWalk.Count == 0 && framesRun.Count > 0) framesWalk.AddRange(framesRun);
        if (framesIdle.Count == 0 && framesWalk.Count > 0) framesIdle.Add(framesWalk[0]);
        if (framesJump.Count == 0 && framesJumpFallback.Count > 0) framesJump.AddRange(framesJumpFallback);
        if (framesJumpFallback.Count == 0 && framesJump.Count > 0) framesJumpFallback.AddRange(framesJump);
        if (framesAttack1.Count == 0 && framesAttackFallback.Count > 0) framesAttack1.AddRange(framesAttackFallback);
        if (framesAttack2.Count == 0 && framesAttack1.Count > 0) framesAttack2.AddRange(framesAttack1);
        if (framesAttack3.Count == 0 && framesAttack2.Count > 0) framesAttack3.AddRange(framesAttack2);
    }

    static bool EhNomeIdle(string nome)
    {
        return nome.Contains("idle");
    }

    static bool EhNomeWalk(string nome)
    {
        return nome.Contains("walk");
    }

    static bool EhNomeRun(string nome)
    {
        return nome.Contains("run_") || nome.Contains("run-") || nome.StartsWith("run");
    }

    static bool EhNomeJump(string nome)
    {
        return nome.Contains("jump") || nome.StartsWith("pngs_");
    }

    static bool EhNomeAtaque(string nome)
    {
        return nome.Contains("attack") || nome.Contains("attk");
    }

    static bool EhNomeAtaqueEtapa(string nome, int etapa)
    {
        if (etapa == 3)
        {
            return nome.Contains("attk-3") || nome.Contains("attack-3") || nome.Contains("ataque3");
        }

        if (etapa == 2)
        {
            return nome.Contains("attk-2") || nome.Contains("attack-2") || nome.Contains("ataque2");
        }

        return nome.Contains("attk-1")
            || nome.Contains("attack-1")
            || nome.Contains("attack_front")
            || nome.Contains("rush_attack")
            || nome.Contains("ataque1");
    }

    void OrdenarFramesPorIndice(List<Sprite> frames)
    {
        frames.Sort((a, b) =>
        {
            int ai = ExtrairIndiceDoNome(a != null ? a.name : string.Empty);
            int bi = ExtrairIndiceDoNome(b != null ? b.name : string.Empty);
            if (ai != bi) return ai.CompareTo(bi);
            return string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty);
        });
    }

    int ExtrairIndiceDoNome(string nome)
    {
        if (string.IsNullOrEmpty(nome)) return int.MaxValue;

        var buffer = new StringBuilder();
        int ultimoNumero = int.MaxValue;
        bool encontrouNumero = false;

        for (int i = 0; i < nome.Length; i++)
        {
            char c = nome[i];
            if (char.IsDigit(c))
            {
                buffer.Append(c);
                continue;
            }

            if (buffer.Length > 0)
            {
                if (int.TryParse(buffer.ToString(), out int valorAtual))
                {
                    ultimoNumero = valorAtual;
                    encontrouNumero = true;
                }
                buffer.Clear();
            }
        }

        if (buffer.Length > 0 && int.TryParse(buffer.ToString(), out int valorFinal))
        {
            ultimoNumero = valorFinal;
            encontrouNumero = true;
        }

        if (encontrouNumero) return ultimoNumero;
        return int.MaxValue;
    }

    void AplicarFrameInicial()
    {
        if (sprite == null) return;

        if (framesIdle.Count > 0) sprite.sprite = framesIdle[0];
        else if (framesWalk.Count > 0) sprite.sprite = framesWalk[0];
        else if (framesRun.Count > 0) sprite.sprite = framesRun[0];
        else if (framesJump.Count > 0) sprite.sprite = framesJump[0];
        else if (framesJumpFallback.Count > 0) sprite.sprite = framesJumpFallback[0];
        else if (framesAttack1.Count > 0) sprite.sprite = framesAttack1[0];
        else if (framesAttackFallback.Count > 0) sprite.sprite = framesAttackFallback[0];
        else if (framesAttack2.Count > 0) sprite.sprite = framesAttack2[0];
        else if (framesAttack3.Count > 0) sprite.sprite = framesAttack3[0];
        else if (spritesJogador1 != null && spritesJogador1.Length > 0)
        {
            for (int i = 0; i < spritesJogador1.Length; i++)
            {
                if (spritesJogador1[i] == null) continue;
                sprite.sprite = spritesJogador1[i];
                break;
            }
        }

        if (sprite.sprite != null) sprite.enabled = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        vel = Mathf.Max(0f, vel);
        forcaPulo = Mathf.Max(0f, forcaPulo);
        gravidade = Mathf.Max(0f, gravidade);
        multiplicadorCorrida = Mathf.Max(1f, multiplicadorCorrida);
        fpsAnimacao = Mathf.Clamp(fpsAnimacao, 1f, 30f);
        fpsAtaque = Mathf.Clamp(fpsAtaque, 1f, 60f);
        intervaloSyncEstadoRede = Mathf.Clamp(intervaloSyncEstadoRede, 0.02f, 0.2f);
        limiarSyncVelocidadeRede = Mathf.Clamp(limiarSyncVelocidadeRede, 0.01f, 0.5f);
        forcaDash = Mathf.Max(0f, forcaDash);
        duracaoDash = Mathf.Max(0.02f, duracaoDash);
        cooldownDash = Mathf.Max(0f, cooldownDash);
        tempoEntreAtaques = Mathf.Max(0.02f, tempoEntreAtaques);
        janelaCombo = Mathf.Max(0.08f, janelaCombo);

        if (tilesetJogador1 == null)
        {
            tilesetJogador1 = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoSpriteInicialPlayer1);
        }

        spritesJogador1 = ExtrairSpritesDoTileset(tilesetJogador1);
        if (spritesJogador1.Length <= 1)
        {
            spritesJogador1 = ExtrairSpritesPadraoJogador1();
        }

        spritesJogador2 = ExtrairSpritesDoTileset(tilesetJogador2);
    }

    void TentarPreencherCacheDeSpritesNoEditor()
    {
        if (tilesetJogador1 == null)
        {
            tilesetJogador1 = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoSpriteInicialPlayer1);
        }

        if (spritesJogador1 == null || spritesJogador1.Length <= 1)
        {
            spritesJogador1 = ExtrairSpritesPadraoJogador1();
        }

        if (sprite != null && sprite.sprite == null)
        {
            var spriteInicial = AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoSpriteInicialPlayer1);
            if (spriteInicial != null) sprite.sprite = spriteInicial;
        }
    }

    Sprite[] ExtrairSpritesPadraoJogador1()
    {
        if (!AssetDatabase.IsValidFolder(PastaSpritesPlayer1))
        {
            return Array.Empty<Sprite>();
        }

        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { PastaSpritesPlayer1 });
        if (guids == null || guids.Length == 0)
        {
            return Array.Empty<Sprite>();
        }

        var sprites = new List<Sprite>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrWhiteSpace(caminho)) continue;
            if (!caminho.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

            var spriteAsset = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
            if (spriteAsset == null) continue;
            sprites.Add(spriteAsset);
        }

        if (sprites.Count == 0)
        {
            return Array.Empty<Sprite>();
        }

        return sprites
            .OrderBy(s => AssetDatabase.GetAssetPath(s), StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    Sprite[] ExtrairSpritesDoTileset(Texture2D tileset)
    {
        if (tileset == null) return Array.Empty<Sprite>();

        string caminho = AssetDatabase.GetAssetPath(tileset);
        if (string.IsNullOrWhiteSpace(caminho)) return Array.Empty<Sprite>();

        return AssetDatabase.LoadAllAssetsAtPath(caminho)
            .OfType<Sprite>()
            .OrderBy(s => s.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
#endif
}
