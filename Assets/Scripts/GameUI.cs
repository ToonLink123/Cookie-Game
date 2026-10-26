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

    private static readonly Color Ink = new Color(0.06f, 0.10f, 0.16f);
    private Font font;
    private GameObject trackers;
    private GameObject controls;
    private GameObject credits;
    private GameObject creditsButton;
    private GameObject facts;
    private Text cookiesText;
    private Text metersText;
    private Text highScoreText;
    private Text factText;
    private Text persistentSlowText;

    public void Initialize(float highScore)
    {
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

        Image controlsImage = CreateImage("Controls", transform, controlsBoxSprite,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -155f), new Vector2(520f, 150f));
        controls = controlsImage.gameObject;
        Text controlsText = CreateText("ControlsText", controls.transform, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(440f, 100f), 38);
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

        RectTransform buttonRect = CreateRect("CreditsButton", transform, new Vector2(1f, 0f),
            new Vector2(270f, 70f));
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-40f, 32f);
        creditsButton = buttonRect.gameObject;
        Image buttonImage = creditsButton.AddComponent<Image>();
        buttonImage.color = new Color(0.76f, 0.77f, 0.79f);
        Button button = creditsButton.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(ToggleCredits);
        Text buttonText = CreateText("Label", creditsButton.transform, new Vector2(0.5f, 0.5f),
            new Vector2(-25f, 0f), new Vector2(190f, 60f), 30);
        buttonText.text = "Credits";
        CreateImage("Arrow", creditsButton.transform, creditsArrowSprite, new Vector2(0.5f, 0.5f),
            new Vector2(90f, 0f), new Vector2(48f, 36f));

        Image factsImage = CreateImage("DeathFact", transform, factsBoxSprite, new Vector2(0.5f, 0.5f),
            new Vector2(0f, 60f), new Vector2(960f, 400f));
        facts = factsImage.gameObject;
        factText = CreateText("FactText", facts.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(820f, 290f), 34);
        facts.SetActive(false);
        UpdateTrackers(0, 0f);
    }

    public void ShowGameplay(float carriedSlow)
    {
        controls.SetActive(false);
        credits.SetActive(false);
        creditsButton.SetActive(false);
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
