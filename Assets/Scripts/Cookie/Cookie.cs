using UnityEngine;

public enum CookieType
{
    FirstParty,
    ThirdParty,
    Session,
    Persistent,
    LegitimateInterest
}

public class Cookie : MonoBehaviour
{
    [HideInInspector] public float speed = 5f;

    private const float SessionSpeed = 1.5f;
    private const float PersistentSpeed = 0.6f;
    public const float PersistentScale = 2.8f;
    // Each cookie's size is randomly between 10% smaller and 70% bigger than the prefab.
    public const float MinSizeScale = 0.9f;
    public const float MaxSizeScale = 1.7f;
    private const float RollSpeed = 0.5f;   // spin rate compared to true rolling
    private const int PersistentReturns = 2;
    private const float TrackerMaxSpeed = 2f;
    private const float TrackerTurnRate = 240f;   // fastest it can turn while homing in, degrees per second
    private const float TrackerTurnSmoothing = 0.12f;   // roughly how long it takes to swing onto a new heading
    // How far it aims ahead of the player's climb or fall: 0 aims at the player, 1 where they'll be if they keep going.
    private const float TrackerLead = 0.75f;
    private const float TrackerMaxAngle = 75f;
    // Trackers stop steering this many seconds (or units) before reaching the player, leaving time to dodge.
    private const float TrackerLockTime = 0.56f;
    private const float TrackerLockDistance = 1.6f;
    // Rejecting by clicking: trackers sidestep a nearby cursor, persistent cookies need three
    // clicks and a confirmation, and session cookies end by themselves soon anyway.
    private const int PersistentClicks = 3;
    private const float MinSessionLength = 2.2f;
    private const float MaxSessionLength = 3.2f;
    private const float SessionFade = 0.4f;
    // A tracker slides up or down out of the cursor's way, but only so far and only while the cursor is
    // close, then eases back onto its course, so the cursor can't steer it away. Near the player it
    // stops dodging altogether, so a last-moment nudge can't make it miss.
    private const float DodgeRadius = 1.4f;
    private const float DodgeDistance = 1.1f;
    private const float DodgeSmoothing = 0.12f;   // roughly how long a slide takes
    private const float DodgeSwitch = 0.3f;   // how far past its course the cursor must go to change sides
    private const float DodgeStopDistance = 1.5f;   // ahead of the player, where dodging ends...
    private const float DodgeFullDistance = 4f;   // ...and where it's at full strength

    private CookieType type;
    private Transform target;
    private PlayerController targetPlayer;
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
    private bool rejected;
    private int clicksLeft = PersistentClicks;
    private bool confirming;
    private float sessionLeft;
    private bool turnedBackOn;
    private WarningMarker aimMarker;
    private float turnVelocity;
    private float dodge;   // how far it has slid up (or down, below zero) off its course
    private float dodgeVelocity;
    private float dodgeSide = 1f;

    // Cookies currently in play, so the spawner can control how many are on screen at once.
    public static int ActiveCount { get; private set; }

    public CookieType Type => type;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        currentSpeed = speed;
        MovingCollider.AddBody(gameObject);
    }

    void OnEnable()
    {
        ActiveCount++;
    }

    void OnDisable()
    {
        ActiveCount--;
    }

    void OnDestroy()
    {
        ClearAimMarker();
    }

    public static string Name(CookieType type)
    {
        switch (type)
        {
            case CookieType.ThirdParty: return "Third-Party";
            case CookieType.Session: return "Session";
            case CookieType.Persistent: return "Persistent";
            case CookieType.LegitimateInterest: return "Legit Interest";
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
            case CookieType.LegitimateInterest: return new Color(1f, 0.82f, 0.3f);
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
        baseSpeed = speed * SpeedScale(type, size);
        currentSpeed = baseSpeed;
        switch (type)
        {
            case CookieType.ThirdParty:
                target = player;
                targetPlayer = player != null ? player.GetComponent<PlayerController>() : null;
                tracking = player != null;
                if (tracking) trackingStartDistance = Vector2.Distance(transform.position, player.position);
                CookieEye.Attach(spriteRenderer, player);
                break;
            case CookieType.Session:
                sessionLeft = Random.Range(MinSessionLength, MaxSessionLength);
                break;
            case CookieType.Persistent:
                transform.localScale *= PersistentScale;
                returnsLeft = PersistentReturns;
                break;
        }
        spriteRenderer.color = color;
        // Big cookies would pop in at the edge, so make sure the whole cookie starts offscreen.
        transform.position = new Vector3(Mathf.Max(transform.position.x, OffscreenRightX()),
            transform.position.y, transform.position.z);
    }

    // A legitimate interest cookie that comes back after being rejected; this time rejecting it sticks.
    public void MarkTurnedBackOn()
    {
        turnedBackOn = true;
    }

    // Speed compared to an average-sized first-party cookie: smaller cookies are faster and bigger ones
    // slower, then session cookies are quicker and persistent ones slower.
    public static float SpeedScale(CookieType type, float size)
    {
        float scale = (MinSizeScale + MaxSizeScale) / 2f / size;
        if (type == CookieType.Session) scale *= SessionSpeed;
        else if (type == CookieType.Persistent) scale *= PersistentSpeed;
        return scale;
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
        age += Time.deltaTime;
        transform.position -= Vector3.up * dodge;   // back onto its course for the move; the dodge goes back on below
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
        {
            heading.y = -heading.y;
            turnVelocity = -turnVelocity;
        }
        if (type == CookieType.ThirdParty)
        {
            Dodge(top - radius, bottom + radius);
            transform.position += Vector3.up * dodge;
        }

        if (type == CookieType.Session)
        {
            // Flickers, and when its session runs out it fades away by itself.
            sessionLeft -= Time.deltaTime;
            color.a = Mathf.Lerp(0.6f, 0.95f, 0.5f + 0.5f * Mathf.Sin(age * 8f)) * Mathf.Clamp01(sessionLeft / SessionFade);
            spriteRenderer.color = color;
            if (sessionLeft <= 0f)
            {
                Destroy(gameObject);
                return;
            }
        }
        else if (type == CookieType.LegitimateInterest)
        {
            // Keeps switching itself off and back on.
            color.a = Mathf.Repeat(age, 1.2f) < 0.9f ? 1f : 0.45f;
            spriteRenderer.color = color;
        }

        if (aimMarker != null && target != null && transform.position.x < target.position.x - 1f) ClearAimMarker();

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

    // Curves toward the player and speeds up while closing in, then stops steering shortly
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
        // Aims ahead of where the player is climbing or falling, so simply drifting out of the way doesn't shake it.
        Vector2 aimAt = toTarget;
        if (targetPlayer != null)
        {
            float edge = mainCamera.orthographicSize - 0.5f;
            float ahead = targetPlayer.VerticalSpeed * Mathf.Max(0f, SecondsToReach(toTarget)) * TrackerLead;
            aimAt.y = Mathf.Clamp(target.position.y + ahead, mainCamera.transform.position.y - edge,
                mainCamera.transform.position.y + edge) - transform.position.y;
        }
        // Turning eases in and out, so it flies in smooth curves instead of snapping to each new heading.
        float aim = Mathf.Clamp(Vector2.SignedAngle(Vector2.left, aimAt), -TrackerMaxAngle, TrackerMaxAngle);
        float angle = Mathf.SmoothDampAngle(Vector2.SignedAngle(Vector2.left, heading), aim, ref turnVelocity,
            TrackerTurnSmoothing, TrackerTurnRate, deltaTime);
        heading = Quaternion.Euler(0f, 0f, angle) * Vector2.left;

        // A slowed-down player needs longer to get out of the way.
        float lockTime = TrackerLockTime / Mathf.Sqrt(GameplayManager.characterSpeed);
        if (-toTarget.x < TrackerLockDistance || SecondsToReach(toTarget) < lockTime)
        {
            tracking = false;
            // Mark where it will now cross the player's column, so there's a clear spot to get away from.
            float crossY = transform.position.y + heading.y / heading.x * (target.position.x - transform.position.x);
            aimMarker = WarningMarker.Eye(new Vector3(target.position.x, crossY, 0f), 2f);
        }
    }

    // How long until it reaches the player's column at its current heading and speed.
    private float SecondsToReach(Vector2 toTarget)
    {
        return toTarget.x / (heading.x * currentSpeed * GameplayManager.overallSpeed);
    }

    // Works out this frame's slide away from the cursor; the cookie is on its course while this runs.
    // The slide stays between top and bottom, the highest and lowest the cookie's centre may go.
    private void Dodge(float top, float bottom)
    {
        Vector2 course = transform.position;
        float goal = 0f;
        // Measured from its course rather than where it has slid to, so a still cursor at the edge of the
        // radius can't flick it in and out of dodging.
        if (CookieRejecter.Pointer.HasValue && Vector2.Distance(course, CookieRejecter.Pointer.Value) < DodgeRadius)
        {
            // Slide to the side away from the cursor, and stay there until the cursor clearly crosses over.
            float cursorAbove = CookieRejecter.Pointer.Value.y - course.y;
            if (Mathf.Abs(cursorAbove) > DodgeSwitch) dodgeSide = cursorAbove > 0f ? -1f : 1f;
            goal = dodgeSide * DodgeDistance;
            if (target != null)
                goal *= Mathf.InverseLerp(DodgeStopDistance, DodgeFullDistance, course.x - target.position.x);
        }
        goal = Mathf.Clamp(course.y + goal, bottom, top) - course.y;
        dodge = Mathf.SmoothDamp(dodge, goal, ref dodgeVelocity, DodgeSmoothing, Mathf.Infinity, Time.deltaTime);
    }

    private void ClearAimMarker()
    {
        if (aimMarker != null) Destroy(aimMarker.gameObject);
        aimMarker = null;
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

    // The player clicked this cookie. Most are rejected right away; persistent cookies hold on.
    public void Click()
    {
        if (collected || rejected) return;

        if (type == CookieType.Persistent)
        {
            if (confirming) return;   // its "Are you sure?" is already up
            clicksLeft--;
            if (clicksLeft > 0)
            {
                FloatingText.AtWorld(transform.position, clicksLeft + (clicksLeft == 1 ? " more click" : " more clicks"),
                    FloatingText.Warning);
                return;
            }
            confirming = true;
            ConfirmPrompt.Show(transform, Reject);
            return;
        }
        Reject();
    }

    public void Reject()
    {
        if (collected || rejected || this == null) return;

        rejected = true;
        GameplayManager.cookiesRejected[(int)type]++;
        FloatingText.AtWorld(transform.position, "Rejected!", FloatingText.Good);
        CookieBurst.Spawn(spriteRenderer);
        // Legitimate interest switches itself back on once; only the second rejection sticks.
        if (type == CookieType.LegitimateInterest && !turnedBackOn)
        {
            CookieSpawner spawner = FindAnyObjectByType<CookieSpawner>();
            if (spawner != null) spawner.TurnBackOn(transform.position.y);
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || rejected || GameplayManager.currentState != GameState.Gameplay) return;

        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        collected = true;
        ClearAimMarker();
        player.EatCookie(type);
        // Stop moving and counting as on screen; CookieSuck slurps it into the player, then removes it.
        enabled = false;
        GetComponent<Collider2D>().enabled = false;
        CookieSuck.Begin(gameObject, player.transform);
    }
}
