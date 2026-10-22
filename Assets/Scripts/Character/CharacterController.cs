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
    [SerializeField] float minMultiplier = 0.2f;

    [Header("Sprites")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite happySprite;
    [SerializeField] Sprite sadSprite;

    Rigidbody2D rb;
    bool holding;
    float vy;
    bool takingOff = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.position = new Vector2(rb.position.x, minY);
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

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

        float m = GameplayManager.overallSpeed;
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

    public void EatCookie()
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

        GameplayManager.cookieCount++;
        GameplayManager.overallSpeed = Mathf.Max(minMultiplier, GameplayManager.overallSpeed - slowPerCookie);
    }
}