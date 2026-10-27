using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Screen-wide effects: fading to and from black between screens, a white flash, and camera shake.
// Drawn above all other UI.
public class ScreenEffects : MonoBehaviour
{
    private static ScreenEffects instance;

    private Image fade;
    private Image flash;
    private Coroutine fading;

    // Starts fully black, then fades in, so every scene load (start, restart, home) eases in.
    public static void Create(float fadeInSeconds)
    {
        instance = UiKit.MakeLayer("Screen Effects").gameObject.AddComponent<ScreenEffects>();
        instance.flash = instance.FullScreen("Flash", new Color(1f, 1f, 1f, 0f));
        instance.fade = instance.FullScreen("Fade", Color.black);
        FadeTo(0f, fadeInSeconds, null);
    }

    private Image FullScreen(string name, Color color)
    {
        Image image = UiKit.MakeImage(name, transform, null, Vector2.zero, Vector2.zero, Vector2.zero);
        UiKit.Stretch(image.rectTransform);
        image.color = color;
        return image;
    }

    // Fades the screen to (or from) black, then runs afterwards. Clicks are blocked while it's dark.
    public static void FadeTo(float darkness, float seconds, Action afterwards)
    {
        if (instance == null)
        {
            afterwards?.Invoke();
            return;
        }
        if (instance.fading != null) instance.StopCoroutine(instance.fading);
        instance.fading = instance.StartCoroutine(instance.Fade(darkness, seconds, afterwards));
    }

    public static void Flash(float strength, float seconds)
    {
        if (instance != null) instance.StartCoroutine(instance.FadeFlash(strength, seconds));
    }

    public static void Shake(float seconds, float strength)
    {
        if (instance != null) instance.StartCoroutine(instance.ShakeCamera(seconds, strength));
    }

    void LateUpdate()
    {
        if (transform.GetSiblingIndex() != transform.parent.childCount - 1) transform.SetAsLastSibling();
    }

    private IEnumerator Fade(float darkness, float seconds, Action afterwards)
    {
        fade.raycastTarget = true;
        float from = fade.color.a;
        for (float time = 0f; time < seconds; time += Time.unscaledDeltaTime)
        {
            fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, darkness, time / seconds));
            yield return null;
        }
        fade.color = new Color(0f, 0f, 0f, darkness);
        fade.raycastTarget = darkness > 0f;
        fading = null;
        afterwards?.Invoke();
    }

    private IEnumerator FadeFlash(float strength, float seconds)
    {
        for (float time = 0f; time < seconds; time += Time.unscaledDeltaTime)
        {
            flash.color = new Color(1f, 1f, 1f, strength * (1f - time / seconds));
            yield return null;
        }
        flash.color = new Color(1f, 1f, 1f, 0f);
    }

    // Jolts the camera around its resting spot, easing off, then puts it back exactly.
    private IEnumerator ShakeCamera(float seconds, float strength)
    {
        Transform cameraTransform = Camera.main.transform;
        Vector3 rest = cameraTransform.position;
        for (float time = 0f; time < seconds; time += Time.unscaledDeltaTime)
        {
            Vector2 jolt = UnityEngine.Random.insideUnitCircle * strength * (1f - time / seconds);
            cameraTransform.position = rest + (Vector3)jolt;
            yield return null;
        }
        cameraTransform.position = rest;
    }
}
