using System.Collections;
using System.Collections.Generic;
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

    // Between the high score text and the start button, leaving room for the hover.
    private static readonly Vector2 TitlePosition = new Vector2(0f, 242f);
    private const float TitleHover = 10f;
    private const float StartButtonPulse = 0.05f;
    // Every cookie type, in the order the legend and game over screen list them; details follow the name.
    private static readonly (CookieType type, string details)[] LegendEntries =
    {
        (CookieType.FirstParty, "\nFlies straight. Click once\nto reject. Slows you 7%."),
        (CookieType.Session, "\nFast, flickery, and expires\nby itself. Slows 20%, briefly."),
        (CookieType.LegitimateInterest, "  (50 m+)\nTurns itself back on once\nafter you reject it."),
        (CookieType.ThirdParty, "  (100 m+)\nTracks you and dodges the\ncursor. Too many = ads."),
        (CookieType.Persistent, "  (220 m+)\n3 clicks to reject. Slows\n10%, and your next game."),
    };

    // The game over screen's two glass jars: cookies accepted (eaten) on the left, rejected on the right.
    private static readonly Vector2 AcceptedJarPosition = new Vector2(-720f, -40f);
    private static readonly Vector2 RejectedJarPosition = new Vector2(720f, -40f);
    private static readonly Color Glass = new Color(0.75f, 0.9f, 1f, 0.55f);
    private const int MaxJarCookies = 35;
    private const float DropGap = 0.06f;
    private const float DropTime = 0.35f;

    private GameObject trackers;
    private GameObject controls;
    private GameObject credits;
    private GameObject legend;
    private GameObject creditsButton;
    private GameObject legendButton;
    private Text cookiesText;
    private Text metersText;
    private Text highScoreText;
    private Text factText;
    private Text persistentSlowText;
    private RectTransform title;
    private RectTransform startButton;
    private GameObject quitButton;
    private Sprite cookieSprite;
    private CanvasGroup gameOver;
    private Text acceptedCount;
    private Text rejectedCount;
    private Text[] eatenCounts;
    private RectTransform acceptedJar;
    private RectTransform rejectedJar;
    private GameObject pauseScreen;
    private int shownCookies = -1;
    private int shownMeters = -1;

    public void Initialize(float highScore, Sprite cookieSprite, RectTransform startButton)
    {
        this.startButton = startButton;
        this.cookieSprite = cookieSprite;
        UiKit.Setup(GetComponentInParent<Canvas>(), factsBoxSprite, creditsBoxSprite, creditsArrowSprite, cookieSprite);

        trackers = CreateRect("Trackers", transform, Vector2.zero, Vector2.zero).gameObject;
        UiKit.Stretch(trackers.GetComponent<RectTransform>());
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
            Vector2.zero, new Vector2(230f, 62f), 19);
        controlsText.text = "<b>Controls</b>\nSpace to fly\nClick cookies to reject";

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
            new Vector2(0f, -62f), new Vector2(440f, 60f), 40);
        legendTitle.text = "<b>Cookie Legend</b>";
        for (int i = 0; i < LegendEntries.Length; i++)
        {
            CreateLegendEntry(LegendEntries[i].type, LegendEntries[i].details, -100f - i * 126f);
        }
        legend.SetActive(false);

        creditsButton = CreateMenuButton("Credits", new Vector2(1f, 0f), ToggleCredits);
        legendButton = CreateMenuButton("Legend", Vector2.zero, ToggleLegend);

        BuildGameOver();
        UpdateTrackers(0, 0f);
    }

    // Everything on the game over screen sits in one group, so it can fade in together.
    private void BuildGameOver()
    {
        RectTransform group = CreateRect("GameOver", transform, Vector2.zero, Vector2.zero);
        UiKit.Stretch(group);
        gameOver = group.gameObject.AddComponent<CanvasGroup>();

        Image factsImage = CreateImage("DeathFact", group, factsBoxSprite, new Vector2(0.5f, 0.5f),
            new Vector2(0f, 60f), new Vector2(960f, 400f));
        factText = CreateText("FactText", factsImage.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(820f, 290f), 34);

        // How many of each cookie type the player ate, between the high score and the fact.
        RectTransform eatenRect = CreateRect("CookiesEaten", group, new Vector2(0.5f, 1f), new Vector2(1500f, 70f));
        eatenRect.pivot = new Vector2(0.5f, 1f);
        eatenRect.anchoredPosition = new Vector2(0f, -120f);
        eatenCounts = new Text[LegendEntries.Length];
        for (int i = 0; i < LegendEntries.Length; i++)
        {
            CookieType type = LegendEntries[i].type;
            RectTransform entry = CreateRect(type + "Eaten", eatenRect, new Vector2(0.5f, 0f), new Vector2(290f, 70f));
            entry.pivot = new Vector2(0.5f, 0f);
            entry.anchoredPosition = new Vector2((i - 2f) * 300f, 0f);
            CreateCookieIcon(type, entry, new Vector2(0f, 0.5f), new Vector2(32f, 0f), 56f);
            Text count = CreateText("Count", entry, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(226f, 60f), 28);
            count.rectTransform.pivot = new Vector2(1f, 0.5f);
            count.alignment = TextAnchor.MiddleLeft;
            eatenCounts[(int)type] = count;
        }

        // The jars are the tall grey box tinted like glass, each with its count underneath.
        acceptedJar = CreateJar("AcceptedJar", group, AcceptedJarPosition, out acceptedCount);
        rejectedJar = CreateJar("RejectedJar", group, RejectedJarPosition, out rejectedCount);

        group.gameObject.SetActive(false);
    }

    private RectTransform CreateJar(string name, Transform parent, Vector2 position, out Text count)
    {
        Image jar = UiKit.MakeBox(name, parent, creditsBoxSprite, new Vector2(0.5f, 0.5f), position,
            new Vector2(240f, 300f));
        jar.color = Glass;
        count = CreateText("Count", parent, new Vector2(0.5f, 0.5f), position + Vector2.down * 178f,
            new Vector2(320f, 40f), 30);
        UiKit.AddOutline(count, Color.white, 1.5f);
        return jar.rectTransform;
    }

    // Pause screen: dims the game, with resume and quit to home buttons.
    public void CreatePauseScreen(UnityEngine.Events.UnityAction resume, UnityEngine.Events.UnityAction quitToHome)
    {
        // Parented to the canvas itself and drawn last, so it covers everything else on screen.
        RectTransform screen = UiKit.MakeLayer("PauseScreen");
        Image dim = screen.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.5f);   // also stops clicks reaching anything underneath
        pauseScreen = screen.gameObject;

        Image panel = CreateImage("Panel", screen, factsBoxSprite, new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(960f, 320f));
        Text heading = CreateText("Title", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f),
            new Vector2(400f, 70f), 56);
        heading.text = "<b>Paused</b>";

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
        gameOver.gameObject.SetActive(false);
        trackers.SetActive(true);
        persistentSlowText.text = "Persistent cookies from last game: "
            + Mathf.RoundToInt(carriedSlow * 100f) + "% slower";
        persistentSlowText.gameObject.SetActive(true);
    }

    public void UpdateTrackers(int cookies, float meters)
    {
        int wholeMeters = Mathf.FloorToInt(meters);
        if (cookies != shownCookies) cookiesText.text = (shownCookies = cookies).ToString();
        if (wholeMeters != shownMeters) metersText.text = (shownMeters = wholeMeters).ToString();
    }

    public void ShowGameOver(float highScore, bool newHighScore)
    {
        SetHighScore(highScore, newHighScore);
        factText.text = CookieFacts.ForRun();
        persistentSlowText.gameObject.SetActive(false);
        for (int i = 0; i < eatenCounts.Length; i++)
        {
            eatenCounts[i].text = Cookie.Name((CookieType)i) + ": " + GameplayManager.cookiesEaten[i];
        }
        gameOver.gameObject.SetActive(true);
        StartCoroutine(PlayGameOver());
    }

    // Fades the screen in, then counts the run up while cookies drop into the two jars.
    private IEnumerator PlayGameOver()
    {
        int rejected = GameplayManager.bannersRejected, accepted = GameplayManager.cookieCount + GameplayManager.bannersAccepted;
        foreach (int count in GameplayManager.cookiesRejected) rejected += count;
        List<CookieType> eaten = Expand(GameplayManager.cookiesEaten, MaxJarCookies);
        List<CookieType> trashed = Expand(GameplayManager.cookiesRejected, MaxJarCookies);
        SetTally(0, 0);

        for (float time = 0f; time < 0.4f; time += Time.unscaledDeltaTime)
        {
            gameOver.alpha = time / 0.4f;
            yield return null;
        }
        gameOver.alpha = 1f;

        int drops = Mathf.Max(eaten.Count, trashed.Count);
        for (int i = 0; i < drops; i++)
        {
            if (i < eaten.Count) StartCoroutine(Drop(eaten[i], acceptedJar, JarSlot(i)));
            if (i < trashed.Count) StartCoroutine(Drop(trashed[i], rejectedJar, JarSlot(i)));
            float progress = (i + 1f) / drops;
            SetTally(Mathf.RoundToInt(rejected * progress), Mathf.RoundToInt(accepted * progress));
            yield return new WaitForSecondsRealtime(DropGap);
        }
        SetTally(rejected, accepted);
    }

    private void SetTally(int rejected, int accepted)
    {
        acceptedCount.text = "<b>Accepted: " + accepted + "</b>";
        rejectedCount.text = "<b>Rejected: " + rejected + "</b>";
    }

    private static List<CookieType> Expand(int[] counts, int cap)
    {
        List<CookieType> cookies = new List<CookieType>();
        for (int type = 0; type < counts.Length; type++)
            for (int i = 0; i < counts[type] && cookies.Count < cap; i++) cookies.Add((CookieType)type);
        return cookies;
    }

    // Cookies stack up from the bottom of the jar, five to a row.
    private static Vector2 JarSlot(int index)
    {
        return new Vector2(-64f + index % 5 * 32f + Random.Range(-4f, 4f), -100f + index / 5 * 30f);
    }

    // One cookie falls from above into a jar.
    private IEnumerator Drop(CookieType type, RectTransform jar, Vector2 landing)
    {
        Image icon = CreateCookieIcon(type, jar, new Vector2(0.5f, 0.5f), landing + Vector2.up * 260f, 30f);
        RectTransform rect = icon.rectTransform;
        Vector2 from = rect.anchoredPosition;
        float spin = Random.Range(-360f, 360f);
        for (float time = 0f; time < DropTime; time += Time.unscaledDeltaTime)
        {
            float fall = time / DropTime;
            rect.anchoredPosition = Vector2.Lerp(from, landing, fall * fall);
            rect.localRotation = Quaternion.Euler(0f, 0f, spin * fall);
            yield return null;
        }
        rect.anchoredPosition = landing;
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
            new Vector2(440f, 122f));
        entry.pivot = new Vector2(0.5f, 1f);
        entry.anchoredPosition = new Vector2(0f, y);

        CreateCookieIcon(type, entry, new Vector2(0f, 0.5f), new Vector2(50f, 0f), 72f);
        Text text = CreateText("Description", entry, new Vector2(1f, 0.5f), Vector2.zero,
            new Vector2(340f, 122f), 24);
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

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 anchor,
        Vector2 position, Vector2 size)
    {
        return UiKit.MakeImage(name, parent, sprite, anchor, position, size);
    }

    private static Text CreateText(string name, Transform parent, Vector2 anchor, Vector2 position,
        Vector2 size, int fontSize)
    {
        return UiKit.MakeText(name, parent, anchor, position, size, fontSize);
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 size)
    {
        return UiKit.MakeRect(name, parent, anchor, size);
    }
}
