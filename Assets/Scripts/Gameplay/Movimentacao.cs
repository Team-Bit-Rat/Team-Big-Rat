using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

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
    [SerializeField] float fpsAnimacao = 10f;
    [SerializeField] float fpsAtaque = 14f;
    [SerializeField] bool bloquearMovimentoDuranteAtaque = true;
    [SerializeField, HideInInspector] Sprite[] spritesJogador1 = System.Array.Empty<Sprite>();
    [SerializeField, HideInInspector] Sprite[] spritesJogador2 = System.Array.Empty<Sprite>();

    Rigidbody2D corpo;
    Collider2D colisor;
    SpriteRenderer sprite;
    TextMeshPro textoNome;
    bool pediuPulo;
    bool pediuAtaque;
    bool estaAtacando;
    bool correndo;
    readonly List<Sprite> framesIdle = new();
    readonly List<Sprite> framesWalk = new();
    readonly List<Sprite> framesRun = new();
    readonly List<Sprite> framesJump = new();
    readonly List<Sprite> framesJumpFallback = new();
    readonly List<Sprite> framesAttackFront = new();
    readonly List<Sprite> framesAttackRush = new();
    List<Sprite> framesAtaqueAtivos;
    List<Sprite> framesAtivos;
    int indiceFrameAtual;
    float acumuladorAnimacao;
    bool loopAnimacaoAtual = true;

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

        ConfigurarSpritesDoTilesetSelecionado();
        AplicarFrameInicial();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        nomeRede.OnValueChanged += OnNomeMudou;
        AtualizarTextoNome(nomeRede.Value.ToString());

        if (IsServer)
        {
            string nome = GerenciadorNomesRede.PegarNomeDoClient(OwnerClientId);
            if (string.IsNullOrWhiteSpace(nome)) nome = "Jogador";
            nomeRede.Value = new FixedString64Bytes(nome);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        nomeRede.OnValueChanged -= OnNomeMudou;
        corpo.linearVelocity = Vector2.zero;
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

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            pediuAtaque = true;
        }
    }

    void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner)
        {
            if (corpo.linearVelocity != Vector2.zero)
            {
                corpo.linearVelocity = Vector2.zero;
            }
            return;
        }

        var t = Keyboard.current; if (t == null) return;
        float eixoX = (t.dKey.isPressed || t.rightArrowKey.isPressed ? 1 : 0) - (t.aKey.isPressed || t.leftArrowKey.isPressed ? 1 : 0);
        float velFinal = vel * (correndo ? multiplicadorCorrida : 1f);

        Vector2 v = corpo.linearVelocity;
        bool travarMovimento = estaAtacando && bloquearMovimentoDuranteAtaque;
        v.x = travarMovimento ? 0f : eixoX * velFinal;

        if (pediuPulo && EstaNoChao())
        {
            v.y = forcaPulo;
        }

        if (pediuAtaque && !estaAtacando)
        {
            IniciarAtaque();
        }

        corpo.linearVelocity = v;
        pediuPulo = false;
        pediuAtaque = false;
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

        float velX = corpo != null ? corpo.linearVelocity.x : 0f;
        float velY = corpo != null ? corpo.linearVelocity.y : 0f;
        float velocidadeX = Mathf.Abs(velX);
        bool estaMovendo = velocidadeX > 0.05f;
        bool estaNoChao = EstaNoChao();

        if (inverterSpritePeloMovimento)
        {
            if (velX > 0.05f) sprite.flipX = false;
            else if (velX < -0.05f) sprite.flipX = true;
        }

        if (estaAtacando && framesAtaqueAtivos != null && framesAtaqueAtivos.Count > 0)
        {
            TrocarFramesAtivos(framesAtaqueAtivos, false);
            bool terminou = AvancarAnimacao(fpsAtaque);
            if (terminou)
            {
                estaAtacando = false;
            }
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
        else if (correndo && framesRun.Count > 0)
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

    void IniciarAtaque()
    {
        if (framesAttackFront.Count == 0 && framesAttackRush.Count == 0) return;

        framesAtaqueAtivos = framesAttackFront.Count > 0 ? framesAttackFront : framesAttackRush;
        estaAtacando = framesAtaqueAtivos != null && framesAtaqueAtivos.Count > 0;
        if (!estaAtacando) return;

        TrocarFramesAtivos(framesAtaqueAtivos, false);
    }

    void ConfigurarSpritesDoTilesetSelecionado()
    {
        framesIdle.Clear();
        framesWalk.Clear();
        framesRun.Clear();
        framesJump.Clear();
        framesJumpFallback.Clear();
        framesAttackFront.Clear();
        framesAttackRush.Clear();
        framesAtaqueAtivos = null;
        estaAtacando = false;
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
            if (nome.StartsWith("idle_")) framesIdle.Add(s);
            else if (nome.StartsWith("walk_")) framesWalk.Add(s);
            else if (nome.StartsWith("run_")) framesRun.Add(s);
            else if (nome.StartsWith("jump_")) framesJump.Add(s);
            else if (nome.StartsWith("attack_slide_front_")) framesJumpFallback.Add(s);
            else if (nome.StartsWith("attack_front_")) framesAttackFront.Add(s);
            else if (nome.StartsWith("rush_attack_")) framesAttackRush.Add(s);
        }

        OrdenarFramesPorIndice(framesIdle, "idle_");
        OrdenarFramesPorIndice(framesWalk, "walk_");
        OrdenarFramesPorIndice(framesRun, "run_");
        OrdenarFramesPorIndice(framesJump, "jump_");
        OrdenarFramesPorIndice(framesJumpFallback, "attack_slide_front_");
        OrdenarFramesPorIndice(framesAttackFront, "attack_front_");
        OrdenarFramesPorIndice(framesAttackRush, "rush_attack_");
    }

    void OrdenarFramesPorIndice(List<Sprite> frames, string prefixo)
    {
        frames.Sort((a, b) =>
        {
            int ai = ExtrairIndiceDoNome(a != null ? a.name : string.Empty, prefixo);
            int bi = ExtrairIndiceDoNome(b != null ? b.name : string.Empty, prefixo);
            if (ai != bi) return ai.CompareTo(bi);
            return string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty);
        });
    }

    int ExtrairIndiceDoNome(string nome, string prefixo)
    {
        if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(prefixo)) return int.MaxValue;
        if (!nome.StartsWith(prefixo, System.StringComparison.OrdinalIgnoreCase)) return int.MaxValue;

        int inicio = prefixo.Length;
        int i = inicio;
        while (i < nome.Length && char.IsDigit(nome[i])) i++;
        if (i == inicio) return int.MaxValue;

        if (int.TryParse(nome.Substring(inicio, i - inicio), out int valor)) return valor;
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
        else if (framesAttackFront.Count > 0) sprite.sprite = framesAttackFront[0];
        else if (framesAttackRush.Count > 0) sprite.sprite = framesAttackRush[0];
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

        spritesJogador1 = ExtrairSpritesDoTileset(tilesetJogador1);
        spritesJogador2 = ExtrairSpritesDoTileset(tilesetJogador2);
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
