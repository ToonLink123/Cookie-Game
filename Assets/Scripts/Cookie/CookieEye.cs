using UnityEngine;

// A blinking eye drawn in code, shown on third-party (tracker) cookies. The pupil follows the player.
public class CookieEye : MonoBehaviour
{
    private const int EyeWidth = 64;
    private const int EyeHeight = 52;
    private const int PupilSize = 24;
    private const float PixelsPerUnit = 64f;
    private const float OutlineWidth = 4f;
    private const float PupilRangeY = 0.12f;
    private const float BlinkDuration = 0.18f;

    // Eye width as a fraction of the cookie's width, and how far the pupil can look sideways.
    public const float SizeOnCookie = 0.6f;
    public const float PupilRangeX = 0.2f;

    private static readonly Color Ink = new Color(0.06f, 0.10f, 0.16f);
    private static Sprite eyeSprite;
    private static Sprite pupilSprite;

    public static Sprite EyeSprite => eyeSprite != null ? eyeSprite : eyeSprite = DrawEye();
    public static Sprite PupilSprite => pupilSprite != null ? pupilSprite : pupilSprite = DrawPupil();

    private Transform pupil;
    private Transform target;
    private float size;
    private float nextBlink;

    public static void Attach(SpriteRenderer cookie, Transform target)
    {
        GameObject eyeObject = new GameObject("Tracker Eye");
        eyeObject.transform.SetParent(cookie.transform, false);
        CookieEye eye = eyeObject.AddComponent<CookieEye>();
        eye.target = target;
        eye.size = cookie.sprite.bounds.size.x * SizeOnCookie;
        eye.nextBlink = Random.Range(0.5f, 2f);
        eye.transform.localScale = Vector3.one * eye.size;

        AddLayer(eyeObject.transform, "Eye White", EyeSprite, cookie, 1);
        eye.pupil = AddLayer(eyeObject.transform, "Pupil", PupilSprite, cookie, 2);
    }

    void Update()
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

        if (target != null)
        {
            Vector2 look = ((Vector2)(target.position - transform.position)).normalized;
            pupil.localPosition = new Vector3(look.x * PupilRangeX, look.y * PupilRangeY, 0f);
        }

        nextBlink -= Time.deltaTime;
        float blink = -nextBlink / BlinkDuration;
        if (blink >= 1f)
        {
            nextBlink = Random.Range(1f, 3f);
            blink = 0f;
        }
        float openness = blink > 0f ? 1f - 0.92f * Mathf.Sin(blink * Mathf.PI) : 1f;
        transform.localScale = new Vector3(size, size * openness, 1f);
    }

    private static Sprite DrawEye()
    {
        Texture2D texture = NewTexture(EyeWidth, EyeHeight);
        Vector2 center = new Vector2(EyeWidth, EyeHeight) / 2f;
        Vector2 radius = center - Vector2.one;
        for (int y = 0; y < EyeHeight; y++)
        {
            for (int x = 0; x < EyeWidth; x++)
            {
                Vector2 offset = new Vector2(x + 0.5f, y + 0.5f) - center;
                // Rough distance in pixels outside the ellipse edge (negative inside).
                float edge = (new Vector2(offset.x / radius.x, offset.y / radius.y).magnitude - 1f) * radius.y;
                Color color = Color.Lerp(Ink, Color.white, Mathf.Clamp01(-edge - OutlineWidth + 0.5f));
                color.a = Mathf.Clamp01(0.5f - edge);
                texture.SetPixel(x, y, color);
            }
        }
        return ToSprite(texture);
    }

    private static Sprite DrawPupil()
    {
        Texture2D texture = NewTexture(PupilSize, PupilSize);
        float radius = PupilSize / 2f - 1f;
        Vector2 center = Vector2.one * PupilSize / 2f;
        Vector2 shine = center + Vector2.one * radius * 0.35f;
        for (int y = 0; y < PupilSize; y++)
        {
            for (int x = 0; x < PupilSize; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                float edge = Vector2.Distance(point, center) - radius;
                float highlight = Mathf.Clamp01(0.5f - (Vector2.Distance(point, shine) - radius * 0.3f));
                Color color = Color.Lerp(Ink, Color.white, highlight);
                color.a = Mathf.Clamp01(0.5f - edge);
                texture.SetPixel(x, y, color);
            }
        }
        return ToSprite(texture);
    }

    private static Texture2D NewTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private static Sprite ToSprite(Texture2D texture)
    {
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private static Transform AddLayer(Transform eye, string name, Sprite sprite, SpriteRenderer cookie, int order)
    {
        GameObject layer = new GameObject(name);
        layer.transform.SetParent(eye, false);
        SpriteRenderer spriteRenderer = layer.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sharedMaterial = cookie.sharedMaterial;
        spriteRenderer.sortingLayerID = cookie.sortingLayerID;
        spriteRenderer.sortingOrder = cookie.sortingOrder + order;
        return layer.transform;
    }
}
