using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class BlocoColisaoMapa : MonoBehaviour
{
    [SerializeField] bool usarCorpoEstatico = true;

    void Reset()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = false;
    }

    void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = false;

        if (!usarCorpoEstatico) return;

        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;
    }
}
