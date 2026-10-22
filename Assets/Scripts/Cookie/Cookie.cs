using UnityEngine;

public class Cookie : MonoBehaviour
{
    [HideInInspector] public float speed = 5f;
    [HideInInspector] public float destroyX = -12f;

    private bool collected;

    void Update()
    {
        if (GameplayManager.currentState != GameState.Gameplay) return;

        transform.position += Vector3.left * speed * GameplayManager.overallSpeed * Time.deltaTime;
        if (transform.position.x < destroyX) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || GameplayManager.currentState != GameState.Gameplay) return;

        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        collected = true;
        player.EatCookie();
        CookieBurst.Spawn(GetComponent<SpriteRenderer>());
        Destroy(gameObject);
    }
}