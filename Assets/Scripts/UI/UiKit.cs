using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Helpers for building UI in code (every screen in this game is made this way), plus the shared art
// they are made from: the game's grey boxes and blue button, stretched with 9-slicing so any size works.
public static class UiKit
{
    public static readonly Color Ink = new Color(0.06f, 0.10f, 0.16f);
    public static readonly Color Paper = Color.white;

    // Set up by GameUI from the sprites wired in the scene.
    public static Canvas Canvas { get; private set; }
    public static Sprite Box { get; private set; }         // Facts.png: the wide grey box
    public static Sprite TallBox { get; private set; }     // Credits.png: the tall grey box
    public static Sprite Arrow { get; private set; }       // CreditsArrow.png
    public static Sprite Cookie { get; private set; }
    public static Sprite BlueButton { get; private set; }  // StartButton.png with its letters erased

    private static Font font;

    // Hand-drawn font matching the lettering in the UI sprites; built by Tools/CookieFont.
    public static Font Font
    {
        get
        {
            if (font == null) font = Resources.Load<Font>("CookieFont");
            return font;
        }
    }

    public static void Setup(Canvas canvas, Sprite box, Sprite tallBox, Sprite arrow, Sprite cookie)
    {
        Canvas = canvas;
        Box = box;
        TallBox = tallBox;
        Arrow = arrow;
        Cookie = cookie;
        BlueButton = Resources.Load<Sprite>("BlankButton");
    }

    public static RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 size)
    {
        GameObject element = new GameObject(name, typeof(RectTransform));
        element.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = element.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = size;
        return rect;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    // A full-screen layer on the canvas, for UI that has to sit above the rest.
    public static RectTransform MakeLayer(string name)
    {
        RectTransform layer = MakeRect(name, Canvas.transform, Vector2.zero, Vector2.zero);
        Stretch(layer);
        return layer;
    }

    public static Image MakeImage(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position,
        Vector2 size)
    {
        RectTransform rect = MakeRect(name, parent, anchor, size);
        rect.anchoredPosition = position;
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    // One of the grey boxes, 9-sliced so its corners stay crisp at any size. pixelScale is how many
    // screen units each of the box's pixels takes up.
    public static Image MakeBox(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 position,
        Vector2 size, float pixelScale = 3f)
    {
        Image image = MakeImage(name, parent, sprite, anchor, position, size);
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f / pixelScale;
        image.raycastTarget = true;   // panels catch clicks so nothing behind them reacts
        return image;
    }

    public static Text MakeText(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size,
        int fontSize)
    {
        RectTransform rect = MakeRect(name, parent, anchor, size);
        rect.anchoredPosition = position;
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = Font;
        text.fontSize = fontSize;
        text.color = Ink;
        text.alignment = TextAnchor.MiddleCenter;
        text.supportRichText = true;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Min(18, fontSize);
        text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    // A labelled button on the blue button art (or the grey box, for the "less important" choice).
    public static Button MakeButton(string label, Transform parent, Vector2 anchor, Vector2 position, Vector2 size,
        int fontSize, UnityAction onClick, bool grey = false)
    {
        Image image = MakeBox(label + " Button", parent, grey ? Box : BlueButton, anchor, position, size,
            grey ? 2f : 4f);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null) button.onClick.AddListener(onClick);
        Text text = MakeText("Label", image.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
            size - new Vector2(16f, 10f), fontSize);
        text.text = label;
        return button;
    }

    public static Outline AddOutline(Graphic graphic, Color color, float width)
    {
        Outline outline = graphic.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(width, width);
        return outline;
    }

    // Where a point in the world sits on the (screen-overlay) canvas, measured from its bottom-left.
    public static Vector2 WorldToCanvas(Vector3 world)
    {
        return (Vector2)Camera.main.WorldToScreenPoint(world) / Canvas.scaleFactor;
    }

    public static Vector2 CanvasSize => ((RectTransform)Canvas.transform).rect.size;
}
