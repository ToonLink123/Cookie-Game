using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [HideInInspector] public float speed = 3f;
    [HideInInspector] public Sprite[] frames;

    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private float animationTime;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

        float deltaTime = Time.deltaTime * GameplayManager.overallSpeed;
        transform.position += Vector3.left * speed * deltaTime;
        if (frames != null && frames.Length > 0)
        {
            animationTime += deltaTime;
            spriteRenderer.sprite = frames[(int)(animationTime * 12f) % frames.Length];
        }

        float leftEdge = mainCamera.transform.position.x - mainCamera.orthographicSize * mainCamera.aspect;
        if (spriteRenderer.bounds.max.x < leftEdge) Destroy(gameObject);
    }
}
