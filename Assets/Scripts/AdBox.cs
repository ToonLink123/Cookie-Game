using UnityEngine;

// An obstacle drawn as a pop-up ad. Owns its generated texture so it is freed with the obstacle.
public class AdBox : MonoBehaviour
{
    // 54 texels per unit keeps the pixel art crisp at 1080p (2 screen pixels per texel).
    private const float PixelsPerUnit = 54f;

    private Texture2D texture;
    private Sprite sprite;

    // Draws a random ad of the given world size onto the obstacle and returns its sprite.
    public static Sprite Create(GameObject obstacle, Vector2 size)
    {
        int width = Mathf.RoundToInt(size.x * PixelsPerUnit);
        int height = Mathf.RoundToInt(size.y * PixelsPerUnit);
        AdPainter painter = new AdPainter(width, height, Random.Range(int.MinValue, int.MaxValue));

        AdBox ad = obstacle.AddComponent<AdBox>();
        ad.texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        ad.texture.filterMode = FilterMode.Point;
        ad.texture.wrapMode = TextureWrapMode.Clamp;
        ad.texture.SetPixels32(painter.Paint(Random.Range(0, AdPainter.DesignCount)));
        ad.texture.Apply();
        ad.sprite = Sprite.Create(ad.texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        return ad.sprite;
    }

    void OnDestroy()
    {
        Destroy(sprite);
        Destroy(texture);
    }
}
