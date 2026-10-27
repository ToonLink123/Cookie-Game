using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] private Sprite cookieTrackerSprite;
    [SerializeField] private Sprite metersTrackerSprite;
    [SerializeField] private Sprite creditsBoxSprite;
    [SerializeField] private Sprite factsBoxSprite;
    [SerializeField] private Sprite controlsBoxSprite;
    [SerializeField] private Sprite creditsArrowSprite;
    [SerializeField] private Sprite titleSprite;

    private static readonly Color Ink = new Color(0.06f, 0.10f, 0.16f);
    // Between the high score text and the start button, leaving room for the hover.
    private static readonly Vector2 TitlePosition = new Vector2(0f, 242f);
    private const float TitleHover = 10f;
    private const float StartButtonPulse = 0.05f;
    // Every cookie type, in the order the legend and game over screen list them; details follow the name.
    private static readonly (CookieType type, string details)[] LegendEntries =
    {
        (CookieType.FirstParty, "\nFlies straight.\nSlows you 7%."),
        (CookieType.Session, "\nFast and flickery.\nSlows you 20%, briefly."),
        (CookieType.ThirdParty, "  (100 m+)\nTracks you! Slows you 7%.\nEat too many and\nobstacles turn into ads."),
        (CookieType.Persistent, "  (220 m+)\nComes back twice.\nSlows you 10%, and your\nnext game too."),
    };

    private Font font;
    private GameObject trackers;
    private GameObject controls;
    private GameObject credits;
    private GameObject legend;
    private GameObject creditsButton;
    private GameObject legendButton;
    private GameObject facts;
    private Text cookiesText;
    private Text metersText;
    private Text highScoreText;
    private Text factText;
    private Text persistentSlowText;
    private RectTransform title;
    private RectTransform startButton;
    private GameObject quitButton;
    private Sprite cookieSprite;
    private GameObject eatenPanel;
    private Text[] eatenCounts;
    private GameObject pauseScreen;

    public void Initialize(float highScore, Sprite cookieSprite, RectTransform startButton)
    {
        this.startButton = startButton;
        this.cookieSprite = cookieSprite;
        font = LoadFont();
        trackers = CreateRect("Trackers", transform, Vector2.zero, Vector2.zero).gameObject;
        Stretch(trackers.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        cookiesText = CreateTracker("Cookies", cookieTrackerSprite, new Vector2(0f, 1f),
            new Vector2(32f, -32f));
        metersText = CreateTracker("Meters", metersTrackerSprite, Vector2.one,
            new Vector2(-32f, -32f));
        trackers.SetActive(false);

        persistentSlowText = CreateText("PersistentSlow", transform, Vector2.zero, new Vector2(32f, 32f),
            new Vector2(760f, 50f), 30);
        persistentSlowText.rectTransform.pivot = Vector2.zero;
        persistentSlowText.alignment = TextAnchor.LowerLeft;
        persistentSlowText.gameObject.SetActive(false);

        highScoreText = CreateText("HighScore", transform, new Vector2(0.5f, 1f),
            new Vector2(0f, -34f), new Vector2(550f, 60f), 34);
        highScoreText.rectTransform.pivot = new Vector2(0.5f, 1f);
        SetHighScore(highScore, false);

        Image titleImage = CreateImage("Title", transform, titleSprite, new Vector2(0.5f, 0.5f),
            TitlePosition, new Vector2(640f, 320f));
        titleImage.preserveAspect = true;
        titleImage.enabled = titleSprite != null;
        if (titleSprite == null) Debug.LogWarning("GameUI: drag Assets/Art/UI/cookieTitle.png into Title Sprite.");
        title = titleImage.rectTransform;

        // Quit sits just under the start button, and the controls box just under that.
        quitButton = CreateSpriteButton("Quit", transform, Resources.Load<Sprite>("QuitButton"),
            new Vector2(0f, -90f), GameplayManager.QuitGame);
        Image controlsImage = CreateImage("Controls", transform, controlsBoxSprite,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -182f), new Vector2(260f, 75f));
        controls = controlsImage.gameObject;
        Text controlsText = CreateText("ControlsText", controls.transform, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(220f, 50f), 19);
        controlsText.text = "<b>Controls</b>\nSpace to Jump";

        Image creditsImage = CreateImage("Credits", transform, creditsBoxSprite,
            new Vector2(1f, 0.5f), new Vector2(-40f, -20f), new Vector2(520f, 760f));
        creditsImage.rectTransform.pivot = new Vector2(1f, 0.5f);
        credits = creditsImage.gameObject;
        creditsImage.raycastTarget = true;   // so clicks on the panel don't pop the menu cookies behind it
        Text creditsText = CreateText("CreditsText", credits.transform, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(440f, 670f), 26);
        creditsText.text = "<b>Cookies:</b>\n\n"
            + "<b>Team Lead / Engine Integration</b>\nLogan and William\n(also supporting programming)\n\n"
            + "<b>Lead Programmer</b>\nEshan\n\n"
            + "<b>Gameplay Programmer</b>\nTed\nRyan\n\n"
            + "<b>Lead Artist</b>\nJordan\n\n"
            + "<b>Artist</b>\nTravis\nAlex\nMax\nSarah\n\n"
            + "<b>Tech/Art Flex + Problem solver</b>\nXander";
        credits.SetActive(false);

        // Cookie legend: mirrors the credits panel on the left side of the home screen.
        Image legendImage = CreateImage("CookieLegend", transform, creditsBoxSprite,
            new Vector2(0f, 0.5f), new Vector2(40f, -20f), new Vector2(520f, 760f));
        legendImage.rectTransform.pivot = new Vector2(0f, 0.5f);
        legend = legendImage.gameObject;
        legendImage.raycastTarget = true;   // so clicks on the panel don't pop the menu cookies behind it
        Text legendTitle = CreateText("Title", legend.transform, new Vector2(0.5f, 1f),
            new Vector2(0f, -75f), new Vector2(440f, 60f), 40);
        legendTitle.text = "<b>Cookie Legend</b>";
        for (int i = 0; i < LegendEntries.Length; i++)
        {
            CreateLegendEntry(LegendEntries[i].type, LegendEntries[i].details, -125f - i * 152f);
        }
        legend.SetActive(false);

        creditsButton = CreateMenuButton("Credits", new Vector2(1f, 0f), ToggleCredits);
        legendButton = CreateMenuButton("Legend", Vector2.zero, ToggleLegend);

        Image factsImage = CreateImage("DeathFact", transform, factsBoxSprite, new Vector2(0.5f, 0.5f),
            new Vector2(0f, 60f), new Vector2(960f, 400f));
        facts = factsImage.gameObject;
        factText = CreateText("FactText", facts.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(820f, 290f), 34);
        facts.SetActive(false);

        // Game over: how many of each cookie type the player ate, between the high score and the fact.
        RectTransform eatenRect = CreateRect("CookiesEaten", transform, new Vector2(0.5f, 1f),
            new Vector2(1200f, 130f));
        eatenRect.pivot = new Vector2(0.5f, 1f);
        eatenRect.anchoredPosition = new Vector2(0f, -110f);
        eatenPanel = eatenRect.gameObject;
        Text eatenTitle = CreateText("Title", eatenRect, new Vector2(0.5f, 1f), new Vector2(0f, -20f),
            new Vector2(600f, 40f), 30);
        eatenTitle.text = "<b>Cookies eaten</b>";
        eatenCounts = new Text[LegendEntries.Length];
        for (int i = 0; i < LegendEntries.Length; i++)
        {
            CookieType type = LegendEntries[i].type;
            RectTransform entry = CreateRect(type + "Eaten", eatenRect, new Vector2(0.5f, 0f),
                new Vector2(290f, 70f));
            entry.pivot = new Vector2(0.5f, 0f);
            entry.anchoredPosition = new Vector2((i - 1.5f) * 300f, 0f);
            CreateCookieIcon(type, entry, new Vector2(0f, 0.5f), new Vector2(35f, 0f), 56f);
            Text count = CreateText("Count", entry, new Vector2(1f, 0.5f), Vector2.zero,
                new Vector2(220f, 60f), 28);
            count.rectTransform.pivot = new Vector2(1f, 0.5f);
            count.alignment = TextAnchor.MiddleLeft;
            eatenCounts[(int)type] = count;
        }
        eatenPanel.SetActive(false);
        UpdateTrackers(0, 0f);
    }

    // Pause screen: dims the game, with resume and quit to home buttons.
    public void CreatePauseScreen(UnityEngine.Events.UnityAction resume, UnityEngine.Events.UnityAction quitToHome)
    {
        // Parented to the canvas itself and drawn last, so it covers everything else on screen.
        RectTransform screen = CreateRect("PauseScreen", GetComponentInParent<Canvas>().transform,
            Vector2.zero, Vector2.zero);
        Stretch(screen, Vector2.zero, Vector2.one);
        Image dim = screen.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.5f);   // also stops clicks reaching anything underneath
        pauseScreen = screen.gameObject;

        Image panel = CreateImage("Panel", screen, factsBoxSprite, new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(960f, 320f));
        Text title = CreateText("Title", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f),
            new Vector2(400f, 70f), 56);
        title.text = "<b>Paused</b>";

        CreateSpriteButton("Resume", panel.transform, Resources.Load<Sprite>("ResumeButton"),
            new Vector2(-190f, -45f), resume);
        CreateSpriteButton("Home", panel.transform, Resources.Load<Sprite>("HomeButton"),
            new Vector2(190f, -45f), quitToHome);
        pauseScreen.SetActive(false);
    }

    public void ShowPause(bool paused)
    {
        pauseScreen.transform.SetAsLastSibling();
        pauseScreen.SetActive(paused);
    }

    // Hand-drawn font matching the lettering in the UI sprites; built by Tools/CookieFont.
    public static Font LoadFont()
    {
        return Resources.Load<Font>("CookieFont");
    }

    void Update()
    {
        if (GameplayManager.currentState != GameState.MainMenu) return;

        // Home screen: the title bobs up and down while the start button pulses up to 5% bigger.
        title.anchoredPosition = TitlePosition + Vector2.up * Mathf.Sin(Time.time * 2f) * TitleHover;
        float pulse = 0.5f - 0.5f * Mathf.Cos(Time.time * 4f);
        startButton.localScale = Vector3.one * (1f + StartButtonPulse * pulse);
    }

    public void ShowGameplay(float carriedSlow)
    {
        title.gameObject.SetActive(false);
        quitButton.SetActive(false);
        controls.SetActive(false);
        credits.SetActive(false);
        legend.SetActive(false);
        creditsButton.SetActive(false);
        legendButton.SetActive(false);
        facts.SetActive(false);
        trackers.SetActive(true);
        persistentSlowText.text = "Persistent cookies from last game: "
            + Mathf.RoundToInt(carriedSlow * 100f) + "% slower";
        persistentSlowText.gameObject.SetActive(true);
    }

    public void UpdateTrackers(int cookies, float meters)
    {
        cookiesText.text = cookies.ToString();
        metersText.text = Mathf.FloorToInt(meters).ToString();
    }

    public void ShowGameOver(float highScore, bool newHighScore)
    {
        SetHighScore(highScore, newHighScore);
        factText.text = CookieFacts.RandomFact();
        facts.SetActive(true);
        persistentSlowText.gameObject.SetActive(false);
        for (int i = 0; i < eatenCounts.Length; i++)
        {
            eatenCounts[i].text = Cookie.Name((CookieType)i) + ": " + GameplayManager.cookiesEaten[i];
        }
        eatenPanel.SetActive(true);
    }

    private void SetHighScore(float highScore, bool newHighScore)
    {
        highScoreText.text = (newHighScore ? "New High Score: " : "High Score: ")
            + Mathf.FloorToInt(highScore) + " m";
    }

    private void ToggleCredits()
    {
        credits.SetActive(!credits.activeSelf);
    }

    private void ToggleLegend()
    {
        legend.SetActive(!legend.activeSelf);
    }

    // A home-screen button in a bottom corner, with its arrow pointing toward that side. The box is the
    // text_box_2 art (identical to Facts.png) at exactly 2x, so its pixels stay crisp.
    private GameObject CreateMenuButton(string label, Vector2 corner, UnityEngine.Events.UnityAction onClick)
    {
        float side = corner.x > 0.5f ? 1f : -1f;
        RectTransform buttonRect = CreateRect(label + "Button", transform, corner, factsBoxSprite.rect.size * 2f);
        buttonRect.pivot = corner;
        buttonRect.anchoredPosition = new Vector2(-40f * side, 32f);
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.sprite = factsBoxSprite;
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);
        // Label and arrow centred together inside the box.
        Text buttonText = CreateText("Label", buttonRect, new Vector2(0.5f, 0.5f),
            new Vector2(-31f * side, 0f), new Vector2(130f, 60f), 30);
        buttonText.text = label;
        Image arrow = CreateImage("Arrow", buttonRect, creditsArrowSprite, new Vector2(0.5f, 0.5f),
            new Vector2(57f * side, 0f), new Vector2(48f, 36f));
        arrow.rectTransform.localScale = new Vector3(side, 1f, 1f);
        return buttonRect.gameObject;
    }

    // A sprite button that looks and reacts like the scene's Start and Restart buttons (sprite at 4x).
    private GameObject CreateSpriteButton(string name, Transform parent, Sprite sprite, Vector2 position,
        UnityEngine.Events.UnityAction onClick)
    {
        Image image = CreateImage(name + "Button", parent, sprite, new Vector2(0.5f, 0.5f), position,
            sprite.rect.size * 4f);
        image.preserveAspect = true;
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        return image.gameObject;
    }

    private Text CreateTracker(string label, Sprite sprite, Vector2 anchor, Vector2 position)
    {
        RectTransform tracker = CreateRect(label + "Tracker", trackers.transform, anchor,
            new Vector2(430f, 85f));
        tracker.pivot = anchor;
        tracker.anchoredPosition = position;
        CreateImage("Label", tracker, sprite, new Vector2(0f, 0.5f),
            new Vector2(125f, 0f), new Vector2(250f, 75f)).preserveAspect = true;
        return CreateText("Value", tracker, new Vector2(1f, 0.5f),
            new Vector2(-85f, 0f), new Vector2(160f, 75f), 42);
    }

    private void CreateLegendEntry(CookieType type, string details, float y)
    {
        RectTransform entry = CreateRect(type + "Entry", legend.transform, new Vector2(0.5f, 1f),
            new Vector2(440f, 140f));
        entry.pivot = new Vector2(0.5f, 1f);
        entry.anchoredPosition = new Vector2(0f, y);

        CreateCookieIcon(type, entry, new Vector2(0f, 0.5f), new Vector2(50f, 0f), 80f);
        Text text = CreateText("Description", entry, new Vector2(1f, 0.5f), Vector2.zero,
            new Vector2(330f, 140f), 26);
        text.rectTransform.pivot = new Vector2(1f, 0.5f);
        text.alignment = TextAnchor.MiddleLeft;
        text.text = "<b>" + Cookie.Name(type) + "</b>" + details;
    }

    // A cookie tinted like its type in game, with the tracker's eye on third-party cookies.
    private Image CreateCookieIcon(CookieType type, Transform parent, Vector2 anchor, Vector2 position, float size)
    {
        // Persistent cookies are bigger in game, so their icon is too.
        if (type == CookieType.Persistent) size *= 1.2f;
        Image icon = CreateImage("Icon", parent, cookieSprite, anchor, position, new Vector2(size, size));
        Color tint = Cookie.Tint(type);
        if (type == CookieType.Session) tint.a = 0.8f;
        icon.color = tint;
        if (type == CookieType.ThirdParty)
        {
            // A still copy of the tracker's eye, looking left like it does in game.
            float eyeWidth = size * CookieEye.SizeOnCookie;
            CreateImage("Eye", icon.transform, CookieEye.EyeSprite, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(eyeWidth, eyeWidth)).preserveAspect = true;
            float pupilSize = eyeWidth * CookieEye.PupilSprite.rect.width / CookieEye.EyeSprite.rect.width;
            CreateImage("Pupil", icon.transform, CookieEye.PupilSprite, new Vector2(0.5f, 0.5f),
                new Vector2(-CookieEye.PupilRangeX * eyeWidth, 0f), new Vector2(pupilSize, pupilSize));
        }
        return icon;
    }

    private Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 anchor,
        Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent, anchor, size);
        rect.anchoredPosition = position;
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private Text CreateText(string name, Transform parent, Vector2 anchor, Vector2 position,
        Vector2 size, int fontSize)
    {
        RectTransform rect = CreateRect(name, parent, anchor, size);
        rect.anchoredPosition = position;
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = Ink;
        text.alignment = TextAnchor.MiddleCenter;
        text.supportRichText = true;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 size)
    {
        GameObject element = new GameObject(name, typeof(RectTransform));
        element.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = element.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = size;
        return rect;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
