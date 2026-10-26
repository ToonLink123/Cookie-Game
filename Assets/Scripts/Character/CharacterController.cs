using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float thrust = 35f;
    [SerializeField] float gravity = 25f;
    [SerializeField] float maxRiseSpeed = 7f;
    [SerializeField] float minY = -4f, maxY = 4f;
    [SerializeField] float takeoffSpeed = 2f;

    [Header("Cookie slowdown")]
    [SerializeField] float slowPerCookie = 0.07f;
    [SerializeField] float persistentSlow = 0.1f;
    [SerializeField] float persistentNextRunSlow = 0.05f;
    [SerializeField] float sessionSlow = 0.2f;
    [SerializeField] float sessionRecoverTime = 5f;
    [SerializeField] float trackedTime = 6f;
    [SerializeField] float minMultiplier = 0.2f;

    [Header("Staying still")]
    [SerializeField] float stillRange = 0.75f;
    [SerializeField] float stillTimeLimit = 5f;

    [Header("Sprites")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite happySprite;
    [SerializeField] Sprite sadSprite;

    Rigidbody2D rb;
    bool holding;
    float vy;
    bool takingOff = true;
    float speedBonus;
    float permanentSlow;
    float temporarySlow;
    float trackedTimer;
    int persistentEaten;
    float stillY;
    float stillTime;

    // While tracked (after eating a third-party cookie), new cookies spawn at the player's height.
    public bool IsTracked => trackedTimer > 0f;

    // True once the player has stayed within stillRange of one height for stillTimeLimit seconds,
    // e.g. by resting on the floor or ceiling. Hazards get sent at them until they move.
    public bool IsCamping => stillTime >= stillTimeLimit;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.position = new Vector2(rb.position.x, minY);
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        speedBonus = GameplayManager.startSpeedBonus;
        permanentSlow = GameplayManager.carriedSlow;
        UpdateSpeed();
    }

    void Update()
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

        trackedTimer -= Time.deltaTime;
        if (temporarySlow > 0f)
        {
            temporarySlow = Mathf.MoveTowards(temporarySlow, 0f, sessionSlow / sessionRecoverTime * Time.deltaTime);
            UpdateSpeed();
        }

        if (Mathf.Abs(rb.position.y - stillY) > stillRange)
        {
            stillY = rb.position.y;
            stillTime = 0f;
        }
        else
        {
            stillTime += Time.deltaTime;
        }

        var kb = Keyboard.current;
        var mouse = Mouse.current;
        holding = (kb != null && kb.spaceKey.isPressed) || (mouse != null && mouse.leftButton.isPressed);
        bool goingUp = takingOff || vy > 0f || (holding && vy == 0f);
        spriteRenderer.sprite = goingUp ? happySprite : sadSprite;
    }

    void FixedUpdate()
    {
        if (GameplayManager.currentState == GameState.GameOver)
        {
            vy -= gravity * Time.fixedDeltaTime;
            rb.linearVelocity = new Vector2(0f, vy);
            return;
        }

        if (GameplayManager.currentState != GameState.Gameplay)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float m = GameplayManager.characterSpeed;
        if (takingOff)
        {
            float takeoffY = Mathf.MoveTowards(rb.position.y, 0f, takeoffSpeed * m * Time.fixedDeltaTime);
            rb.MovePosition(new Vector2(rb.position.x, takeoffY));
            vy = takeoffSpeed * m;
            takingOff = takeoffY < 0f;
            return;
        }

        vy += (holding ? thrust : -gravity) * m * Time.fixedDeltaTime;
        vy = Mathf.Min(vy, maxRiseSpeed * m);

        float y = rb.position.y;
        if ((y <= minY && vy < 0f) || (y >= maxY && vy > 0f)) vy = 0f;
        if (y < minY || y > maxY) rb.position = new Vector2(rb.position.x, Mathf.Clamp(y, minY, maxY));
        rb.linearVelocity = new Vector2(0f, vy);
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        if (GameplayManager.currentState != GameState.Gameplay || other.GetComponent<Obstacle>() == null) return;

        GameplayManager.currentState = GameState.GameOver;
        spriteRenderer.sprite = sadSprite;
        vy = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    public void EatCookie(CookieType type)
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

        GameplayManager.cookieCount++;
        GameplayManager.cookiesEaten[(int)type]++;
        switch (type)
        {
            case CookieType.Session:
                temporarySlow += sessionSlow;
                break;
            case CookieType.Persistent:
                permanentSlow += persistentSlow;
                persistentEaten++;
                GameplayManager.SaveNextRunSlow(persistentEaten * persistentNextRunSlow);
                break;
            case CookieType.ThirdParty:
                permanentSlow += slowPerCookie;
                trackedTimer = trackedTime;
                GameplayManager.trackerCookiesHit++;
                break;
            default:
                permanentSlow += slowPerCookie;
                break;
        }
        UpdateSpeed();
    }

    // The "Clear Browsing Data" pickup: back to full speed, no more tracking or ads, and the
    // persistent-cookie slowdown saved for next game is gone.
    public void ClearBrowsingData()
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

        permanentSlow = 0f;
        temporarySlow = 0f;
        trackedTimer = 0f;
        GameplayManager.trackerCookiesHit = 0;
        persistentEaten = 0;
        GameplayManager.SaveNextRunSlow(0f);
        UpdateSpeed();
    }

    void UpdateSpeed()
    {
        GameplayManager.characterSpeed = Mathf.Max(minMultiplier, 1f + speedBonus - permanentSlow - temporarySlow);
    }
}