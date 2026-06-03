using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class Movimentacao : MonoBehaviour
{
    [SerializeField] float vel = 5f;
    Rigidbody2D corpo;
    void Awake() => corpo = GetComponent<Rigidbody2D>();
    void FixedUpdate()
    {
        var t = Keyboard.current; if (t == null) return;
        var eixo = new Vector2(
            (t.dKey.isPressed || t.rightArrowKey.isPressed ? 1 : 0) - (t.aKey.isPressed || t.leftArrowKey.isPressed ? 1 : 0),
            (t.wKey.isPressed || t.upArrowKey.isPressed ? 1 : 0) - (t.sKey.isPressed || t.downArrowKey.isPressed ? 1 : 0));
        corpo.linearVelocity = eixo.normalized * vel;
    }
}
