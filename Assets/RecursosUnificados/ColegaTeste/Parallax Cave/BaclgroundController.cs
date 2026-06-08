using UnityEngine;

public class Parallax : MonoBehaviour
{
    private float startPosX;
    private float startPosY;
    private float length;

    public GameObject cam;
    public float parallaxEffectX = 0.5f;
    public float parallaxEffectY = 0.3f;

    void Awake()
    {
        startPosX = transform.position.x;
        startPosY = transform.position.y;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void FixedUpdate()
    {
        float distanceX = cam.transform.position.x * parallaxEffectX;
        float distanceY = cam.transform.position.y * parallaxEffectY;

        float movementX = cam.transform.position.x * (1 - parallaxEffectX);

        transform.position = new Vector3(
            startPosX + distanceX,
            startPosY + distanceY,
            transform.position.z
        );

        // Loop infinito horizontal
        if (movementX > startPosX + length)
            startPosX += length;
        else if (movementX < startPosX - length)
            startPosX -= length;
    }
}