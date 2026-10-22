using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState
{
    MainMenu,
    Gameplay,
    Paused,
    Victory,
    GameOver
}

public class GameplayManager : MonoBehaviour
{
    public static GameState currentState = GameState.MainMenu;
    public static float overallSpeed = 1f;
    public static float characterSpeed = 1f;
    public static int cookieCount;
    public static float metersTraveled = 0f;
    public static float difficultyUpdater = 0f;


    private static bool restarting;

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject player;
    [SerializeField] private CookieSpawner cookieSpawner;
    [SerializeField] private Text statsText;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private ObstacleSpawner obstacleSpawner;

    void Awake()
    {
        currentState = GameState.MainMenu;
        overallSpeed = 1f;
        characterSpeed = 1f;
        metersTraveled = 0f;
        cookieCount = 0;
        difficultyUpdater = 0f;
        statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statsText.gameObject.SetActive(false);
        restartButton.SetActive(false);
        player.SetActive(false);
        cookieSpawner.enabled = false;
    }

    void Start()
    {
        if (!restarting) return;

        restarting = false;
        StartGame();
    }

    void Update()
    {

        if (currentState != GameState.GameOver) {
            metersTraveled = metersTraveled + (overallSpeed * 0.02f);
            difficultyUpdater = metersTraveled + (overallSpeed * 0.02f);
            if (difficultyUpdater >= 1000) {
                if (obstacleSpawner.maxSpawnDelay > 1) {
                    obstacleSpawner.maxSpawnDelay = obstacleSpawner.maxSpawnDelay - 0.5f;
                }
                if (obstacleSpawner.minSpawnDelay > 1) {
                    obstacleSpawner.minSpawnDelay = obstacleSpawner.minSpawnDelay - 0.5f;
                }
                difficultyUpdater = 0f;
            }
        }
        statsText.text = "Cookies: " + cookieCount + "\nSpeed: " + (characterSpeed * 100f).ToString("0") + "%" + "\nMeters: " + metersTraveled.ToString("0") + "m" + "\ntime to next update: " + (1000f - difficultyUpdater).ToString("0") + "m";
        restartButton.SetActive(currentState == GameState.GameOver);
        overallSpeed = overallSpeed + 0.0001f;
    }

    public void StartGame()
    {
        currentState = GameState.Gameplay;
        mainMenu.SetActive(false);
        statsText.gameObject.SetActive(true);
        player.SetActive(true);
        cookieSpawner.enabled = true;
    }

    public void RestartGame()
    {
        if (currentState != GameState.GameOver) return;

        restarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
