using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// The "Are you sure?" bubble a persistent cookie puts up after three clicks. It follows the cookie,
// and goes away if the cookie does.
public class ConfirmPrompt : MonoBehaviour
{
    private static readonly Vector2 Size = new Vector2(320f, 130f);
    private const float Above = 110f;

    private Transform target;
    private RectTransform rect;

    public static void Show(Transform target, UnityAction yes)
    {
        Image box = UiKit.MakeBox("Are You Sure", UiKit.Canvas.transform, UiKit.Box, Vector2.zero, Vector2.zero,
            Size, 2f);
        Text question = UiKit.MakeText("Question", box.transform, new Vector2(0.5f, 1f), new Vector2(0f, -38f),
            new Vector2(Size.x - 30f, 56f), 22);
        question.text = "<b>Are you sure?</b>\nThis cookie remembers you.";

        ConfirmPrompt prompt = box.gameObject.AddComponent<ConfirmPrompt>();
        prompt.target = target;
        prompt.rect = box.rectTransform;
        UiKit.MakeButton("Yes, reject", box.transform, new Vector2(0.5f, 0f), new Vector2(0f, 32f),
            new Vector2(200f, 46f), 22, () =>
            {
                Destroy(prompt.gameObject);
                yes();
            });
        prompt.LateUpdate();
    }

    void LateUpdate()
    {
        if (target == null || GameplayManager.currentState == GameState.GameOver)
        {
            Destroy(gameObject);
            return;
        }

        // Float above the cookie, kept fully on screen.
        Vector2 canvas = UiKit.CanvasSize;
        Vector2 position = UiKit.WorldToCanvas(target.position) + Vector2.up * Above;
        position.x = Mathf.Clamp(position.x, Size.x / 2f, canvas.x - Size.x / 2f);
        position.y = Mathf.Clamp(position.y, Size.y / 2f, canvas.y - Size.y / 2f);
        rect.anchoredPosition = position;
    }
}
