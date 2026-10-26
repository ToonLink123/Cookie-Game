using UnityEngine;

// The "Clear Browsing Data" pickup: a green box that flies at the player, trailing green pixels.
// Grabbing it restores full speed, resets the ads, and erases the slowdown saved for next game.
public class ClearDataPickup : MonoBehaviour
{
    private const float Speed = 4f;
    private const float BobHeight = 0.15f;
    private const float TrailPixelsPerSecond = 70f;
    private const int ArtPixels = 24;   // width of the sprite in pixels

    private static readonly Color[] TrailColors =
    {
        new Color(0.24f, 0.78f, 0.35f),
        new Color(0.59f, 0.94f, 0.59f),
        new Color(0.12f, 0.51f, 0.24f),
        new Color(0.9f, 1f, 0.78f),
    };

    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private float baseY;
    private float age;
    private float trailTimer;
    private bool collected;

    // Sends a pickup in from the right edge at height y, size units wide.
    public static void Spawn(float y, float size)
    {
        Sprite sprite = Resources.Load<Sprite>("ClearBrowsingData");
        GameObject pickup = new GameObject("Clear Browsing Data");
        SpriteRenderer spriteRenderer = pickup.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 3;
        pickup.transform.localScale = Vector3.one * (size / sprite.bounds.size.x);
        BoxCollider2D collider = pickup.AddComponent<BoxCollider2D>();
        collider.size = sprite.bounds.size;
        collider.isTrigger = true;

        Camera camera = Camera.main;
        float x = camera.transform.position.x + camera.orthographicSize * camera.aspect + size;
        pickup.transform.position = new Vector3(x, y, 0f);
        pickup.AddComponent<ClearDataPickup>().baseY = y;
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

        float deltaTime = Time.deltaTime * GameplayManager.overallSpeed;
        age += deltaTime;
        transform.position = new Vector3(transform.position.x - Speed * deltaTime,
            baseY + Mathf.Sin(age * 4f) * BobHeight, 0f);

        // Shed green pixels off the back edge; left behind as the box moves on, they streak into a trail.
        trailTimer += deltaTime * TrailPixelsPerSecond;
        for (; trailTimer >= 1f; trailTimer--) EmitTrailPixel();

        float leftEdge = mainCamera.transform.position.x - mainCamera.orthographicSize * mainCamera.aspect;
        if (spriteRenderer.bounds.max.x < leftEdge) Destroy(gameObject);
    }

    private void EmitTrailPixel()
    {
        Bounds bounds = spriteRenderer.bounds;
        float artPixel = bounds.size.x / ArtPixels;
        Vector3 position = new Vector3(bounds.max.x - artPixel * 2f,
            Random.Range(bounds.min.y + artPixel * 2f, bounds.max.y - artPixel * 2f), 0f);
        Vector2 drift = new Vector2(Random.Range(0.8f, 2f), Random.Range(-0.25f, 0.25f));
        PixelParticle.Spawn(position, drift, artPixel * Random.Range(1, 4),
            TrailColors[Random.Range(0, TrailColors.Length)], Random.Range(0.5f, 0.9f), 2);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || GameplayManager.currentState != GameState.Gameplay) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;
        collected = true;
        player.ClearBrowsingData();
        CookieBurst.Spawn(spriteRenderer);
        Destroy(gameObject);
    }
}
