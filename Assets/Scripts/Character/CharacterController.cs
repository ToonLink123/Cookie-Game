using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float thrust = 35f;
    [SerializeField] float gravity = 25f;
    [SerializeField] float maxRiseSpeed = 7f;
    [SerializeField] float maxFallSpeed = 9f;
    [SerializeField] float minY = -4f, maxY = 4f;

    [Header("Cookie slowdown")]
    [SerializeField] float slowPerCookie = 0.07f;
    [SerializeField] float minMultiplier = 0.2f;
    [Range(0f, 1f)] public float slowMultiplier = 1f;

    [SerializeField] GameObject burner;

    Rigidbody2D rb;
    bool holding;
    float vy;

    void Awake()
    {
        // no rotating, no moving x, no fr gravity
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        // made it so spacebar and left click both count for ascending
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        holding = (kb != null && kb.spaceKey.isPressed) || (mouse != null && mouse.leftButton.isPressed);
        // idk how we are doing this, but if we have a fire sprite for when we are floating up, set it to this
        if (burner) burner.SetActive(holding);
    }

    void FixedUpdate()
    {
        // m is the cookie mod
        float m = slowMultiplier;

        // velocity is holding space multiplied times slow mod
        vy += (holding ? thrust : -gravity) * m * Time.fixedDeltaTime;
        //clamp velocities between max and min described above
        vy = Mathf.Clamp(vy, -maxFallSpeed * m, maxRiseSpeed * m);

<<<<<<< Updated upstream
=======
        // current height, check for out of bounds
>>>>>>> Stashed changes
        float y = rb.position.y;
        if ((y <= minY && vy < 0f) || (y >= maxY && vy > 0f)) vy = 0f;
        if (y < minY || y > maxY) rb.position = new Vector2(rb.position.x, Mathf.Clamp(y, minY, maxY));
        rb.linearVelocity = new Vector2(0f, vy);
    }


    // TODO: HAVE COOKIES CALL THIS WHEN EATEN!!!
    public void EatCookie()
    {
        slowMultiplier = Mathf.Max(minMultiplier, slowMultiplier - slowPerCookie);
    }
}