using UnityEngine;

// Extra life for the balloon, made from its own sprite and pixel particles: a flickering burner flame
// while thrusting, a gentle swing under the envelope, a pilot who gets stuffed (the balloon widens and
// sags) and sweaty as cookies slow them down, relief when their data is cleared, and a crumpling
// deflate on a crash.
public class BalloonVisuals : MonoBehaviour
{
    // Spots on the balloon sprite, in its own pixels from the sprite's middle (x right, y up).
    private static readonly Vector2 Burner = new Vector2(-1.5f, -14f);
    private static readonly Vector2 Mouth = new Vector2(-1.5f, -28f);
    private static readonly Vector2 Head = new Vector2(-1.5f, -22f);
    private static readonly Vector2 EnvelopeTop = new Vector2(0f, 50f);
    private const float SwingPivot = 30f;   // the basket swings from this many pixels above the middle

    private static readonly Color[] FlameColors =
    {
        new Color(1f, 0.45f, 0.08f), new Color(1f, 0.75f, 0.15f), new Color(1f, 0.95f, 0.6f),
    };
    private static readonly Color SweatColor = new Color(0.55f, 0.85f, 1f);
    private static readonly Color[] SparkleColors = { new Color(0.6f, 1f, 0.6f), Color.white };
    private const float CrashTime = 0.8f;

    private PlayerController player;
    private Transform swing;
    private SpriteRenderer body;
    private float pixel;
    private float flameTimer;
    private float sweatTimer;
    private float lean;
    private float crashAge = -1f;

    public SpriteRenderer Body => body;

    // Moves the player's sprite onto a swinging child so it can sway and squash without touching the
    // physics body; returns the new renderer for the player to swap sprites on.
    public static BalloonVisuals Attach(PlayerController player, SpriteRenderer original)
    {
        BalloonVisuals visuals = player.gameObject.AddComponent<BalloonVisuals>();
        visuals.player = player;
        visuals.pixel = 1f / original.sprite.pixelsPerUnit;

        visuals.swing = new GameObject("Balloon Swing").transform;
        visuals.swing.SetParent(player.transform, false);
        visuals.swing.localPosition = Vector3.up * SwingPivot * visuals.pixel;
        GameObject sprite = new GameObject("Balloon Sprite");
        sprite.transform.SetParent(visuals.swing, false);
        sprite.transform.localPosition = Vector3.down * SwingPivot * visuals.pixel;

        visuals.body = sprite.AddComponent<SpriteRenderer>();
        visuals.body.sprite = original.sprite;
        visuals.body.sharedMaterial = original.sharedMaterial;
        visuals.body.color = original.color;
        visuals.body.sortingLayerID = original.sortingLayerID;
        visuals.body.sortingOrder = original.sortingOrder;
        original.enabled = false;
        return visuals;
    }

    void Update()
    {
        GameState state = GameplayManager.currentState;
        if (state == GameState.GameOver)
        {
            Deflate();
            return;
        }
        if (state != GameState.Gameplay) return;

        if (player.Thrusting)
        {
            // After a long frame, catch up with a few flames rather than every one that was missed.
            flameTimer = Mathf.Max(flameTimer - Time.deltaTime, -0.05f);
            for (; flameTimer <= 0f; flameTimer += 0.012f) EmitFlame();
        }

        // A gentle pendulum, leaning against the climb or the fall.
        lean = Mathf.Lerp(lean, -Mathf.Clamp(player.VerticalSpeed, -7f, 7f) * 1.2f, 4f * Time.deltaTime);
        swing.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.2f) * 3f + lean);

        // The slower the cookies have made you, the wider and saggier the balloon.
        float stuffed = Mathf.Clamp01((1f - GameplayManager.characterSpeed) / 0.8f);
        Vector3 wanted = new Vector3(1f + 0.22f * stuffed, 1f - 0.08f * stuffed, 1f);
        swing.localScale = Vector3.Lerp(swing.localScale, wanted, 5f * Time.deltaTime);

        // Badly slowed down, the pilot starts to sweat.
        if (stuffed > 0.45f)
        {
            sweatTimer -= Time.deltaTime;
            if (sweatTimer <= 0f)
            {
                EmitSweat();
                sweatTimer = Mathf.Lerp(0.6f, 0.15f, (stuffed - 0.45f) / 0.55f);
            }
        }
    }

    private Vector3 SpotOnSprite(Vector2 pixels)
    {
        return body.transform.TransformPoint(pixels * pixel);
    }

    private void EmitFlame()
    {
        Vector3 at = SpotOnSprite(Burner + new Vector2(Random.Range(-2f, 2f), 0f));
        PixelParticle.Spawn(at, new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(0.6f, 1.2f)),
            Random.Range(0.025f, 0.045f), FlameColors[Random.Range(0, FlameColors.Length)],
            Random.Range(0.08f, 0.16f), body.sortingOrder + 1, body.transform);
    }

    private void EmitSweat()
    {
        float side = Random.value < 0.5f ? -1f : 1f;
        Vector3 at = SpotOnSprite(Head + new Vector2(side * 7f, 2f));
        PixelParticle.Spawn(at, new Vector2(side * 0.3f, -0.8f), 0.035f, SweatColor, 0.5f, body.sortingOrder + 1,
            body.transform);
    }

    // Crumbs spray from the pilot's mouth as a cookie goes in.
    public void Crumbs(Color tint)
    {
        Color crumb = new Color(0.72f * tint.r, 0.45f * tint.g, 0.25f * tint.b);
        for (int i = 0; i < 7; i++)
        {
            PixelParticle.Spawn(SpotOnSprite(Mouth), new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(-1.5f, 0.5f)),
                Random.Range(0.03f, 0.05f), crumb, Random.Range(0.3f, 0.5f), body.sortingOrder + 1);
        }
    }

    // A burst of sparkles around the pilot when Clear Browsing Data takes the weight off.
    public void Relieved()
    {
        for (int i = 0; i < 16; i++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            PixelParticle.Spawn(SpotOnSprite(Head) + (Vector3)(direction * 0.25f), direction * Random.Range(1f, 2.2f),
                Random.Range(0.04f, 0.07f), SparkleColors[Random.Range(0, SparkleColors.Length)],
                Random.Range(0.4f, 0.7f), body.sortingOrder + 2);
        }
    }

    // On a crash the balloon crumples: it wobbles, squashes down and tips over while air puffs out the top.
    private void Deflate()
    {
        if (crashAge >= CrashTime + 1f) return;
        crashAge = Mathf.Max(crashAge, 0f) + Time.deltaTime;
        float t = Mathf.Clamp01(crashAge / CrashTime);
        float wobble = Mathf.Sin(crashAge * 28f) * (1f - t) * 0.1f;
        swing.localScale = new Vector3(Mathf.Lerp(1f, 0.78f, t) + wobble, Mathf.Lerp(1f, 0.5f, t) - wobble, 1f);
        swing.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(crashAge * 14f) * 20f * (1f - t) + 30f * t);
        if (crashAge < CrashTime && Random.value < 0.6f)
        {
            PixelParticle.Spawn(SpotOnSprite(EnvelopeTop + new Vector2(Random.Range(-20f, 20f), 0f)),
                new Vector2(Random.Range(-1f, 1f), Random.Range(0.8f, 2f)), Random.Range(0.05f, 0.09f),
                new Color(1f, 1f, 1f, 0.8f), Random.Range(0.35f, 0.6f), body.sortingOrder + 1);
        }
    }
}
