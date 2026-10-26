using UnityEngine;

// One square pixel that drifts, shrinks and fades away, then removes itself.
public class PixelParticle : MonoBehaviour
{
    private static Sprite pixelSprite;

    private SpriteRenderer spriteRenderer;
    private Vector3 velocity;
    private Color color;
    private float size;
    private float lifetime;
    private float age;

    public static void Spawn(Vector3 position, Vector2 velocity, float size, Color color, float lifetime,
        int sortingOrder)
    {
        if (pixelSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.filterMode = FilterMode.Point;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        GameObject pixel = new GameObject("Pixel Particle");
        pixel.transform.position = position;
        pixel.transform.localScale = Vector3.one * size;
        SpriteRenderer spriteRenderer = pixel.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = pixelSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;

        PixelParticle particle = pixel.AddComponent<PixelParticle>();
        particle.spriteRenderer = spriteRenderer;
        particle.velocity = velocity;
        particle.color = color;
        particle.size = size;
        particle.lifetime = lifetime;
    }

    void Update()
    {
        if (GameplayManager.currentState == GameState.Paused) return;

        float deltaTime = Time.deltaTime * GameplayManager.overallSpeed;
        age += deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float remaining = 1f - age / lifetime;
        transform.position += velocity * deltaTime;
        transform.localScale = Vector3.one * size * Mathf.Lerp(0.6f, 1f, remaining);
        spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * Mathf.Sqrt(remaining));
    }
}
