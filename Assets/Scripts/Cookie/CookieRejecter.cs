using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// In game the mouse is for rejecting cookies (Space flies): clicking one rejects it, though each type
// resists in its own way (see Cookie.Click). Also tracks the pointer, which tracker cookies run from.
public class CookieRejecter : MonoBehaviour
{
    // Where the mouse is in the world, or null without one.
    public static Vector2? Pointer { get; private set; }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Pointer = null;
        if (mouse == null || Camera.main == null) return;
        Pointer = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());

        if (GameplayManager.currentState != GameState.Gameplay || !mouse.leftButton.wasPressedThisFrame) return;
        // Clicks on pop-ups and other UI are for them, not for the cookies behind.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        foreach (Collider2D hit in Physics2D.OverlapPointAll(Pointer.Value))
        {
            Cookie cookie = hit.GetComponent<Cookie>();
            if (cookie == null || !cookie.enabled) continue;
            cookie.Click();
            return;
        }
    }
}
