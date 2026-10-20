using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private float scrollSpeed = 1f;

    private SpriteRenderer[] tiles;
    private float tileWidth;
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
        float scale = screenHeight / backgroundSprite.bounds.size.y;
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
                mainCamera.transform.position.y,
                0f);

            tiles[i] = tile.AddComponent<SpriteRenderer>();
            tiles[i].sprite = backgroundSprite;
            tiles[i].sortingOrder = -100;
        }
    }

    private void Update()
    {
        float leftEdge = mainCamera.transform.position.x - mainCamera.orthographicSize * mainCamera.aspect;

        foreach (SpriteRenderer tile in tiles)
        {
            tile.transform.position += Vector3.left * (scrollSpeed * Time.deltaTime) * 2;
            if (tile.transform.position.x + tileWidth / 2f < leftEdge)
            {
                tile.transform.position += Vector3.right * (tiles.Length * tileWidth);
            }
        }
    }
}
