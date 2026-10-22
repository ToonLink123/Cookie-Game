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
    public static int cookieCount;

    private static bool restarting;

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject player;
    [SerializeField] private CookieSpawner cookieSpawner;
    [SerializeField] private Text statsText;
    [SerializeField] private GameObject restartButton;

    void Awake()
    {
        currentState = GameState.MainMenu;
        overallSpeed = 1f;
        cookieCount = 0;
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
        statsText.text = "Cookies: " + cookieCount + "\nSpeed: " + (overallSpeed * 100f).ToString("0") + "%";
        restartButton.SetActive(currentState == GameState.GameOver);
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
