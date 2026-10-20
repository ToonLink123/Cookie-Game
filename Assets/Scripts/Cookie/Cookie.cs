using UnityEngine;

public class Cookie : MonoBehaviour
{
    [HideInInspector] public float speed = 5f;
    [HideInInspector] public float destroyX = -12f;

    void Update()
    {
        transform.position += Vector3.left * speed * Time.deltaTime;
        if (transform.position.x < destroyX) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        player.EatCookie();
        Destroy(gameObject);
    }
}