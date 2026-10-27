using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Cookie consent pop-ups that interrupt the flight, a caricature of real ones. "Accept All" is always big
// and easy, but lets a wave of cookies in. Rejecting gets harder each time:
//   1. a plain "Reject All"                 2. reject is small, grey, and swaps sides
//   3. the reject button dodges the cursor   4. no reject at all: "Manage preferences", switch off every
//   5. guilt-trip wording, then an              purpose (some are hidden further down, under "legitimate
//      "Are you sure?"                          interest"), then confirm
// After that it mixes stages 3-5. You keep flying with Space while clicking, and ignoring a banner
// counts down to "continuing means you accept".
public class ConsentBanner : MonoBehaviour
{
    private const float FirstBannerMeters = 20f;
    private const float MinGap = 25f;            // seconds of flight between banners
    private const float MaxGap = 35f;
    private const float AutoAccept = 8f;
    private const float AutoAcceptWithMenus = 15f;
    private const int Stages = 5;
    private const float SlideTime = 0.25f;
    private const float DodgeRadius = 130f;
    private const int Dodges = 3;
    private const int WaveBase = 3;              // cookies an Accept All lets in, plus one per stage
    private const int MaxPartialWave = 6;
    private const float RowHeight = 56f;

    private static readonly Vector2 BannerSize = new Vector2(1380f, 260f);
    private static readonly Vector2 ModalSize = new Vector2(940f, 600f);
    private static readonly Vector2 BigButton = new Vector2(300f, 86f);
    private static readonly Color Faint = new Color(0.45f, 0.48f, 0.52f);
    private static readonly string[] Purposes = { "Analytics", "Marketing", "Personalised ads", "Social media" };
    private static readonly string[] Interests =
    {
        "Measure ad performance", "Develop and improve products", "Build a profile of you", "Store info on your device",
    };

    private CookieSpawner spawner;
    private RectTransform layer;
    private RectTransform banner;   // the open pop-up, or null
    private Vector2 restingPosition;
    private float shownFor;
    private int shownCount;
    private int stage;
    private float untilNext = -1f;
    private float untilAutoAccept;
    private Text countdown;
    private int countdownShown;   // seconds on the countdown, so its text is only rebuilt when they tick
    private RectTransform dodging;
    private RectTransform dodgeAround;
    private int dodgesLeft;
    private readonly List<bool> choices = new List<bool>();

    public void Initialize(CookieSpawner cookieSpawner)
    {
        spawner = cookieSpawner;
        layer = UiKit.MakeLayer("Consent Banners");
    }

    void Update()
    {
        if (GameplayManager.currentState == GameState.GameOver)
        {
            if (banner != null) Close();
            return;
        }
        if (GameplayManager.currentState != GameState.Gameplay) return;

        if (banner == null)
        {
            if (GameplayManager.metersTraveled < FirstBannerMeters) return;
            untilNext -= Time.deltaTime;
            if (untilNext <= 0f) Open();
            return;
        }

        // Slides up into place, then counts down to accepting for you.
        shownFor += Time.deltaTime;
        float slide = Mathf.SmoothStep(0f, 1f, shownFor / SlideTime);
        banner.anchoredPosition = restingPosition + Vector2.down * (1f - slide) * (banner.sizeDelta.y + 80f);
        untilAutoAccept -= Time.deltaTime;
        int seconds = Mathf.CeilToInt(untilAutoAccept);
        if (seconds != countdownShown)
        {
            countdownShown = seconds;
            countdown.text = "Continuing to fly means you accept all cookies (" + seconds + ")";
        }
        if (untilAutoAccept <= 0f)
        {
            AcceptAll("Accepted by default!");
            return;
        }
        if (dodging != null && dodgesLeft > 0) Dodge();
    }

    private void Open()
    {
        shownCount++;
        stage = shownCount <= Stages ? shownCount : Random.Range(3, Stages + 1);
        shownFor = 0f;
        dodging = null;
        untilAutoAccept = stage == 4 ? AutoAcceptWithMenus : AutoAccept;
        countdownShown = -1;
        if (stage == 4) OpenPreferencesFront();
        else OpenBanner();
    }

    // ---- The bottom banner (stages 1, 2, 3 and 5) ----

    private void OpenBanner()
    {
        NewPanel(new Vector2(1f, 0f), BannerSize, new Vector2(-24f, 24f));
        Header("<b>We value your privacy</b>",
            "We and our 742 partners use cookies to make your flight tastier, measure you, and sell you snacks.");
        switch (stage)
        {
            case 1:
                BannerButton("Reject All", new Vector2(-440f, 0f), new Vector2(240f, 70f), 28, Reject, false);
                BannerButton("Accept All", new Vector2(-170f, 0f), new Vector2(240f, 70f), 28, AcceptAll, false);
                break;
            case 2:
            {
                // The choice that matters shrinks into small grey text, and the buttons trade places.
                bool swapped = Random.value < 0.5f;
                BannerButton("Accept All", new Vector2(swapped ? -470f : -190f, 0f), BigButton, 34, AcceptAll, false);
                Button reject = BannerButton("reject", new Vector2(swapped ? -170f : -500f, -40f), new Vector2(130f, 40f),
                    18, Reject, true);
                reject.GetComponentInChildren<Text>().color = Faint;
                break;
            }
            case 3:
                dodgeAround = (RectTransform)BannerButton("Accept All", new Vector2(-190f, 0f), BigButton, 34, AcceptAll,
                    false).transform;
                dodging = (RectTransform)BannerButton("Reject All", new Vector2(-480f, 0f), new Vector2(220f, 64f), 26, Reject,
                    true).transform;
                dodgesLeft = Dodges;
                break;
            default:
                BannerButton("Accept All", new Vector2(-190f, 0f), BigButton, 34, AcceptAll, false);
                BannerButton("No thanks, I hate free cookies", new Vector2(-500f, -40f), new Vector2(250f, 50f), 18,
                    AreYouSure, true);
                break;
        }
    }

    // Stage 5: refusing gets a second, guiltier question.
    private void AreYouSure()
    {
        Clear();
        Header("<b>Are you sure?</b>", "You'll miss out on personalised cookies baked just for you!");
        BannerButton("Yes, accept all", new Vector2(-190f, 0f), BigButton, 30, AcceptAll, false);
        BannerButton("Reject anyway", new Vector2(-500f, -40f), new Vector2(170f, 44f), 20, Reject, true);
    }

    // The reject button hops away when the cursor gets close (a few times, then it gives up).
    private void Dodge()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(banner, mouse.position.ReadValue(), null, out Vector2 pointer);
        if (Vector2.Distance(pointer, CenterInBanner(dodging)) > DodgeRadius) return;

        Vector2 size = dodging.sizeDelta;
        for (int tries = 0; tries < 20; tries++)
        {
            Vector2 spot = new Vector2(Random.Range(-BannerSize.x + 200f + size.x / 2f, -size.x / 2f - 30f),
                Random.Range(-BannerSize.y / 2f + size.y / 2f + 34f, BannerSize.y / 2f - size.y / 2f - 20f));
            Vector2 local = new Vector2(spot.x, BannerSize.y / 2f + spot.y);
            Vector2 gap = spot - dodgeAround.anchoredPosition;
            bool clearOfAccept = Mathf.Abs(gap.x) > (size.x + BigButton.x) / 2f + 10f
                || Mathf.Abs(gap.y) > (size.y + BigButton.y) / 2f + 10f;
            if (!clearOfAccept || Vector2.Distance(local, pointer) < DodgeRadius * 2f) continue;
            dodging.anchoredPosition = spot;
            break;
        }
        dodgesLeft--;
    }

    private Vector2 CenterInBanner(RectTransform rect)
    {
        return banner.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
    }

    // ---- The preferences pop-up (stage 4) ----

    private void OpenPreferencesFront()
    {
        // Sits right of centre, so the balloon at the left stays in view.
        NewPanel(new Vector2(0.5f, 0.5f), ModalSize, new Vector2(140f, 0f));
        Title("<b>Your privacy choices</b>", 40f);
        Paragraph("We use cookies and similar tech to personalise content, measure you, and share data with our "
            + "1,206 trusted partners. You can change your mind at any time (it's in the settings, somewhere).", 40f);
        ModalButton("Accept All", new Vector2(160f, -150f), BigButton, 34, AcceptAll, false);
        ModalButton("Manage preferences", new Vector2(-200f, -150f), new Vector2(240f, 50f), 20, OpenPreferences, true);
    }

    private void OpenPreferences()
    {
        Clear();
        Title("<b>Manage preferences</b>", 40f);
        choices.Clear();

        RectTransform list = UiKit.MakeRect("List", banner, new Vector2(0.5f, 0.5f), new Vector2(840f, 320f));
        list.anchoredPosition = new Vector2(0f, 20f);
        Image backing = list.gameObject.AddComponent<Image>();
        backing.color = new Color(0f, 0f, 0f, 0.06f);   // also catches the mouse wheel for scrolling
        list.gameObject.AddComponent<RectMask2D>();
        RectTransform rows = UiKit.MakeRect("Rows", list, new Vector2(0.5f, 1f), new Vector2(820f, 0f));
        rows.pivot = new Vector2(0.5f, 1f);
        ScrollRect scroll = list.gameObject.AddComponent<ScrollRect>();
        scroll.content = rows;
        scroll.viewport = list;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.scrollSensitivity = 40f;

        float y = 0f;
        Row(rows, ref y, "Strictly necessary", false, "Always on");
        foreach (string purpose in Purposes) Row(rows, ref y, purpose, true, null);
        Row(rows, ref y, "<b>Legitimate interest</b> (switched on separately)", false, "");
        foreach (string interest in Interests) Row(rows, ref y, interest, true, null);
        rows.sizeDelta = new Vector2(820f, -y);

        UiKit.MakeText("Scroll Hint", banner, new Vector2(0.5f, 0.5f), new Vector2(330f, -152f), new Vector2(200f, 26f), 18)
            .text = "scroll for more...";
        ModalButton("Confirm my choices", new Vector2(-230f, -220f), new Vector2(240f, 50f), 20, Confirm, true);
        ModalButton("Accept All", new Vector2(190f, -220f), new Vector2(280f, 76f), 32, AcceptAll, false);
    }

    private void Row(RectTransform rows, ref float y, string label, bool toggle, string note)
    {
        Text text = UiKit.MakeText("Row", rows, new Vector2(0f, 1f), new Vector2(20f, y - RowHeight / 2f),
            new Vector2(600f, RowHeight - 8f), 24);
        text.rectTransform.pivot = new Vector2(0f, 0.5f);
        text.alignment = TextAnchor.MiddleLeft;
        text.text = label;
        if (toggle)
        {
            int index = choices.Count;
            choices.Add(true);   // switched on for you, of course
            Button button = UiKit.MakeButton("ON", rows, new Vector2(1f, 1f), new Vector2(-75f, y - RowHeight / 2f),
                new Vector2(110f, 42f), 22, null);
            button.onClick.AddListener(() => Flip(index, button));
        }
        else if (!string.IsNullOrEmpty(note))
        {
            Text noteText = UiKit.MakeText("Note", rows, new Vector2(1f, 1f), new Vector2(-75f, y - RowHeight / 2f),
                new Vector2(140f, 40f), 20);
            noteText.text = note;
            noteText.color = Faint;
        }
        y -= RowHeight;
    }

    private void Flip(int index, Button button)
    {
        choices[index] = !choices[index];
        Image image = (Image)button.targetGraphic;
        image.sprite = choices[index] ? UiKit.BlueButton : UiKit.Box;
        image.pixelsPerUnitMultiplier = choices[index] ? 0.25f : 0.5f;
        button.GetComponentInChildren<Text>().text = choices[index] ? "ON" : "OFF";
    }

    private void Confirm()
    {
        int stillOn = choices.FindAll(on => on).Count;
        if (stillOn == 0)
        {
            Reject();
            return;
        }
        GameplayManager.bannersAccepted++;
        FloatingText.AtPlayer("Missed " + stillOn + (stillOn == 1 ? " switch" : " switches") + "!", FloatingText.Bad);
        spawner.SpawnConsentWave(Mathf.Min(stillOn, MaxPartialWave));
        Close();
    }

    // ---- Outcomes ----

    private void AcceptAll()
    {
        AcceptAll("Accepted all cookies!");
    }

    private void AcceptAll(string message)
    {
        int wave = WaveBase + stage;
        GameplayManager.bannersAccepted++;
        FloatingText.AtPlayer(message, FloatingText.Bad);
        FloatingText.AtPlayer(wave + " cookies incoming!", FloatingText.Bad);
        spawner.SpawnConsentWave(wave);
        Close();
    }

    private void Reject()
    {
        GameplayManager.bannersRejected++;
        FloatingText.AtPlayer("Cookies rejected!", FloatingText.Good);
        Close();
    }

    private void Close()
    {
        if (banner != null) Destroy(banner.gameObject);
        banner = null;
        dodging = null;
        untilNext = Random.Range(MinGap, MaxGap);
    }

    // ---- Building blocks ----

    private void NewPanel(Vector2 anchor, Vector2 size, Vector2 position)
    {
        Image panel = UiKit.MakeBox("Cookie Pop-up", layer, UiKit.Box, anchor, position, size, 3f);
        panel.rectTransform.pivot = anchor;
        banner = panel.rectTransform;
        restingPosition = position;
        banner.anchoredPosition = position + Vector2.down * (size.y + 80f);
        AddCountdown();
    }

    // Empties the pop-up for its next page, keeping the countdown.
    private void Clear()
    {
        foreach (Transform child in banner) Destroy(child.gameObject);
        dodging = null;
        AddCountdown();
    }

    private void AddCountdown()
    {
        countdown = UiKit.MakeText("Countdown", banner, new Vector2(0f, 0f), new Vector2(30f, 26f),
            new Vector2(700f, 30f), 20);
        countdown.rectTransform.pivot = new Vector2(0f, 0.5f);
        countdown.alignment = TextAnchor.MiddleLeft;
        countdown.color = Faint;
    }

    private void Header(string title, string body)
    {
        UiKit.MakeImage("Cookie", banner, UiKit.Cookie, new Vector2(0f, 0.5f), new Vector2(100f, 12f),
            new Vector2(110f, 110f));
        Text heading = UiKit.MakeText("Title", banner, new Vector2(0f, 1f), new Vector2(190f, -52f), new Vector2(580f, 52f), 36);
        heading.rectTransform.pivot = new Vector2(0f, 0.5f);
        heading.alignment = TextAnchor.MiddleLeft;
        heading.text = title;
        Text text = UiKit.MakeText("Body", banner, new Vector2(0f, 0.5f), new Vector2(190f, -6f), new Vector2(580f, 96f), 24);
        text.rectTransform.pivot = new Vector2(0f, 0.5f);
        text.alignment = TextAnchor.UpperLeft;
        text.text = body;
    }

    private void Title(string title, float fromTop)
    {
        Text heading = UiKit.MakeText("Title", banner, new Vector2(0.5f, 1f), new Vector2(0f, -fromTop - 20f),
            new Vector2(ModalSize.x - 80f, 52f), 38);
        heading.text = title;
    }

    private void Paragraph(string body, float offset)
    {
        UiKit.MakeText("Body", banner, new Vector2(0.5f, 0.5f), new Vector2(0f, offset), new Vector2(ModalSize.x - 120f, 200f), 26)
            .text = body;
    }

    // Banner buttons are placed from the banner's right-hand side.
    private Button BannerButton(string label, Vector2 fromRight, Vector2 size, int fontSize, UnityAction onClick, bool grey)
    {
        return UiKit.MakeButton(label, banner, new Vector2(1f, 0.5f), fromRight, size, fontSize, onClick, grey);
    }

    private Button ModalButton(string label, Vector2 fromCenter, Vector2 size, int fontSize, UnityAction onClick, bool grey)
    {
        return UiKit.MakeButton(label, banner, new Vector2(0.5f, 0.5f), fromCenter, size, fontSize, onClick, grey);
    }
}
