using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private float scrollSpeed = 1f;
    [SerializeField] private float takeoffDistance = 3f;
    [SerializeField] private float takeoffSpeed = 1.5f;
    [SerializeField] private Sprite[] cloudSprites = new Sprite[0];
    [SerializeField] private int cloudCount = 6;

    private SpriteRenderer[] tiles;
    private SpriteRenderer[] clouds;
    private float tileWidth;
    private float verticalOffset;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (backgroundSprite == null || mainCamera == null)
        {
            enabled = false;
            return;
        }

        float screenHeight = mainCamera.orthographicSize * 2f;
        float scale = (screenHeight + takeoffDistance) / backgroundSprite.bounds.size.y;
        tileWidth = backgroundSprite.bounds.size.x * scale;
        float screenWidth = screenHeight * mainCamera.aspect;
        int tileCount = Mathf.CeilToInt(screenWidth / tileWidth) + 2;
        tiles = new SpriteRenderer[tileCount];

        for (int i = 0; i < tileCount; i++)
        {
            GameObject tile = new GameObject("Background Tile");
            tile.transform.SetParent(transform);
            tile.transform.localScale = Vector3.one * scale;
            tile.transform.position = new Vector3(
                mainCamera.transform.position.x - screenWidth / 2f + (i - 0.5f) * tileWidth,
                mainCamera.transform.position.y + takeoffDistance / 2f,
                0f);

            tiles[i] = tile.AddComponent<SpriteRenderer>();
            tiles[i].sprite = backgroundSprite;
            tiles[i].sortingOrder = -100;
        }

        clouds = new SpriteRenderer[cloudSprites.Length > 0 ? cloudCount : 0];
        for (int i = 0; i < clouds.Length; i++)
        {
            GameObject cloud = new GameObject("Cloud");
            cloud.transform.SetParent(transform);
            clouds[i] = cloud.AddComponent<SpriteRenderer>();
            clouds[i].sortingOrder = -50;
            RandomizeCloud(clouds[i], false);
        }
    }

    private void Update()
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

        float deltaTime = Time.deltaTime * GameplayManager.overallSpeed;
        float nextOffset = Mathf.MoveTowards(verticalOffset, takeoffDistance, takeoffSpeed * deltaTime);
        transform.position += Vector3.down * (nextOffset - verticalOffset);
        verticalOffset = nextOffset;

        float leftEdge = mainCamera.transform.position.x - mainCamera.orthographicSize * mainCamera.aspect;

        foreach (SpriteRenderer tile in tiles)
        {
            tile.transform.position += Vector3.left * (scrollSpeed * deltaTime) * 2;
            if (tile.transform.position.x + tileWidth / 2f < leftEdge)
            {
                tile.transform.position += Vector3.right * (tiles.Length * tileWidth);
            }
        }

        foreach (SpriteRenderer cloud in clouds)
        {
            cloud.transform.position += Vector3.left * scrollSpeed * deltaTime;
            if (cloud.bounds.max.x < leftEdge)
            {
                RandomizeCloud(cloud, true);
            }
        }
    }

    private void RandomizeCloud(SpriteRenderer cloud, bool offscreen)
    {
        cloud.sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
        cloud.transform.localScale = Vector3.one * Random.Range(1.5f, 3f);
        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float x = mainCamera.transform.position.x + (offscreen
            ? halfWidth + cloud.bounds.extents.x
            : Random.Range(-halfWidth, halfWidth));
        float y = Random.Range(mainCamera.transform.position.y,
            mainCamera.transform.position.y + mainCamera.orthographicSize - cloud.bounds.extents.y);
        cloud.transform.position = new Vector3(x, y, 0f);
    }
}
