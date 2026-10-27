using UnityEngine;

// A blinking warning in the world: the red arrow pointing at something about to arrive, or the
// tracker's eye in red marking where a tracker is going to cross. Made from existing sprites.
public class WarningMarker : MonoBehaviour
{
    private static readonly Color Red = new Color(1f, 0.25f, 0.2f);
    private const float BlinkSpeed = 12f;
    private const int SortingOrder = 20;

    private SpriteRenderer[] renderers;
    private Renderer watching;       // goes away once this is on screen (or gone)
    private bool watchingSomething;
    private float lifetime;
    private float age;

    // An arrow at position pointing the given way (degrees; 0 = right, 90 = up).
    public static WarningMarker Arrow(Vector3 position, float angle, Renderer watch, float lifetime)
    {
        WarningMarker marker = New("Warning Arrow", position, lifetime, watch);
        marker.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        marker.transform.localScale = Vector3.one * 2.5f;
        marker.renderers = new[] { marker.Layer(marker.transform, UiKit.Arrow, 0) };
        return marker;
    }

    // The tracker's eye, tinted red, staring at the spot it is aiming for.
    public static WarningMarker Eye(Vector3 position, float lifetime)
    {
        WarningMarker marker = New("Tracker Target", position, lifetime, null);
        marker.transform.localScale = Vector3.one * 0.55f;
        marker.renderers = new[]
        {
            marker.Layer(marker.transform, CookieEye.EyeSprite, 0),
            marker.Layer(marker.transform, CookieEye.PupilSprite, 1),
        };
        return marker;
    }

    private static WarningMarker New(string name, Vector3 position, float lifetime, Renderer watch)
    {
        GameObject marker = new GameObject(name);
        marker.transform.position = position;
        WarningMarker warning = marker.AddComponent<WarningMarker>();
        warning.lifetime = lifetime;
        warning.watching = watch;
        warning.watchingSomething = watch != null;
        return warning;
    }

    private SpriteRenderer Layer(Transform parent, Sprite sprite, int order)
    {
        GameObject layer = new GameObject("Layer");
        layer.transform.SetParent(parent, false);
        SpriteRenderer spriteRenderer = layer.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Red;
        spriteRenderer.sortingOrder = SortingOrder + order;
        return spriteRenderer;
    }

    public void MoveTo(Vector3 position)
    {
        transform.position = position;
    }

    void Update()
    {
        if (GameplayManager.currentState == GameState.Paused) return;

        age += Time.deltaTime;
        bool arrived = watchingSomething && (watching == null || OnScreen(watching.bounds));
        if (arrived || (lifetime > 0f && age >= lifetime) || GameplayManager.currentState == GameState.GameOver)
        {
            Destroy(gameObject);
            return;
        }

        float alpha = Mathf.Lerp(0.35f, 1f, 0.5f + 0.5f * Mathf.Sin(age * BlinkSpeed));
        foreach (SpriteRenderer spriteRenderer in renderers)
            spriteRenderer.color = new Color(Red.r, Red.g, Red.b, alpha);
    }

    private static bool OnScreen(Bounds bounds)
    {
        Camera camera = Camera.main;
        float halfHeight = camera.orthographicSize, halfWidth = halfHeight * camera.aspect;
        Vector3 middle = camera.transform.position;
        return bounds.max.x > middle.x - halfWidth && bounds.min.x < middle.x + halfWidth
            && bounds.max.y > middle.y - halfHeight && bounds.min.y < middle.y + halfHeight;
    }
}
