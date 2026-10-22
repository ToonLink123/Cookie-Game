using UnityEngine;

public class CookieBurst : MonoBehaviour
{
    private const int PieceCount = 10;
    private const float Lifetime = 0.5f;

    private SpriteRenderer[] pieces = new SpriteRenderer[PieceCount];
    private Vector3[] velocities = new Vector3[PieceCount];
    private float[] spins = new float[PieceCount];
    private Color color;
    private float elapsed;

    public static void Spawn(SpriteRenderer source)
    {
        if (source == null || source.sprite == null) return;

        GameObject burstObject = new GameObject("Cookie Burst");
        CookieBurst burst = burstObject.AddComponent<CookieBurst>();
        burst.transform.position = source.transform.position;
        burst.color = source.color;
        float rotation = Random.Range(0f, Mathf.PI * 2f);

        for (int i = 0; i < PieceCount; i++)
        {
            GameObject piece = new GameObject("Cookie Piece");
            piece.transform.SetParent(burst.transform, false);
            piece.transform.localScale = source.transform.lossyScale * Random.Range(0.2f, 0.35f);
            piece.transform.Rotate(0f, 0f, Random.Range(0f, 360f));

            SpriteRenderer spriteRenderer = piece.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = source.sprite;
            spriteRenderer.sharedMaterial = source.sharedMaterial;
            spriteRenderer.color = source.color;
            spriteRenderer.sortingLayerID = source.sortingLayerID;
            spriteRenderer.sortingOrder = source.sortingOrder + 1;
            burst.pieces[i] = spriteRenderer;

            float angle = rotation + i * Mathf.PI * 2f / PieceCount + Random.Range(-0.2f, 0.2f);
            burst.velocities[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * Random.Range(1.5f, 3.5f);
            burst.spins[i] = Random.Range(-540f, 540f);
        }
    }

    void Update()
    {
        if (GameplayManager.currentState == GameState.Paused) return;

        elapsed += Time.deltaTime;
        if (elapsed >= Lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Color fadedColor = color;
        fadedColor.a *= 1f - elapsed / Lifetime;
        for (int i = 0; i < PieceCount; i++)
        {
            pieces[i].transform.position += velocities[i] * Time.deltaTime;
            pieces[i].transform.Rotate(0f, 0f, spins[i] * Time.deltaTime);
            pieces[i].color = fadedColor;
        }
    }
}
