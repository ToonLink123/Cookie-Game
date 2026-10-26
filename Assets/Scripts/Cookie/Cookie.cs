using UnityEngine;

public enum CookieType
{
    FirstParty,
    ThirdParty,
    Session,
    Persistent
}

public class Cookie : MonoBehaviour
{
    [HideInInspector] public float speed = 5f;

    private const float SessionSpeed = 1.5f;
    private const float PersistentSpeed = 0.6f;
    private const float PersistentScale = 2.8f;
    // Each cookie's size is randomly between 10% smaller and 70% bigger than the prefab.
    private const float MinSizeScale = 0.9f;
    private const float MaxSizeScale = 1.7f;
    private const float RollSpeed = 0.5f;   // spin rate compared to true rolling
    private const int PersistentReturns = 2;
    private const float TrackerMaxSpeed = 2f;
    private const float TrackerTurnRate = 120f;
    private const float TrackerMaxAngle = 60f;
    private const float TrackerLockTime = 0.7f;
    private const float TrackerLockDistance = 2f;

    private CookieType type;
    private Transform target;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private Color color = Color.white;
    private Vector2 heading = Vector2.left;
    private float baseSpeed;
    private float currentSpeed;
    private float trackingStartDistance;
    private bool tracking;
    private int returnsLeft;
    private float age;
    private bool collected;

    // Cookies currently in play, so the spawner can control how many are on screen at once.
    public static int ActiveCount { get; private set; }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        currentSpeed = speed;
    }

    void OnEnable()
    {
        ActiveCount++;
    }

    void OnDisable()
    {
        ActiveCount--;
    }

    public static string Name(CookieType type)
    {
        switch (type)
        {
            case CookieType.ThirdParty: return "Third-Party";
            case CookieType.Session: return "Session";
            case CookieType.Persistent: return "Persistent";
            default: return "First-Party";
        }
    }

    public static Color Tint(CookieType type)
    {
        switch (type)
        {
            case CookieType.ThirdParty: return new Color(1f, 0.35f, 0.4f);
            case CookieType.Session: return new Color(0.75f, 0.45f, 1f);
            case CookieType.Persistent: return new Color(0.55f, 0.9f, 0.4f);
            default: return Color.white;
        }
    }

    // direction is the straight line the cookie travels (tracker cookies steer from it toward the player).
    public void Setup(CookieType cookieType, Transform player, Vector2 direction)
    {
        type = cookieType;
        heading = direction.normalized;
        color = Tint(type);
        float size = Random.Range(MinSizeScale, MaxSizeScale);
        transform.localScale *= size;
        // Smaller cookies fly faster and bigger ones slower; an average-sized cookie keeps its type's speed.
        baseSpeed = speed * (MinSizeScale + MaxSizeScale) / 2f / size;
        currentSpeed = baseSpeed;
        switch (type)
        {
            case CookieType.ThirdParty:
                target = player;
                tracking = player != null;
                if (tracking) trackingStartDistance = Vector2.Distance(transform.position, player.position);
                CookieEye.Attach(spriteRenderer, player);
                break;
            case CookieType.Session:
                currentSpeed = baseSpeed * SessionSpeed;
                break;
            case CookieType.Persistent:
                currentSpeed = baseSpeed * PersistentSpeed;
                transform.localScale *= PersistentScale;
                returnsLeft = PersistentReturns;
                break;
        }
        spriteRenderer.color = color;
        // Big cookies would pop in at the edge, so make sure the whole cookie starts offscreen.
        transform.position = new Vector3(Mathf.Max(transform.position.x, OffscreenRightX()),
            transform.position.y, transform.position.z);
    }

    private float OffscreenRightX()
    {
        return mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect
            + spriteRenderer.bounds.extents.x + 0.1f;
    }

    void Update()
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

        float deltaTime = GameplayManager.overallSpeed * Time.deltaTime;
        if (tracking) Track(deltaTime);
        Vector3 step = heading * currentSpeed * deltaTime;
        transform.position += step;
        // Roll like a wheel, counter-clockwise while moving left, but a bit slower than true rolling so it reads clearly.
        float radius = spriteRenderer.sprite.bounds.extents.x * transform.lossyScale.x;
        transform.Rotate(0f, 0f, -step.x / radius * Mathf.Rad2Deg * RollSpeed);

        // Bounce off the top and bottom of the screen (only while heading into the edge, so it can't stick).
        float top = mainCamera.transform.position.y + mainCamera.orthographicSize;
        float bottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
        if ((transform.position.y + radius > top && heading.y > 0f) || (transform.position.y - radius < bottom && heading.y < 0f))
            heading.y = -heading.y;

        if (type == CookieType.Session)
        {
            age += Time.deltaTime;
            color.a = Mathf.Lerp(0.6f, 0.95f, 0.5f + 0.5f * Mathf.Sin(age * 8f));
            spriteRenderer.color = color;
        }

        // Gone as soon as it has fully left the screen, so it stops counting as on screen.
        float leftEdge = mainCamera.transform.position.x - mainCamera.orthographicSize * mainCamera.aspect;
        if (transform.position.x + radius < leftEdge)
        {
            if (returnsLeft > 0) ReturnFromRight();
            else Destroy(gameObject);
        }
        else if (Mathf.Abs(transform.position.y - mainCamera.transform.position.y) > mainCamera.orthographicSize + radius)
        {
            Destroy(gameObject);
        }
    }

    // Steers toward the player and speeds up while closing in, then stops steering shortly
    // before impact so the player can still dodge by rising or dropping.
    private void Track(float deltaTime)
    {
        if (target == null)
        {
            tracking = false;
            return;
        }

        Vector2 toTarget = target.position - transform.position;
        float closeness = 1f - Mathf.Clamp01(toTarget.magnitude / trackingStartDistance);
        currentSpeed = baseSpeed * Mathf.Lerp(1f, TrackerMaxSpeed, closeness);
        heading = Vector3.RotateTowards(heading, toTarget.normalized, TrackerTurnRate * Mathf.Deg2Rad * deltaTime, 0f);
        float angle = Mathf.Clamp(Vector2.SignedAngle(Vector2.left, heading), -TrackerMaxAngle, TrackerMaxAngle);
        heading = Quaternion.Euler(0f, 0f, angle) * Vector2.left;

        // A slowed-down player needs longer to get out of the way.
        float secondsToReach = toTarget.x / (heading.x * currentSpeed * GameplayManager.overallSpeed);
        float lockTime = TrackerLockTime / Mathf.Sqrt(GameplayManager.characterSpeed);
        if (-toTarget.x < TrackerLockDistance || secondsToReach < lockTime) tracking = false;
    }

    // Persistent cookies don't go away when they leave the screen; they come back from the
    // right, a little smaller and fainter each pass until they expire.
    private void ReturnFromRight()
    {
        returnsLeft--;
        transform.localScale *= 0.85f;
        transform.position = new Vector3(OffscreenRightX(), transform.position.y, transform.position.z);
        color.a *= 0.8f;
        spriteRenderer.color = color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || GameplayManager.currentState != GameState.Gameplay) return;

        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        collected = true;
        player.EatCookie(type);
        CookieBurst.Spawn(GetComponent<SpriteRenderer>());
        Destroy(gameObject);
    }
}
