using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Browser clutter: notification toasts that slide in at the right edge, more often the more you're
// tracked (some quote the data profile). They don't block anything; they're just in the way.
public class BrowserToasts : MonoBehaviour
{
    private const int MaxToasts = 4;
    private const float SlowestGap = 9f;     // seconds between toasts with one tracker eaten
    private const float FastestGap = 2.5f;   // ...and with ten or more
    private const float Stay = 3.5f;
    private const float SlideTime = 0.25f;
    private const float Bottom = 320f;       // clear of the consent banners at the bottom
    private static readonly Vector2 Size = new Vector2(400f, 72f);

    private static readonly string[] Messages =
    {
        "<b>Cookie Corp</b>\n3 new deals picked for you!",
        "<b>Someone near you</b>\njust bought a dozen cookies",
        "<b>Your cart misses you</b>\nCome back for 10% off!",
        "<b>Allow notifications?</b>\nWe promise it's just a few",
        "<b>Trending in the sky</b>\nSnacks, snacks and snacks",
        "<b>You have (1) new tracker</b>\nIt likes you already",
        "<b>Flash sale!</b>\n50% off cookies, today only",
    };

    private class Toast
    {
        public RectTransform rect;
        public float age;
        public float y;
    }

    private readonly List<Toast> toasts = new List<Toast>();
    private float untilNext;

    public static void Create()
    {
        UiKit.MakeLayer("Browser Toasts").gameObject.AddComponent<BrowserToasts>();
    }

    void Update()
    {
        GameState state = GameplayManager.currentState;
        if (state == GameState.GameOver || state == GameState.MainMenu)
        {
            foreach (Toast toast in toasts) Destroy(toast.rect.gameObject);
            toasts.Clear();
            return;
        }
        if (state == GameState.Paused) return;

        int hits = GameplayManager.trackerCookiesHit;
        untilNext -= Time.deltaTime;
        if (hits > 0 && untilNext <= 0f && toasts.Count < MaxToasts)
        {
            Add();
            untilNext = Mathf.Lerp(SlowestGap, FastestGap, Mathf.Clamp01((hits - 1) / 9f));
        }

        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            Toast toast = toasts[i];
            toast.age += Time.deltaTime;
            if (toast.age >= Stay + SlideTime * 2f)
            {
                Destroy(toast.rect.gameObject);
                toasts.RemoveAt(i);
                continue;
            }
            // Slides in from off the right edge, stays, slides back out; toasts stack upwards.
            float slideIn = Mathf.SmoothStep(0f, 1f, toast.age / SlideTime);
            float slideOut = Mathf.SmoothStep(0f, 1f, (toast.age - Stay - SlideTime) / SlideTime);
            float wantedY = Bottom + i * (Size.y + 10f);
            toast.y = Mathf.Lerp(toast.y, wantedY, 10f * Time.deltaTime);
            toast.rect.anchoredPosition = new Vector2(Mathf.Lerp(Size.x + 40f, -20f, slideIn - slideOut), toast.y);
        }
    }

    private void Add()
    {
        Image box = UiKit.MakeBox("Toast", transform, UiKit.Box, new Vector2(1f, 0f), Vector2.zero, Size, 2f);
        box.raycastTarget = false;
        box.rectTransform.pivot = new Vector2(1f, 0f);
        // A little tracker cookie as the "app icon".
        Image icon = UiKit.MakeImage("Icon", box.transform, UiKit.Cookie, new Vector2(0f, 0.5f), new Vector2(40f, 0f),
            new Vector2(46f, 46f));
        icon.color = Cookie.Tint(CookieType.ThirdParty);
        Text text = UiKit.MakeText("Text", box.transform, new Vector2(0f, 0.5f), new Vector2(76f, 0f),
            new Vector2(Size.x - 92f, Size.y - 10f), 20);
        text.rectTransform.pivot = new Vector2(0f, 0.5f);
        text.alignment = TextAnchor.MiddleLeft;
        text.text = Message();
        toasts.Add(new Toast { rect = box.rectTransform, y = Bottom + toasts.Count * (Size.y + 10f) });
    }

    private static string Message()
    {
        // Once the trackers know enough, some notifications get personal.
        if (DataProfile.Known >= 2 && Random.value < 0.4f)
            return "<b>Because you like cookies</b>\nHere are " + Random.Range(3, 9) + " more cookies";
        if (DataProfile.Known >= 6 && Random.value < 0.4f)
            return "<b>" + Mathf.FloorToInt(GameplayManager.metersTraveled) + " m flown!</b>\nTreat yourself to a cookie";
        return Messages[Random.Range(0, Messages.Length)];
    }
}
