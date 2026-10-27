using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The trackers' dossier on you: each tracker cookie eaten fills in another line, ads start quoting it,
// and Clear Browsing Data wipes it. A small tab under the cookie counter pops open whenever it learns more.
public class DataProfile : MonoBehaviour
{
    private const float OpenTime = 3f;
    private const float WipedTime = 2f;
    private const float Width = 430f;
    private const float HeaderHeight = 48f;
    private const float LineHeight = 30f;
    private static readonly Vector2 Position = new Vector2(32f, -128f);   // just under the cookie counter
    private static readonly Color NewLine = new Color(0.8f, 0.1f, 0.35f);

    private static readonly string[] Labels =
    {
        "Location", "Interests", "Device", "Cookies eaten", "Favorite cookie", "Distance flown", "Mood",
        "Likely to buy", "Ad clicks predicted", "Status",
    };

    public static int Known => Mathf.Min(GameplayManager.trackerCookiesHit, Labels.Length);

    private RectTransform panel;
    private Text header;
    private readonly List<Text> lines = new List<Text>();
    private int shownKnown;
    // What the card last showed (lines known, open, cookies, favorite, meters); only redrawn when it changes,
    // since rewriting its text every frame filled memory with throwaway strings.
    private (int, bool, int, CookieType, int) drawn = (-1, false, 0, CookieType.FirstParty, 0);
    private float openFor;
    private float wipedFor;

    public static void Create()
    {
        UiKit.MakeLayer("Data Profile").gameObject.AddComponent<DataProfile>().Build();
    }

    private void Build()
    {
        Image box = UiKit.MakeBox("Profile", transform, UiKit.Box, new Vector2(0f, 1f), Position,
            new Vector2(Width, HeaderHeight), 2f);
        box.raycastTarget = false;
        panel = box.rectTransform;
        panel.pivot = new Vector2(0f, 1f);
        header = Line("Header", 0f, 24);
        for (int i = 0; i < Labels.Length; i++) lines.Add(Line("Line", HeaderHeight + i * LineHeight - 6f, 21));
        panel.gameObject.SetActive(false);
    }

    private Text Line(string name, float fromTop, int fontSize)
    {
        Text text = UiKit.MakeText(name, panel, new Vector2(0f, 1f), new Vector2(18f, -fromTop - 24f),
            new Vector2(Width - 36f, 30f), fontSize);
        text.rectTransform.pivot = new Vector2(0f, 0.5f);
        text.alignment = TextAnchor.MiddleLeft;
        return text;
    }

    // What the trackers have worked out for a given line (some of it is live, from the run so far).
    private static string ValueOf(int line)
    {
        switch (line)
        {
            case 0: return "somewhere in the sky";
            case 1: return "cookies, balloons";
            case 2: return "hot air balloon";
            case 3: return GameplayManager.cookieCount.ToString();
            case 4: return Cookie.Name(FavoriteCookie());
            case 5: return Mathf.FloorToInt(GameplayManager.metersTraveled) + " m";
            case 6: return "hungry";
            case 7: return "more cookies";
            case 8: return "lots";
            default: return "complete - selling to partners";
        }
    }

    private static CookieType FavoriteCookie()
    {
        int best = 0;
        for (int i = 1; i < GameplayManager.cookiesEaten.Length; i++)
            if (GameplayManager.cookiesEaten[i] > GameplayManager.cookiesEaten[best]) best = i;
        return (CookieType)best;
    }

    // A headline for an ad that quotes what the trackers know, or null to keep the ad's own.
    public static string PersonalizedHeadline()
    {
        int known = Known;
        if (known == 0 || Random.value < 0.4f) return null;
        List<string> options = new List<string> { "SKY HIGH DEALS NEAR YOU" };
        if (known >= 2) options.Add("LOVE COOKIES? GET MORE");
        if (known >= 3) options.Add("BALLOON INSURANCE 50% OFF");
        if (known >= 6) options.Add(Mathf.FloorToInt(GameplayManager.metersTraveled) + " M FLOWN! TREAT YOURSELF");
        if (known >= 7) options.Add("HUNGRY? COOKIES NEAR YOU");
        if (known >= 8) options.Add("PILOTS LIKE YOU BUY MORE");
        return options[Random.Range(0, options.Count)];
    }

    void Update()
    {
        GameState state = GameplayManager.currentState;
        if (state == GameState.MainMenu || state == GameState.GameOver)
        {
            panel.gameObject.SetActive(false);
            return;
        }
        if (state == GameState.Paused) return;

        int known = Known;
        if (known > shownKnown) openFor = OpenTime;
        else if (known < shownKnown && known == 0) wipedFor = WipedTime;
        shownKnown = known;
        openFor -= Time.deltaTime;
        wipedFor -= Time.deltaTime;

        bool visible = known > 0 || wipedFor > 0f;
        panel.gameObject.SetActive(visible);
        if (!visible) return;

        bool open = openFor > 0f && known > 0;
        var card = (known, open, GameplayManager.cookieCount, FavoriteCookie(), Mathf.FloorToInt(GameplayManager.metersTraveled));
        if (card == drawn) return;
        drawn = card;

        header.text = known == 0 ? "<b>Profile wiped!</b>" : "<b>Your data profile</b>  " + known + "/" + Labels.Length;
        for (int i = 0; i < lines.Count; i++)
        {
            bool shown = open && i < known;
            lines[i].gameObject.SetActive(shown);
            if (!shown) continue;
            lines[i].text = Labels[i] + ": " + ValueOf(i);
            lines[i].color = i == known - 1 ? NewLine : UiKit.Ink;
        }
        panel.sizeDelta = new Vector2(Width, HeaderHeight + (open ? known * LineHeight + 8f : 0f));
    }
}
