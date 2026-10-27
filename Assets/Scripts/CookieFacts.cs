using System.Collections.Generic;
using UnityEngine;

public static class CookieFacts
{
    private const string CreepFactor =
        "<b>Usage & Statistics\nThe \"Creep\" Factor</b>\nAbout 90% of people find it creepy that banner ads on websites look personalized, and 64% refuse to allow their personal data to be used for advertising. [1, 2]";
    private const string ConsentAndBehavior =
        "<b>Usage & Statistics\nConsent & Behavior</b>\nRoughly 24% of people just click \"Accept\" on cookie pop-ups immediately without reading them, and 60% of users will share data if offered rewards or discounts. [1, 2]";

    private static readonly string[] Facts =
    {
        "<b>Usage & Statistics\nWeb Adoption</b>\nAbout 42.4% of global websites use cookies. [1]",
        "<b>Usage & Statistics\nMarketing Reliance</b>\nRoughly 75% of marketing leaders rely on third-party cookies, and 92.8% of advertisers worry about cookie depreciation. [1]",
        "<b>Usage & Statistics\nUser Understanding</b>\nOnly 28% of US adults truly understand what cookies are, while 49% understand them partially, and 24% are completely confused. [1]",
        CreepFactor,
        ConsentAndBehavior,
        "<b>Usage & Statistics\nGeographic Differences</b>\nPoland leads cookie consent rates at 64%, while the US has the lowest consent acceptance/tracking rate at 32%. [1]",
        "<b>Fun Technical Facts\nThe 4KB Limit</b>\nBrowsers generally cap individual cookies at around 4KB of data. If you try to store large JSON objects inside a cookie, it will get chopped off or ignored.",
        "<b>Fun Technical Facts\nThey Travel Everywhere</b>\nBrowsers automatically attach stored cookies to every HTTP request sent to that domain (including images, scripts, and API calls). Big cookies mean bloated, slower network requests.",
        "<b>Fun Technical Facts\nNot Viruses</b>\nCookies are just plain text data. They cannot execute code, run like programs, or infect your device with viruses.",
        "<b>Fun Technical Facts\nOrigin Story</b>\nLou Montulli invented and patented the web cookie technology in 1995 while working at Netscape, originally designed to make virtual shopping carts work.",
        "<b>Fun Technical Facts\nThe Name</b>\nThey were named \"cookies\" after the programming term \"magic cookie,\" which refers to a packet of data a program receives and sends back unchanged. [1, 2, 3, 4, 5]"
    };

    // What each kind of cookie in the game really is, indexed by CookieType.
    private static readonly string[] TypeFacts =
    {
        "<b>Cookie Types\nFirst-Party Cookies</b>\nCreated by the site you are visiting, usually to save logins and preferences. Browsers allow them by default.",
        "<b>Cookie Types\nThird-Party Cookies</b>\nCreated by external domains, like advertisers, to track you across different websites.",
        "<b>Cookie Types\nSession Cookies</b>\nTemporary files stored in memory that delete automatically when you close the browser tab or window.",
        "<b>Cookie Types\nPersistent Cookies</b>\nStored on your device until a set expiration date, even after you close the browser.",
        "<b>Consent Pop-ups\nLegitimate Interest</b>\nMany cookie banners list \"legitimate interest\" separately from consent, so some data use stays switched on unless you find those switches too.",
    };

    public static string RandomFact()
    {
        return Facts[Random.Range(0, Facts.Length)];
    }

    // A fact that fits what just happened: accepting a pop-up, or the kind of cookie eaten most.
    public static string ForRun()
    {
        List<string> fitting = new List<string>();
        if (GameplayManager.bannersAccepted > 0) fitting.Add(ConsentAndBehavior);

        int[] eaten = GameplayManager.cookiesEaten;
        int favorite = 0;
        for (int i = 1; i < eaten.Length; i++)
            if (eaten[i] > eaten[favorite]) favorite = i;
        if (eaten[favorite] > 0)
        {
            fitting.Add(TypeFacts[favorite]);
            if (favorite == (int)CookieType.ThirdParty) fitting.Add(CreepFactor);
        }
        return fitting.Count > 0 ? fitting[Random.Range(0, fitting.Count)] : RandomFact();
    }
}
