using UnityEngine;

// In game, an eaten cookie gets slurped into the balloon pilot's mouth: it stretches toward them,
// swirls and shrinks, speeding up as it goes in.
public class CookieSuck : MonoBehaviour
{
    private const float Duration = 0.25f;
    private const float Stretch = 0.8f;    // extra length toward the mouth by the end
    private const float Swirl = 720f;      // degrees per second at full speed
    // Where the pilot's mouth is on the player sprite, from its middle.
    private static readonly Vector3 MouthOffset = new Vector3(0f, -0.28f, 0f);

    private Transform player;
    private Transform cookie;
    private Vector3 start;
    private float elapsed;

    public static void Begin(GameObject cookie, Transform player)
    {
        // The cookie rides on a holder that always points at the mouth, so stretching the holder
        // pulls the cookie long toward the mouth whichever way it was rolling.
        GameObject holder = new GameObject("Cookie Being Eaten");
        holder.transform.SetPositionAndRotation(cookie.transform.position, PointingAt(cookie.transform.position, player));
        cookie.transform.SetParent(holder.transform, true);

        CookieSuck suck = holder.AddComponent<CookieSuck>();
        suck.player = player;
        suck.cookie = cookie.transform;
        suck.start = cookie.transform.position;
    }

    private static Quaternion PointingAt(Vector3 from, Transform player)
    {
        Vector2 toMouth = player.TransformPoint(MouthOffset) - from;
        return Quaternion.Euler(0f, 0f, Mathf.Atan2(toMouth.y, toMouth.x) * Mathf.Rad2Deg);
    }

    void Update()
    {
        if (GameplayManager.currentState == GameState.Paused) return;

        elapsed += Time.deltaTime;
        float progress = elapsed / Duration;
        if (progress >= 1f || player == null)
        {
            Destroy(gameObject);
            return;
        }

        // Starts gently, then whooshes in, following the player as they move.
        float pull = progress * progress;
        transform.position = Vector3.Lerp(start, player.TransformPoint(MouthOffset), pull);
        transform.rotation = PointingAt(transform.position, player);
        float size = 1f - pull;
        transform.localScale = new Vector3(size * (1f + Stretch * progress), size * (1f - 0.4f * progress), 1f);
        cookie.Rotate(0f, 0f, Swirl * progress * Time.deltaTime);
    }
}
