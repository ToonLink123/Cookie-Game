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
    [HideInInspector] public float destroyX = -12f;

    private const float SessionSpeed = 1.5f;
    private const float PersistentSpeed = 0.6f;
    private const float PersistentScale = 1.4f;
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
    private float currentSpeed;
    private float trackingStartDistance;
    private bool tracking;
    private int returnsLeft;
    private float age;
    private bool collected;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        currentSpeed = speed;
    }

    public void Setup(CookieType cookieType, Transform player)
    {
        type = cookieType;
        switch (type)
        {
            case CookieType.ThirdParty:
                color = new Color(1f, 0.35f, 0.4f);
                target = player;
                tracking = player != null;
                if (tracking) trackingStartDistance = Vector2.Distance(transform.position, player.position);
                CookieEye.Attach(spriteRenderer, player);
                break;
            case CookieType.Session:
                color = new Color(0.75f, 0.45f, 1f);
                currentSpeed = speed * SessionSpeed;
                break;
            case CookieType.Persistent:
                color = new Color(0.55f, 0.9f, 0.4f);
                currentSpeed = speed * PersistentSpeed;
                transform.localScale *= PersistentScale;
                returnsLeft = PersistentReturns;
                break;
        }
        spriteRenderer.color = color;
    }

    void Update()
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

        float deltaTime = GameplayManager.overallSpeed * Time.deltaTime;
        if (tracking) Track(deltaTime);
        transform.position += (Vector3)(heading * currentSpeed * deltaTime);

        if (type == CookieType.Session)
        {
            age += Time.deltaTime;
            color.a = Mathf.Lerp(0.6f, 0.95f, 0.5f + 0.5f * Mathf.Sin(age * 8f));
            spriteRenderer.color = color;
        }

        if (transform.position.x < destroyX)
        {
            if (returnsLeft > 0) ReturnFromRight();
            else Destroy(gameObject);
        }
        else if (Mathf.Abs(transform.position.y - mainCamera.transform.position.y) > mainCamera.orthographicSize + 1f)
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
        currentSpeed = speed * Mathf.Lerp(1f, TrackerMaxSpeed, closeness);
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
        float rightEdge = mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect + 1f;
        transform.position = new Vector3(rightEdge, transform.position.y, transform.position.z);
        transform.localScale *= 0.85f;
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
