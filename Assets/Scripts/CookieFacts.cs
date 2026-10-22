using UnityEngine;

public static class CookieFacts
{
    private static readonly string[] Facts =
    {
        "<b>Usage & Statistics\nWeb Adoption</b>\nAbout 42.4% of global websites use cookies. [1]",
        "<b>Usage & Statistics\nMarketing Reliance</b>\nRoughly 75% of marketing leaders rely on third-party cookies, and 92.8% of advertisers worry about cookie depreciation. [1]",
        "<b>Usage & Statistics\nUser Understanding</b>\nOnly 28% of US adults truly understand what cookies are, while 49% understand them partially, and 24% are completely confused. [1]",
        "<b>Usage & Statistics\nThe \"Creep\" Factor</b>\nAbout 90% of people find it creepy that banner ads on websites look personalized, and 64% refuse to allow their personal data to be used for advertising. [1, 2]",
        "<b>Usage & Statistics\nConsent & Behavior</b>\nRoughly 24% of people just click \"Accept\" on cookie pop-ups immediately without reading them, and 60% of users will share data if offered rewards or discounts. [1, 2]",
        "<b>Usage & Statistics\nGeographic Differences</b>\nPoland leads cookie consent rates at 64%, while the US has the lowest consent acceptance/tracking rate at 32%. [1]",
        "<b>Fun Technical Facts\nThe 4KB Limit</b>\nBrowsers generally cap individual cookies at around 4KB of data. If you try to store large JSON objects inside a cookie, it will get chopped off or ignored.",
        "<b>Fun Technical Facts\nThey Travel Everywhere</b>\nBrowsers automatically attach stored cookies to every HTTP request sent to that domain (including images, scripts, and API calls). Big cookies mean bloated, slower network requests.",
        "<b>Fun Technical Facts\nNot Viruses</b>\nCookies are just plain text data. They cannot execute code, run like programs, or infect your device with viruses.",
        "<b>Fun Technical Facts\nOrigin Story</b>\nLou Montulli invented and patented the web cookie technology in 1995 while working at Netscape, originally designed to make virtual shopping carts work.",
        "<b>Fun Technical Facts\nThe Name</b>\nThey were named \"cookies\" after the programming term \"magic cookie,\" which refers to a packet of data a program receives and sends back unchanged. [1, 2, 3, 4, 5]"
    };

    public static string RandomFact()
    {
        return Facts[Random.Range(0, Facts.Length)];
    }
}
