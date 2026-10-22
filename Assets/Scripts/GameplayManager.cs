
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

    private const string HighScoreKey = "CookieGame.HighScoreMeters";
    public static float HighScore { get; private set; }

    private static bool restarting;
    private bool gameOverHandled;

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject player;
    [SerializeField] private CookieSpawner cookieSpawner;
    [SerializeField] private Text statsText;
    [SerializeField] private GameObject restartButton;
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private GameUI gameUI;

    void Awake()
    {
        currentState = GameState.MainMenu;
        overallSpeed = 1f;
        characterSpeed = 1f;
        metersTraveled = 0f;
        cookieCount = 0;
        difficultyUpdater = 0f;
        HighScore = PlayerPrefs.GetFloat(HighScoreKey, 0f);
        statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statsText.gameObject.SetActive(false);
        restartButton.SetActive(false);
        player.SetActive(false);
        cookieSpawner.enabled = false;
        gameUI.Initialize(HighScore);
    }

    void Start()
    {
        if (!restarting) return;

        restarting = false;
        StartGame();
    }

    void Update()
    {

        if (currentState == GameState.Gameplay)
        {
            float distance = overallSpeed * 1.2f * Time.deltaTime;
            metersTraveled += distance;
            difficultyUpdater += distance;
            if (difficultyUpdater >= 500f)
            {
                obstacleSpawner.maxSpawnDelay = Mathf.Max(1f, obstacleSpawner.maxSpawnDelay - 0.2f);
                obstacleSpawner.minSpawnDelay = Mathf.Max(1f, obstacleSpawner.minSpawnDelay - 0.2f);
                difficultyUpdater = 0f;
            }
            overallSpeed += 0.006f * Time.deltaTime;
        }

        if (currentState == GameState.GameOver && !gameOverHandled)
        {
            gameOverHandled = true;
            bool newHighScore = metersTraveled > HighScore;
            if (newHighScore)
            {
                HighScore = metersTraveled;
                PlayerPrefs.SetFloat(HighScoreKey, HighScore);
                PlayerPrefs.Save();
            }

            cookieSpawner.enabled = false;
            gameUI.ShowGameOver(HighScore, newHighScore);
            restartButton.SetActive(true);
        }

        statsText.text = "Speed: " + (characterSpeed * 100f).ToString("0") + "%";
        gameUI.UpdateTrackers(cookieCount, metersTraveled);
    }

    public void StartGame()
    {
        if (currentState != GameState.MainMenu) return;

        currentState = GameState.Gameplay;
        mainMenu.SetActive(false);
        gameUI.ShowGameplay();
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