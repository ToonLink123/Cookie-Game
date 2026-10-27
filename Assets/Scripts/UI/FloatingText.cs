using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Short messages that float up and fade. Messages about the player stack above the balloon: the newest
// sits at the bottom and pushes older ones up a line, so several at once never overlap. Other messages
// float up from a point in the world (like "Rejected!" where a cookie was clicked).
public class FloatingText : MonoBehaviour
{
    public static readonly Color Bad = new Color(1f, 0.45f, 0.35f);
    public static readonly Color Good = new Color(0.45f, 1f, 0.5f);
    public static readonly Color Warning = new Color(1f, 0.85f, 0.3f);
    public static readonly Color Tracking = new Color(1f, 0.55f, 0.9f);

    private const float Lifetime = 1.8f;
    private const float RiseSpeed = 28f;     // canvas units per second
    private const float LineHeight = 36f;
    private const float PopTime = 0.15f;     // new lines grow in while older ones make room
    private const int FontSize = 28;
    private const float TopMargin = 150f;    // keep clear of the trackers along the top
    private static readonly Vector3 AbovePlayer = new Vector3(0f, 0.8f, 0f);
    private static readonly Vector3 BelowPlayer = new Vector3(0f, -0.8f, 0f);

    private class Message
    {
        public Text text;
        public Color color;
        public float age;
        public float offset;
        public Vector3 world;
    }

    private static FloatingText instance;

    private readonly List<Message> stack = new List<Message>();   // oldest first
    private readonly List<Message> loose = new List<Message>();
    private Transform player;

    public static void Create()
    {
        instance = UiKit.MakeLayer("Floating Text").gameObject.AddComponent<FloatingText>();
    }

    public static void AtPlayer(string message, Color color)
    {
        if (instance != null) instance.stack.Add(instance.Make(message, color, Vector3.zero));
    }

    public static void AtWorld(Vector3 position, string message, Color color)
    {
        if (instance != null) instance.loose.Add(instance.Make(message, color, position));
    }

    private Message Make(string message, Color color, Vector3 world)
    {
        Text text = UiKit.MakeText("Message", transform, Vector2.zero, Vector2.zero, new Vector2(700f, LineHeight),
            FontSize);
        text.text = message;
        text.color = color;
        text.resizeTextForBestFit = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        UiKit.AddOutline(text, Color.black, 2f);
        text.rectTransform.localScale = Vector3.zero;
        return new Message { text = text, color = color, world = world };
    }

    void LateUpdate()
    {
        float deltaTime = GameplayManager.currentState == GameState.Paused ? 0f : Time.deltaTime;
        if (player == null && stack.Count > 0)   // the player is switched off on the home screen; don't search every frame
        {
            PlayerController found = FindAnyObjectByType<PlayerController>();
            if (found != null) player = found.transform;
        }

        if (player != null && stack.Count > 0)
        {
            // Near the ceiling the stack hangs below the basket instead, so it stays on screen.
            Vector2 head = UiKit.WorldToCanvas(player.position + AbovePlayer);
            bool below = head.y + stack.Count * LineHeight > UiKit.CanvasSize.y - TopMargin;
            Vector2 anchor = below ? UiKit.WorldToCanvas(player.position + BelowPlayer) : head;
            float lowestFree = 0f;
            for (int i = stack.Count - 1; i >= 0; i--)   // newest first
            {
                Message message = stack[i];
                message.age += deltaTime;
                float wanted = Mathf.Max(message.age * RiseSpeed, lowestFree);
                message.offset = Mathf.MoveTowards(message.offset, wanted, LineHeight / PopTime * Time.deltaTime);
                message.offset = Mathf.Max(message.offset, message.age * RiseSpeed);
                lowestFree = message.offset + LineHeight;
                Place(message, anchor + Vector2.up * (below ? -message.offset : message.offset));
            }
        }

        foreach (Message message in loose)
        {
            message.age += deltaTime;
            Place(message, UiKit.WorldToCanvas(message.world) + Vector2.up * (message.age * RiseSpeed * 1.5f));
        }

        Expire(stack);
        Expire(loose);
    }

    private static void Place(Message message, Vector2 position)
    {
        message.text.rectTransform.anchoredPosition = position;
        message.text.rectTransform.localScale = Vector3.one * Mathf.Clamp01(message.age / PopTime);
        // Solid for most of its life, then fades out.
        float fade = Mathf.Clamp01((Lifetime - message.age) / (Lifetime * 0.35f));
        message.text.color = new Color(message.color.r, message.color.g, message.color.b, fade);
    }

    private static void Expire(List<Message> messages)
    {
        for (int i = messages.Count - 1; i >= 0; i--)
        {
            if (messages[i].age < Lifetime) continue;
            Destroy(messages[i].text.gameObject);
            messages.RemoveAt(i);
        }
    }
}
