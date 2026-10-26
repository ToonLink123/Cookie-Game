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
    private static readonly (CookieType type, string description)[] LegendEntries =
    {
        (CookieType.FirstParty, "<b>First-Party</b>\nFlies straight.\nSlows you 7%."),
        (CookieType.Session, "<b>Session</b>\nFast and flickery.\nSlows you 20%, briefly."),
        (CookieType.ThirdParty, "<b>Third-Party</b>  (100 m+)\nTracks you! Slows you 7%.\nEat too many and\nobstacles turn into ads."),
        (CookieType.Persistent, "<b>Persistent</b>  (220 m+)\nComes back twice.\nSlows you 10%, and your\nnext game too."),
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

    public void Initialize(float highScore, Sprite cookieSprite, RectTransform startButton)
    {
        this.startButton = startButton;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        // Same top edge as before so it still sits just under the start button.
        Image controlsImage = CreateImage("Controls", transform, controlsBoxSprite,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -118f), new Vector2(260f, 75f));
        controls = controlsImage.gameObject;
        Text controlsText = CreateText("ControlsText", controls.transform, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(220f, 50f), 19);
        controlsText.text = "<b>Controls</b>\nSpace to Jump";

        Image creditsImage = CreateImage("Credits", transform, creditsBoxSprite,
            new Vector2(1f, 0.5f), new Vector2(-40f, -20f), new Vector2(520f, 760f));
        creditsImage.rectTransform.pivot = new Vector2(1f, 0.5f);
        credits = creditsImage.gameObject;
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
        Text legendTitle = CreateText("Title", legend.transform, new Vector2(0.5f, 1f),
            new Vector2(0f, -75f), new Vector2(440f, 60f), 40);
        legendTitle.text = "<b>Cookie Legend</b>";
        for (int i = 0; i < LegendEntries.Length; i++)
        {
            CreateLegendEntry(LegendEntries[i].type, LegendEntries[i].description, cookieSprite,
                -125f - i * 152f);
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
        UpdateTrackers(0, 0f);
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

    // A grey home-screen button in a bottom corner, with its arrow pointing toward that side.
    private GameObject CreateMenuButton(string label, Vector2 corner, UnityEngine.Events.UnityAction onClick)
    {
        float side = corner.x > 0.5f ? 1f : -1f;
        RectTransform buttonRect = CreateRect(label + "Button", transform, corner, new Vector2(270f, 70f));
        buttonRect.pivot = corner;
        buttonRect.anchoredPosition = new Vector2(-40f * side, 32f);
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = new Color(0.76f, 0.77f, 0.79f);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);
        Text buttonText = CreateText("Label", buttonRect, new Vector2(0.5f, 0.5f),
            new Vector2(-25f * side, 0f), new Vector2(190f, 60f), 30);
        buttonText.text = label;
        Image arrow = CreateImage("Arrow", buttonRect, creditsArrowSprite, new Vector2(0.5f, 0.5f),
            new Vector2(90f * side, 0f), new Vector2(48f, 36f));
        arrow.rectTransform.localScale = new Vector3(side, 1f, 1f);
        return buttonRect.gameObject;
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

    private void CreateLegendEntry(CookieType type, string description, Sprite cookieSprite, float y)
    {
        RectTransform entry = CreateRect(type + "Entry", legend.transform, new Vector2(0.5f, 1f),
            new Vector2(440f, 140f));
        entry.pivot = new Vector2(0.5f, 1f);
        entry.anchoredPosition = new Vector2(0f, y);

        float iconSize = type == CookieType.Persistent ? 96f : 80f;
        Image icon = CreateImage("Icon", entry, cookieSprite, new Vector2(0f, 0.5f), new Vector2(50f, 0f),
            new Vector2(iconSize, iconSize));
        Color tint = Cookie.Tint(type);
        if (type == CookieType.Session) tint.a = 0.8f;
        icon.color = tint;
        if (type == CookieType.ThirdParty)
        {
            // A still copy of the tracker's eye, looking left like it does in game.
            float eyeWidth = iconSize * CookieEye.SizeOnCookie;
            CreateImage("Eye", icon.transform, CookieEye.EyeSprite, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(eyeWidth, eyeWidth)).preserveAspect = true;
            float pupilSize = eyeWidth * CookieEye.PupilSprite.rect.width / CookieEye.EyeSprite.rect.width;
            CreateImage("Pupil", icon.transform, CookieEye.PupilSprite, new Vector2(0.5f, 0.5f),
                new Vector2(-CookieEye.PupilRangeX * eyeWidth, 0f), new Vector2(pupilSize, pupilSize));
        }

        Text text = CreateText("Description", entry, new Vector2(1f, 0.5f), Vector2.zero,
            new Vector2(330f, 140f), 26);
        text.rectTransform.pivot = new Vector2(1f, 0.5f);
        text.alignment = TextAnchor.MiddleLeft;
        text.text = description;
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
