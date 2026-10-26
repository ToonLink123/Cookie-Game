using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Home screen decoration: every type of cookie tumbles down from the top, sized and paced like in
// game, a few at a time. Clicking one bursts it the same way eating one does in game.
public class MenuCookieRain : MonoBehaviour
{
    private const int MaxCookies = 6;
    private const float FallSpeed = 3f;           // for an average-sized first-party cookie
    private const float MinSpawnDelay = 0.3f;
    private const float MaxSpawnDelay = 1.2f;
    private const float MaxSpin = 90f;            // degrees per second

    private class FallingCookie
    {
        public SpriteRenderer spriteRenderer;
        public float speed;
        public float spin;
        public float radius;
    }

    private readonly List<FallingCookie> cookies = new List<FallingCookie>();

    // Easter egg: breaking these gives a speed bonus for the next game (see GameplayManager.StartGame).
    public int CookiesBroken { get; private set; }
    private SpriteRenderer prefabRenderer;
    private float prefabScale;
    private Camera mainCamera;
    private float spawnTimer;

    public void Initialize(GameObject cookiePrefab)
    {
        prefabRenderer = cookiePrefab.GetComponent<SpriteRenderer>();
        prefabScale = cookiePrefab.transform.localScale.x;
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Only on the home screen; once a run starts the rain is cleared away for good.
        if (GameplayManager.currentState != GameState.MainMenu)
        {
            foreach (FallingCookie cookie in cookies) Destroy(cookie.spriteRenderer.gameObject);
            cookies.Clear();
            enabled = false;
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f && cookies.Count < MaxCookies)
        {
            Spawn();
            spawnTimer = Random.Range(MinSpawnDelay, MaxSpawnDelay);
        }

        float bottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
        for (int i = cookies.Count - 1; i >= 0; i--)
        {
            Transform cookie = cookies[i].spriteRenderer.transform;
            cookie.position += Vector3.down * cookies[i].speed * Time.deltaTime;
            cookie.Rotate(0f, 0f, cookies[i].spin * Time.deltaTime);
            if (cookie.position.y + cookies[i].radius < bottom)
            {
                Destroy(cookie.gameObject);
                cookies.RemoveAt(i);
            }
        }

        Mouse mouse = Mouse.current;
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI)
            BurstCookieAt(mainCamera.ScreenToWorldPoint(mouse.position.ReadValue()));
    }

    private void Spawn()
    {
        CookieType type = (CookieType)Random.Range(0, System.Enum.GetValues(typeof(CookieType)).Length);
        float size = Random.Range(Cookie.MinSizeScale, Cookie.MaxSizeScale);
        float scale = prefabScale * size * (type == CookieType.Persistent ? Cookie.PersistentScale : 1f);

        GameObject cookie = new GameObject("Menu Cookie");
        cookie.transform.localScale = Vector3.one * scale;
        SpriteRenderer spriteRenderer = cookie.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = prefabRenderer.sprite;
        spriteRenderer.sharedMaterial = prefabRenderer.sharedMaterial;
        spriteRenderer.sortingLayerID = prefabRenderer.sortingLayerID;
        spriteRenderer.sortingOrder = prefabRenderer.sortingOrder;
        Color tint = Cookie.Tint(type);
        if (type == CookieType.Session) tint.a = 0.8f;
        spriteRenderer.color = tint;
        if (type == CookieType.ThirdParty) CookieEye.Attach(spriteRenderer, null);

        // Start just above a random spot along the top, fully on screen sideways.
        float radius = spriteRenderer.sprite.bounds.extents.x * scale;
        Vector3 cameraPosition = mainCamera.transform.position;
        float halfWidth = Mathf.Max(0f, mainCamera.orthographicSize * mainCamera.aspect - radius);
        cookie.transform.position = new Vector3(cameraPosition.x + Random.Range(-halfWidth, halfWidth),
            cameraPosition.y + mainCamera.orthographicSize + radius, 0f);

        cookies.Add(new FallingCookie
        {
            spriteRenderer = spriteRenderer,
            speed = FallSpeed * Cookie.SpeedScale(type, size),
            spin = Random.Range(-MaxSpin, MaxSpin),
            radius = radius,
        });
    }

    // Bursts a cookie under the point, if there is one (the newest, where cookies overlap).
    private void BurstCookieAt(Vector2 point)
    {
        for (int i = cookies.Count - 1; i >= 0; i--)
        {
            if (Vector2.Distance(point, cookies[i].spriteRenderer.transform.position) > cookies[i].radius) continue;

            CookieBurst.Spawn(cookies[i].spriteRenderer);
            Destroy(cookies[i].spriteRenderer.gameObject);
            cookies.RemoveAt(i);
            CookiesBroken++;
            return;
        }
    }
}
