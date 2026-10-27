using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private float scrollSpeed = 1f;
    [SerializeField] private float takeoffDistance = 3f;
    [SerializeField] private float takeoffSpeed = 1.5f;
    [SerializeField] private Sprite[] cloudSprites = new Sprite[0];
    [SerializeField] private int cloudCount = 6;
    [SerializeField] private int trackerHitsForAllAdClouds = 10;
    [SerializeField] private float minMenuDrift = 0.3f;   // home screen cloud speeds, units per second
    [SerializeField] private float maxMenuDrift = 1f;

    // Ads in the sky are a bit see-through, so they read as far away rather than as obstacles. They don't
    // take the sky's colour either, so at night they glow like neon signs.
    private static readonly Color AdCloudTint = new Color(1f, 1f, 1f, 0.7f);

    // Far hills: a hazy copy of the background's own hills, peeking over them and scrolling slower.
    private static readonly Color FarHillsTint = new Color(0.55f, 0.75f, 0.95f);
    private const float FarHillsLift = 0.9f;
    private const float FarHillsSpeed = 1.3f;   // the near hills scroll at 2
    private const float NearHillsSpeed = 2f;

    // The sky through the run: day, dusk, night, then night lit by the ads, each 250 m after the last,
    // before brightening back into day and going round again.
    private const float SkyStageMeters = 250f;
    private static readonly Color[] SkyTints =
    {
        Color.white, new Color(1f, 0.8f, 0.7f), new Color(0.42f, 0.46f, 0.8f), new Color(0.45f, 0.36f, 0.85f),
    };

    private SpriteRenderer[] tiles;
    private SpriteRenderer[] farTiles = new SpriteRenderer[0];
    private SpriteRenderer[] clouds;
    private float[] menuDrift;   // per cloud; negative drifts left
    private float tileWidth;
    private float verticalOffset;
    private Camera mainCamera;
    private Color skyColor;
    private bool skyCutOut;

    private void Start()
    {
        mainCamera = Camera.main;
        if (backgroundSprite == null || mainCamera == null)
        {
            enabled = false;
            return;
        }

        // With the flat sky cut out of the background, the camera paints the sky and the far hills can sit
        // between the two. If the texture can't be read, the background is used as it is, without them.
        Sprite ground = SkyCutOut(backgroundSprite, out skyColor);
        skyCutOut = ground != null;
        if (skyCutOut)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = skyColor;
        }

        float screenHeight = mainCamera.orthographicSize * 2f;
        float scale = (screenHeight + takeoffDistance) / backgroundSprite.bounds.size.y;
        tileWidth = backgroundSprite.bounds.size.x * scale;
        tiles = MakeTiles("Background Tile", skyCutOut ? ground : backgroundSprite, scale, 0f, -100);
        if (skyCutOut) farTiles = MakeTiles("Far Hills", ground, scale, FarHillsLift, -110);

        clouds = new SpriteRenderer[cloudSprites.Length > 0 ? cloudCount : 0];
        menuDrift = new float[clouds.Length];
        for (int i = 0; i < clouds.Length; i++)
        {
            GameObject cloud = new GameObject("Cloud");
            cloud.transform.SetParent(transform);
            clouds[i] = cloud.AddComponent<SpriteRenderer>();
            clouds[i].sortingOrder = -50;
            RandomizeCloud(clouds[i], false);
            menuDrift[i] = Random.Range(minMenuDrift, maxMenuDrift) * (Random.value < 0.5f ? -1f : 1f);
        }
        ApplySky(0f);
    }

    private SpriteRenderer[] MakeTiles(string name, Sprite sprite, float scale, float lift, int sortingOrder)
    {
        float screenWidth = mainCamera.orthographicSize * 2f * mainCamera.aspect;
        SpriteRenderer[] made = new SpriteRenderer[Mathf.CeilToInt(screenWidth / tileWidth) + 2];
        for (int i = 0; i < made.Length; i++)
        {
            GameObject tile = new GameObject(name);
            tile.transform.SetParent(transform);
            tile.transform.localScale = Vector3.one * scale;
            tile.transform.position = new Vector3(
                mainCamera.transform.position.x - screenWidth / 2f + (i - 0.5f) * tileWidth,
                mainCamera.transform.position.y + takeoffDistance / 2f + lift,
                0f);
            made[i] = tile.AddComponent<SpriteRenderer>();
            made[i].sprite = sprite;
            made[i].sortingOrder = sortingOrder;
        }
        return made;
    }

    // A copy of the background with its flat sky colour made see-through (needs Read/Write on the texture).
    private static Sprite SkyCutOut(Sprite sprite, out Color sky)
    {
        sky = Color.white;
        Texture2D source = sprite.texture;
        if (!source.isReadable) return null;

        Rect area = sprite.textureRect;
        int width = (int)area.width, height = (int)area.height;
        Color[] pixels = source.GetPixels((int)area.x, (int)area.y, width, height);
        sky = pixels[(height - 1) * width];   // the top-left corner is always sky
        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];
            if (Mathf.Abs(pixel.r - sky.r) + Mathf.Abs(pixel.g - sky.g) + Mathf.Abs(pixel.b - sky.b) < 0.06f)
                pixels[i] = new Color(sky.r, sky.g, sky.b, 0f);
        }
        Texture2D cut = new Texture2D(width, height, TextureFormat.RGBA32, false);
        cut.filterMode = source.filterMode;
        cut.wrapMode = TextureWrapMode.Clamp;
        cut.SetPixels(pixels);
        cut.Apply();
        return Sprite.Create(cut, new Rect(0f, 0f, width, height),
            new Vector2(sprite.pivot.x / area.width, sprite.pivot.y / area.height), sprite.pixelsPerUnit);
    }

    private void Update()
    {
        if (GameplayManager.currentState == GameState.MainMenu)
        {
            DriftClouds();
            return;
        }
        if (GameplayManager.currentState != GameState.Gameplay) return;

        float deltaTime = Time.deltaTime * GameplayManager.overallSpeed;
        float nextOffset = Mathf.MoveTowards(verticalOffset, takeoffDistance, takeoffSpeed * deltaTime);
        transform.position += Vector3.down * (nextOffset - verticalOffset);
        verticalOffset = nextOffset;

        float leftEdge = mainCamera.transform.position.x - mainCamera.orthographicSize * mainCamera.aspect;
        Scroll(tiles, scrollSpeed * NearHillsSpeed * deltaTime, leftEdge);
        Scroll(farTiles, scrollSpeed * FarHillsSpeed * deltaTime, leftEdge);

        foreach (SpriteRenderer cloud in clouds)
        {
            cloud.transform.position += Vector3.left * scrollSpeed * deltaTime;
            if (cloud.bounds.max.x < leftEdge)
            {
                RandomizeCloud(cloud, true);
            }
        }
        ApplySky(GameplayManager.metersTraveled);
    }

    private void Scroll(SpriteRenderer[] row, float distance, float leftEdge)
    {
        foreach (SpriteRenderer tile in row)
        {
            tile.transform.position += Vector3.left * distance;
            if (tile.transform.position.x + tileWidth / 2f < leftEdge)
            {
                tile.transform.position += Vector3.right * (row.Length * tileWidth);
            }
        }
    }

    private void ApplySky(float meters)
    {
        Color tint = SkyTint(meters);
        foreach (SpriteRenderer tile in tiles) tile.color = tint;
        foreach (SpriteRenderer tile in farTiles) tile.color = FarHillsTint * tint;
        foreach (SpriteRenderer cloud in clouds)
            cloud.color = System.Array.IndexOf(cloudSprites, cloud.sprite) < 0 ? AdCloudTint : tint;   // ads aren't cloud sprites
        if (skyCutOut) mainCamera.backgroundColor = skyColor * tint;
    }

    private static Color SkyTint(float meters)
    {
        float stage = Mathf.Repeat(meters / SkyStageMeters, SkyTints.Length);
        int from = Mathf.FloorToInt(stage);
        return Color.Lerp(SkyTints[from], SkyTints[(from + 1) % SkyTints.Length], stage - from);
    }

    // Home screen: each cloud glides its own way at its own pace, wrapping around to the other side.
    private void DriftClouds()
    {
        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float left = mainCamera.transform.position.x - halfWidth;
        float right = mainCamera.transform.position.x + halfWidth;
        for (int i = 0; i < clouds.Length; i++)
        {
            Transform cloud = clouds[i].transform;
            cloud.position += Vector3.right * menuDrift[i] * Time.deltaTime;
            Bounds bounds = clouds[i].bounds;
            float wrap = right - left + bounds.size.x;
            if (menuDrift[i] > 0f && bounds.min.x > right) cloud.position += Vector3.left * wrap;
            else if (menuDrift[i] < 0f && bounds.max.x < left) cloud.position += Vector3.right * wrap;
        }
    }

    private void RandomizeCloud(SpriteRenderer cloud, bool offscreen)
    {
        // If this cloud was an ad last time around, free that ad's drawing.
        AdBox oldAd = cloud.GetComponent<AdBox>();
        if (oldAd != null) Destroy(oldAd);

        cloud.sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
        cloud.transform.localScale = Vector3.one * Random.Range(1.5f, 3f);
        // Each tracker cookie eaten makes it 10% likelier this cloud comes back as an ad of the same size.
        if (Random.Range(0, trackerHitsForAllAdClouds) < GameplayManager.trackerCookiesHit)
        {
            Vector2 size = cloud.bounds.size;
            cloud.transform.localScale = Vector3.one;
            cloud.sprite = AdBox.Create(cloud.gameObject, size, DataProfile.PersonalizedHeadline());
        }

        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float x = mainCamera.transform.position.x + (offscreen
            ? halfWidth + cloud.bounds.extents.x
            : Random.Range(-halfWidth, halfWidth));
        float y = Random.Range(mainCamera.transform.position.y,
            mainCamera.transform.position.y + mainCamera.orthographicSize - cloud.bounds.extents.y);
        cloud.transform.position = new Vector3(x, y, 0f);
    }
}
