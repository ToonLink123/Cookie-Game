using UnityEngine;

// Anything with a collider that flies across the screen (cookies, hazards, pickups) gets a kinematic
// Rigidbody2D. Without one its collider counts as static scenery, and Unity rebuilds a static collider from
// scratch every time it moves: every frame, for every one on screen, so the game got choppier the busier
// the sky became. A kinematic body moves its collider for almost nothing and still sets off the triggers.
public static class MovingCollider
{
    public static void AddBody(GameObject mover)
    {
        Rigidbody2D body = mover.GetComponent<Rigidbody2D>();
        if (body == null) body = mover.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;   // a sleeping body can miss a trigger
    }
}
