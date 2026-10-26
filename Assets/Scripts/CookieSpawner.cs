using Unity.VisualScripting;
using UnityEngine;

public class CookieSpawner : MonoBehaviour
{
    [SerializeField]private GameObject Cookie;

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
    [SerializeField] private float thirdPartyUnlockDistance = 100f;
    [SerializeField] private float persistentUnlockDistance = 220f;

    private PlayerController player;

    void Start()
    {
        timeNextSpawn = Random.Range(minspawnDelay, maxspwanDelay);
    }

    void Update()
    {
      if (GameplayManager.currentState != GameState.Gameplay) return;

      spawnTimer += Time.deltaTime * GameplayManager.overallSpeed;

      if( spawnTimer > timeNextSpawn)
        {
            if (player == null) player = FindAnyObjectByType<PlayerController>();

            Camera mainCamera = Camera.main;
            float spawnX = mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect + 1f;
            // Eating a third-party cookie makes the next cookies spawn right at the player's height.
            float spawnY = player != null && player.IsTracked
                ? Mathf.Clamp(player.transform.position.y, minY, maxY)
                : Random.Range(minY, maxY);
            Vector3 spawnPos = new Vector3(spawnX, spawnY);
            Instantiate(Cookie, spawnPos, Quaternion.identity).GetComponent<Cookie>()
                .Setup(RandomCookieType(), player != null ? player.transform : null);

            spawnTimer = 0f;

            timeNextSpawn = Random.Range(minspawnDelay, maxspwanDelay);

        }

    }

    private CookieType RandomCookieType()
    {
        float meters = GameplayManager.metersTraveled;
        float thirdParty = meters >= thirdPartyUnlockDistance ? thirdPartyWeight : 0f;
        float persistent = meters >= persistentUnlockDistance ? persistentWeight : 0f;
        float roll = Random.Range(0f, firstPartyWeight + sessionWeight + thirdParty + persistent);
        if ((roll -= firstPartyWeight) <= 0f) return CookieType.FirstParty;
        if ((roll -= sessionWeight) <= 0f) return CookieType.Session;
        if ((roll -= thirdParty) <= 0f) return CookieType.ThirdParty;
        return CookieType.Persistent;
    }

}
