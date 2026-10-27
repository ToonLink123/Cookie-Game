using System.Collections.Generic;
using UnityEngine;

// One square pixel that drifts, shrinks and fades away. The burner flame and the Clear Browsing Data trail
// put out well over a hundred a second, so finished pixels are switched off and reused instead of being
// destroyed and made again, and they all live under one holder rather than being parented to what they
// ride on (both kept the game churning through objects).
public class PixelParticle : MonoBehaviour
{
    private static Sprite pixelSprite;
    private static Transform holder;
    private static readonly Stack<PixelParticle> spare = new Stack<PixelParticle>();

    private SpriteRenderer spriteRenderer;
    private Transform anchor;
    private Vector3 offset;   // from the anchor, while riding on one
    private Vector3 velocity;
    private Color color;
    private float size;
    private float lifetime;
    private float age;

    // With a parent, the pixel rides along with it (a flame stays on its burner) while still drifting.
    public static void Spawn(Vector3 position, Vector2 velocity, float size, Color color, float lifetime,
        int sortingOrder, Transform parent = null)
    {
        PixelParticle particle = Take();
        particle.anchor = parent;
        particle.offset = parent != null ? position - parent.position : Vector3.zero;
        particle.velocity = velocity;
        particle.color = color;
        particle.size = parent != null ? size * parent.lossyScale.x : size;   // as big as it was as a child
        particle.lifetime = lifetime;
        particle.age = 0f;
        particle.transform.position = position;
        particle.transform.localScale = Vector3.one * particle.size;
        particle.spriteRenderer.color = color;
        particle.spriteRenderer.sortingOrder = sortingOrder;
        particle.gameObject.SetActive(true);
    }

    private static PixelParticle Take()
    {
        // Loading the scene again destroys the holder along with every pixel, so start a fresh supply.
        if (holder == null)
        {
            holder = new GameObject("Pixel Particles").transform;
            spare.Clear();
        }
        if (spare.Count > 0) return spare.Pop();

        if (pixelSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.filterMode = FilterMode.Point;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }
        GameObject pixel = new GameObject("Pixel Particle");
        pixel.transform.SetParent(holder, false);
        PixelParticle particle = pixel.AddComponent<PixelParticle>();
        particle.spriteRenderer = pixel.AddComponent<SpriteRenderer>();
        particle.spriteRenderer.sprite = pixelSprite;
        return particle;
    }

    void Update()
    {
        if (GameplayManager.currentState == GameState.Paused) return;

        float deltaTime = Time.deltaTime * GameplayManager.overallSpeed;
        age += deltaTime;
        if (age >= lifetime)
        {
            gameObject.SetActive(false);
            spare.Push(this);
            return;
        }

        float remaining = 1f - age / lifetime;
        if (anchor != null)
        {
            offset += velocity * deltaTime;
            transform.position = anchor.position + offset;
        }
        else
        {
            transform.position += velocity * deltaTime;
        }
        transform.localScale = Vector3.one * size * Mathf.Lerp(0.6f, 1f, remaining);
        spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * Mathf.Sqrt(remaining));
    }
}
