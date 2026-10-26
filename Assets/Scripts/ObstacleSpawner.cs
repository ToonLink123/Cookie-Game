using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private Sprite[] beanstalkSprites = new Sprite[0];
    [SerializeField] private Sprite[] birdFrames = new Sprite[0];
    [SerializeField] private Sprite cometSprite;
    [SerializeField] public float minSpawnDelay = 8f;
    [SerializeField] public float maxSpawnDelay = 14f;
    [SerializeField] private int trackerHitsForAllAds = 10;

    private Camera mainCamera;
    private float spawnTimer;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null || beanstalkSprites.Length == 0 || birdFrames.Length == 0 || cometSprite == null)
        {
            enabled = false;
            return;
        }

        spawnTimer = Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    void Update()
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

        spawnTimer -= Time.deltaTime * GameplayManager.overallSpeed;
        if (spawnTimer > 0f) return;

        SpawnObstacle();
        spawnTimer = Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    private void SpawnObstacle()
    {
        int kind = Random.Range(0, 3);
        bool bird = kind == 0;
        bool comet = kind == 1;
        bool airborne = bird || comet;
        // Each tracker cookie eaten makes it likelier this obstacle shows up as an ad of the same size.
        bool ad = Random.Range(0, trackerHitsForAllAds) < GameplayManager.trackerCookiesHit;
        Sprite sprite = bird ? birdFrames[0]
            : comet ? cometSprite : beanstalkSprites[Random.Range(0, beanstalkSprites.Length)];
        GameObject obstacle = new GameObject(ad ? "Ad" : bird ? "Bird" : comet ? "Comet" : "Beanstalk");
        SpriteRenderer spriteRenderer = obstacle.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 1;

        float height = bird ? 1.2f : comet ? 1.8f : Random.Range(3f, 6f);
        float scale = height / sprite.bounds.size.y;
        if (ad)
        {
            spriteRenderer.sprite = AdBox.Create(obstacle, sprite.bounds.size * scale);
        }
        else
        {
            spriteRenderer.sprite = sprite;
            obstacle.transform.localScale = Vector3.one * scale;
        }
        float halfHeight = spriteRenderer.bounds.extents.y;
        float y = airborne
            ? Random.Range(mainCamera.transform.position.y - mainCamera.orthographicSize + halfHeight + 0.5f,
                mainCamera.transform.position.y + mainCamera.orthographicSize - halfHeight - 0.5f)
            : mainCamera.transform.position.y - mainCamera.orthographicSize + halfHeight;
        float x = mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect
            + spriteRenderer.bounds.extents.x + 0.1f;
        obstacle.transform.position = new Vector3(x, y, 0f);

        if (bird || ad)
        {
            BoxCollider2D collider = obstacle.AddComponent<BoxCollider2D>();
            collider.size = spriteRenderer.sprite.bounds.size;
            collider.isTrigger = true;
        }
        else
        {
            PolygonCollider2D collider = obstacle.AddComponent<PolygonCollider2D>();
            collider.isTrigger = true;
            collider.pathCount = sprite.GetPhysicsShapeCount();
            List<Vector2> points = new List<Vector2>();
            for (int i = 0; i < collider.pathCount; i++)
            {
                sprite.GetPhysicsShape(i, points);
                collider.SetPath(i, points);
            }
        }

        Obstacle movement = obstacle.AddComponent<Obstacle>();
        movement.speed = comet ? 6f : bird ? 5f : 3f;
        if (bird && !ad) movement.frames = birdFrames;
    }
}
