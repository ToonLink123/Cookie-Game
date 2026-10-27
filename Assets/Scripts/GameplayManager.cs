
using UnityEngine;
using UnityEngine.InputSystem;
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
    public static int[] cookiesEaten;   // this run, indexed by CookieType
    public static int trackerCookiesHit;   // since the last Clear Browsing Data; drives the ads

    private const string HighScoreKey = "CookieGame.HighScoreMeters";
    public static float HighScore { get; private set; }

    // Slowdown this run starts with, saved on the device by persistent cookies eaten last game.
    private const string NextRunSlowKey = "CookieGame.PersistentCookieSlow";
    public static float carriedSlow;

    // Easter egg: each cookie broken on the home screen adds 1% speed to the next game, up to 50%.
    private const float BonusPerMenuCookie = 0.01f;
    private const int MaxBonusMenuCookies = 50;
    public static float startSpeedBonus;

    // Speed readout colours at 20%, 100% and 150% speed.
    private static readonly Color SlowestColor = new Color32(0xb4, 0x02, 0x19, 0xff);
    private static readonly Color NormalSpeedColor = new Color32(0x08, 0xe8, 0x17, 0xff);
    private static readonly Color FastestColor = new Color32(0x93, 0xee, 0x99, 0xff);
    private MenuCookieRain menuCookieRain;

    private static bool restarting;
    private bool gameOverHandled;
    private GameObject homeButton;

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
        cookiesEaten = new int[System.Enum.GetValues(typeof(CookieType)).Length];
        trackerCookiesHit = 0;
        HighScore = PlayerPrefs.GetFloat(HighScoreKey, 0f);
        carriedSlow = PlayerPrefs.GetFloat(NextRunSlowKey, 0f);
        startSpeedBonus = 0f;
        statsText.font = GameUI.LoadFont();
        Outline outline = statsText.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1.5f, 1.5f);
        statsText.gameObject.SetActive(false);
        restartButton.SetActive(false);
        homeButton = CreateHomeButton();
        player.SetActive(false);
        cookieSpawner.enabled = false;
        gameUI.Initialize(HighScore, cookieSpawner.CookieSprite, mainMenu.GetComponent<RectTransform>());
        gameUI.CreatePauseScreen(ResumeGame, GoToHomeScreen);
        menuCookieRain = gameObject.AddComponent<MenuCookieRain>();
        menuCookieRain.Initialize(cookieSpawner.CookiePrefab);
    }

    void Start()
    {
        if (!restarting) return;

        restarting = false;
        StartGame();
    }

    void Update()
    {
        // Esc or P pauses a run, and resumes it again.
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
        {
            if (currentState == GameState.Gameplay) PauseGame();
            else if (currentState == GameState.Paused) ResumeGame();
        }

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
            homeButton.SetActive(true);
        }

        statsText.text = "Speed: " + (characterSpeed * 100f).ToString("0") + "%";
        statsText.color = SpeedColor(characterSpeed);
        gameUI.UpdateTrackers(cookieCount, metersTraveled);
    }

    // Dark red when crawling, through orange and yellow to green at normal speed, then paler green when boosted.
    private static Color SpeedColor(float speed)
    {
        return speed < 1f
            ? BlendHue(SlowestColor, NormalSpeedColor, Mathf.InverseLerp(0.2f, 1f, speed))
            : BlendHue(NormalSpeedColor, FastestColor, Mathf.InverseLerp(1f, 1.5f, speed));
    }

    // Blends around the colour wheel the short way, so red to green passes orange and yellow instead of brown.
    private static Color BlendHue(Color from, Color to, float t)
    {
        Color.RGBToHSV(from, out float fromHue, out float fromSaturation, out float fromValue);
        Color.RGBToHSV(to, out float toHue, out float toSaturation, out float toValue);
        if (toHue - fromHue > 0.5f) fromHue += 1f;
        else if (fromHue - toHue > 0.5f) toHue += 1f;
        return Color.HSVToRGB(Mathf.Repeat(Mathf.Lerp(fromHue, toHue, t), 1f),
            Mathf.Lerp(fromSaturation, toSaturation, t), Mathf.Lerp(fromValue, toValue, t));
    }

    public void StartGame()
    {
        if (currentState != GameState.MainMenu) return;

        currentState = GameState.Gameplay;
        // Last game's persistent cookies expire once this run has picked them up.
        SaveNextRunSlow(0f);
        startSpeedBonus = Mathf.Min(menuCookieRain.CookiesBroken, MaxBonusMenuCookies) * BonusPerMenuCookie;
        mainMenu.SetActive(false);
        gameUI.ShowGameplay(carriedSlow);
        statsText.gameObject.SetActive(true);
        player.SetActive(true);
        cookieSpawner.enabled = true;
    }

    public static void SaveNextRunSlow(float slow)
    {
        PlayerPrefs.SetFloat(NextRunSlowKey, slow);
        PlayerPrefs.Save();
    }

    public void RestartGame()
    {
        if (currentState != GameState.GameOver) return;

        restarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Reloading without the restarting flag lands on the home screen instead of a new run.
    public void GoToHomeScreen()
    {
        if (currentState != GameState.GameOver && currentState != GameState.Paused) return;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // Application.Quit does nothing in the editor
#else
        Application.Quit();
#endif
    }

    // Everything that moves already stops whenever the state isn't Gameplay.
    public void PauseGame()
    {
        if (currentState != GameState.Gameplay) return;

        currentState = GameState.Paused;
        gameUI.ShowPause(true);
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        currentState = GameState.Gameplay;
        gameUI.ShowPause(false);
    }

    // A copy of the restart button, just below it, that goes back to the home screen.
    private GameObject CreateHomeButton()
    {
        GameObject home = Instantiate(restartButton, restartButton.transform.parent);
        home.name = "HomeButton";
        RectTransform rect = home.GetComponent<RectTransform>();
        rect.anchoredPosition += Vector2.down * (rect.sizeDelta.y + 18f);
        home.GetComponent<Image>().sprite = Resources.Load<Sprite>("HomeButton");
        Button button = home.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();   // drop the copied RestartGame call
        button.onClick.AddListener(GoToHomeScreen);
        return home;
    }
}