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
    [SerializeField] private float campingHazardInterval = 1.5f;

    // The comet art is drawn heading down-left at this many degrees below horizontal.
    private const float CometArtAngle = 46f;
    private const float MinCometAngle = 25f;
    private const float MaxCometAngle = 55f;
    private const float CometSpeed = 7f;
    private const float BirdSpeed = 5f;
    private const float BeanstalkSpeed = 3f;
    private const float CometWarning = 0.9f;   // seconds a comet's arrow shows before it comes into view

    private Camera mainCamera;
    private PlayerController player;
    private float spawnTimer;
    private float campingTimer;
    private WarningMarker campingArrow;

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
        if (player == null) player = FindAnyObjectByType<PlayerController>();

        // Staying in one spot too long brings hazards straight at that height until the player moves.
        if (player != null && player.IsCamping)
        {
            // An arrow at the right edge, level with the player, shows where the hazards will come from.
            Vector3 arrowAt = new Vector3(mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect - 0.6f,
                player.transform.position.y, 0f);
            if (campingArrow == null)
            {
                campingArrow = WarningMarker.Arrow(arrowAt, 0f, null, 0f);
                FloatingText.AtPlayer("Don't sit still!", FloatingText.Warning);
            }
            campingArrow.MoveTo(arrowAt);

            campingTimer -= Time.deltaTime;
            if (campingTimer <= 0f)
            {
                SpawnObstacle(player.transform.position.y);
                campingTimer = campingHazardInterval;
            }
        }
        else
        {
            campingTimer = 0f;
            if (campingArrow != null) Destroy(campingArrow.gameObject);
        }

        spawnTimer -= Time.deltaTime * GameplayManager.overallSpeed;
        if (spawnTimer > 0f) return;

        SpawnObstacle(null);
        spawnTimer = Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    // targetY sends an airborne hazard (bird or comet) at that height; otherwise any obstacle at random.
    private void SpawnObstacle(float? targetY)
    {
        int kind = Random.Range(0, targetY.HasValue ? 2 : 3);
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
            spriteRenderer.sprite = AdBox.Create(obstacle, sprite.bounds.size * scale, DataProfile.PersonalizedHeadline());
        }
        else
        {
            spriteRenderer.sprite = sprite;
            obstacle.transform.localScale = Vector3.one * scale;
        }
        Vector2 direction = Vector2.left;
        if (comet)
        {
            direction = AimComet(obstacle.transform, spriteRenderer.bounds.extents.magnitude, !ad, targetY);
            WarnOfComet(obstacle.transform, spriteRenderer, direction);
        }
        else
        {
            float halfHeight = spriteRenderer.bounds.extents.y;
            float lowest = mainCamera.transform.position.y - mainCamera.orthographicSize + halfHeight;
            float highest = mainCamera.transform.position.y + mainCamera.orthographicSize - halfHeight - 0.5f;
            float y = !airborne ? lowest
                : targetY.HasValue ? Mathf.Clamp(targetY.Value, lowest + 0.5f, highest)
                : Random.Range(lowest + 0.5f, highest);
            float x = mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect
                + spriteRenderer.bounds.extents.x + 0.1f;
            obstacle.transform.position = new Vector3(x, y, 0f);
        }

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

        MovingCollider.AddBody(obstacle);
        Obstacle movement = obstacle.AddComponent<Obstacle>();
        movement.speed = comet ? CometSpeed : bird ? BirdSpeed : BeanstalkSpeed;
        movement.direction = direction;
        if (bird && !ad) movement.frames = birdFrames;
    }

    // Puts a blinking arrow where the comet will come in, pointing back at it, and starts the comet a bit
    // further back along its path so the arrow shows for a moment before it arrives.
    private void WarnOfComet(Transform comet, SpriteRenderer spriteRenderer, Vector2 direction)
    {
        Vector3 cameraPosition = mainCamera.transform.position;
        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float top = cameraPosition.y + mainCamera.orthographicSize;
        // It starts just above the top edge; follow its path down to the edge to find where it enters.
        float down = Mathf.Max(0.01f, -direction.y);
        float entryX = comet.position.x + direction.x * (comet.position.y - top) / down;
        Vector3 arrowAt = new Vector3(Mathf.Clamp(entryX, cameraPosition.x - halfWidth + 0.6f, cameraPosition.x + halfWidth - 0.6f),
            top - 0.6f, 0f);
        float pointing = Mathf.Atan2(-direction.y, -direction.x) * Mathf.Rad2Deg;
        WarningMarker.Arrow(arrowAt, pointing, spriteRenderer, CometWarning + 2f);
        comet.position -= (Vector3)(direction * CometSpeed * GameplayManager.overallSpeed * CometWarning);
    }

    // Places a comet just above the top right of the screen and returns its fall direction: a
    // diagonal that crosses the player's column somewhere the player can be (or at targetY), so it
    // can still be dodged.
    private Vector2 AimComet(Transform comet, float radius, bool alignArt, float? targetY)
    {
        Vector3 cameraPosition = mainCamera.transform.position;
        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float playerX = player != null ? player.transform.position.x : cameraPosition.x - halfWidth + 2f;
        float reach = mainCamera.orthographicSize - 1f;

        float x = Random.Range(cameraPosition.x, cameraPosition.x + halfWidth);
        float y = cameraPosition.y + mainCamera.orthographicSize + radius;
        float run = x - playerX;
        float minAngle = Mathf.Max(MinCometAngle, Mathf.Atan2(y - (cameraPosition.y + reach), run) * Mathf.Rad2Deg);
        float maxAngle = Mathf.Min(MaxCometAngle, Mathf.Atan2(y - (cameraPosition.y - reach), run) * Mathf.Rad2Deg);
        float angle = targetY.HasValue
            ? Mathf.Atan2(y - targetY.Value, run) * Mathf.Rad2Deg
            : Random.Range(minAngle, maxAngle);

        comet.position = new Vector3(x, y, 0f);
        // Turn the art so its tail trails straight behind the way it is falling.
        if (alignArt) comet.rotation = Quaternion.Euler(0f, 0f, angle - CometArtAngle);
        return new Vector2(-Mathf.Cos(angle * Mathf.Deg2Rad), -Mathf.Sin(angle * Mathf.Deg2Rad));
    }
}
