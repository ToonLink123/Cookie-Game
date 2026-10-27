using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class CookieSpawner : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("Cookie")] private GameObject cookiePrefab;

    [SerializeField] private float minY = -3f;
    [SerializeField] private float maxY = 3f;

    [SerializeField] private float minspawnDelay = 3f;
    [SerializeField] private float maxspwanDelay = 6f;

    [SerializeField] private float spawnTimer = 0f;
    [SerializeField] private float timeNextSpawn;

    [Header("Cookie types")]
    [SerializeField] private float firstPartyWeight = 50f;
    [SerializeField] private float sessionWeight = 20f;
    [SerializeField] private float thirdPartyWeight = 20f;
    [SerializeField] private float persistentWeight = 10f;
    [SerializeField] private float legitInterestWeight = 15f;
    [SerializeField] private float thirdPartyUnlockDistance = 100f;
    [SerializeField] private float persistentUnlockDistance = 220f;
    [SerializeField] private float legitInterestUnlockDistance = 50f;
    [SerializeField] private float turnBackOnDelay = 0.8f;

    [Header("Cookies on screen")]
    [SerializeField] private int startMinOnScreen = 1;
    [SerializeField] private int startMaxOnScreen = 3;
    [SerializeField] private int fullMinOnScreen = 5;
    [SerializeField] private int fullMaxOnScreen = 7;
    [SerializeField] private float fullOnScreenDistance = 300f;
    [SerializeField] private float groupSpread = 4f;

    [Header("Clear Browsing Data")]
    [SerializeField] private float firstClearDataDistance = 100f;
    [SerializeField] private float clearDataGapGrowth = 50f;
    [SerializeField] private float clearDataSizeVsSmallestCookie = 2.5f;

    [Header("Angled cookies")]
    [SerializeField, Range(0f, 1f)] private float angledChance = 0.4f;
    [SerializeField] private float minAngle = 10f;
    [SerializeField] private float maxAngle = 25f;

    private PlayerController player;
    private float clearDataGap;
    private float nextClearData;

    public GameObject CookiePrefab => cookiePrefab;
    public Sprite CookieSprite => cookiePrefab.GetComponent<SpriteRenderer>().sprite;

    void Start()
    {
        timeNextSpawn = Random.Range(minspawnDelay, maxspwanDelay);
        clearDataGap = nextClearData = firstClearDataDistance;
    }

    void Update()
    {
      if (GameplayManager.currentState != GameState.Gameplay) return;

      spawnTimer += Time.deltaTime * GameplayManager.overallSpeed;

      // Clear Browsing Data gets rarer as the run goes on: at 100 m, then 150 m later, then 200 m later...
      if (GameplayManager.metersTraveled >= nextClearData)
        {
            SpawnClearData();
            clearDataGap += clearDataGapGrowth;
            nextClearData += clearDataGap;
        }

      // How many cookies should be on screen ramps linearly from the start range to the full range.
      float ramp = Mathf.Clamp01(GameplayManager.metersTraveled / fullOnScreenDistance);
      int fewest = Mathf.RoundToInt(Mathf.Lerp(startMinOnScreen, fullMinOnScreen, ramp));
      int most = Mathf.RoundToInt(Mathf.Lerp(startMaxOnScreen, fullMaxOnScreen, ramp));

      // A group arrives on the usual timer, or right away when too few cookies are left,
      // topping the screen up to a random amount in range without going over the cap.
      if (spawnTimer > timeNextSpawn || Cookie.ActiveCount < fewest)
        {
            SpawnGroup(Random.Range(fewest, most + 1) - Cookie.ActiveCount);

            spawnTimer = 0f;

            timeNextSpawn = Random.Range(minspawnDelay, maxspwanDelay);

        }

    }

    // Sends the pickup at the player's current height, sized against the smallest a cookie can be.
    private void SpawnClearData()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();

        SpriteRenderer cookie = cookiePrefab.GetComponent<SpriteRenderer>();
        float smallestCookie = cookie.sprite.bounds.size.x * cookiePrefab.transform.localScale.x * Cookie.MinSizeScale;
        float y = player != null ? Mathf.Clamp(player.transform.position.y, minY, maxY) : 0f;
        ClearDataPickup.Spawn(y, smallestCookie * clearDataSizeVsSmallestCookie);
    }

    // Spreads the group over the spawn height in lanes, staggered sideways into a loose cluster.
    private void SpawnGroup(int count)
    {
        if (count <= 0) return;
        if (player == null) player = FindAnyObjectByType<PlayerController>();

        Camera mainCamera = Camera.main;
        float spawnX = mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect + 1f;
        float laneHeight = (maxY - minY) / count;
        for (int i = 0; i < count; i++)
        {
            float spawnY = minY + laneHeight * (i + Random.value);
            // Eating a third-party cookie makes one cookie in each group come right at the player's height.
            bool aimedAtPlayer = i == 0 && player != null && player.IsTracked;
            if (aimedAtPlayer) spawnY = Mathf.Clamp(player.transform.position.y, minY, maxY);

            CookieType type = RandomCookieType();
            Vector2 direction = Vector2.left;
            // Some cookies cut across at an angle, heading away from the edge they spawned nearer to.
            if (!aimedAtPlayer && type != CookieType.ThirdParty && Random.value < angledChance)
            {
                float angle = Random.Range(minAngle, maxAngle) * Mathf.Deg2Rad;
                float upOrDown = spawnY < (minY + maxY) / 2f ? 1f : -1f;
                direction = new Vector2(-Mathf.Cos(angle), upOrDown * Mathf.Sin(angle));
            }

            Spawn(type, new Vector3(spawnX + Random.Range(0f, groupSpread), spawnY), direction);
        }
    }

    private Cookie Spawn(CookieType type, Vector3 position, Vector2 direction)
    {
        Cookie cookie = Instantiate(cookiePrefab, position, Quaternion.identity).GetComponent<Cookie>();
        cookie.Setup(type, player != null ? player.transform : null, direction);
        return cookie;
    }

    // "Accept All" on a consent banner lets every kind of cookie in at once, all headed your way.
    public void SpawnConsentWave(int count)
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        Camera mainCamera = Camera.main;
        float spawnX = mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect + 1f;
        int typeCount = System.Enum.GetValues(typeof(CookieType)).Length;
        for (int i = 0; i < count; i++)
        {
            Vector3 position = new Vector3(spawnX + i * 0.9f,
                Mathf.Clamp(player.transform.position.y + Random.Range(-1.5f, 1.5f), minY, maxY));
            Spawn((CookieType)Random.Range(0, typeCount), position,
                (player.transform.position - position).normalized);
        }
    }

    // A rejected legitimate interest cookie comes back from the right a moment later, once.
    public void TurnBackOn(float y)
    {
        StartCoroutine(TurnBackOnLater(y));
    }

    private IEnumerator TurnBackOnLater(float y)
    {
        yield return new WaitForSeconds(turnBackOnDelay);
        while (GameplayManager.currentState == GameState.Paused) yield return null;
        if (GameplayManager.currentState != GameState.Gameplay) yield break;

        Camera mainCamera = Camera.main;
        float rightEdge = mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect;
        Spawn(CookieType.LegitimateInterest, new Vector3(rightEdge + 1f, y), Vector2.left).MarkTurnedBackOn();
        FloatingText.AtWorld(new Vector3(rightEdge - 2.5f, y), "Turned itself back on!", FloatingText.Warning);
    }

    private CookieType RandomCookieType()
    {
        float meters = GameplayManager.metersTraveled;
        float thirdParty = meters >= thirdPartyUnlockDistance ? thirdPartyWeight : 0f;
        float persistent = meters >= persistentUnlockDistance ? persistentWeight : 0f;
        float legit = meters >= legitInterestUnlockDistance ? legitInterestWeight : 0f;
        float roll = Random.Range(0f, firstPartyWeight + sessionWeight + thirdParty + persistent + legit);
        if ((roll -= firstPartyWeight) <= 0f) return CookieType.FirstParty;
        if ((roll -= sessionWeight) <= 0f) return CookieType.Session;
        if ((roll -= thirdParty) <= 0f) return CookieType.ThirdParty;
        if ((roll -= legit) <= 0f) return CookieType.LegitimateInterest;
        return CookieType.Persistent;
    }

}
