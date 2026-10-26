using System.Collections.Generic;
using UnityEngine;

// Shatters a cookie (or any sprite) into jagged shards of its own picture that fly apart, tumble, and fade.
public class CookieBurst : MonoBehaviour
{
    private const int Grid = 3;            // shards come from a Grid x Grid lattice, each cell cut in two
    private const float Jitter = 0.35f;    // how far lattice points wander, as a share of a cell
    private const int Patterns = 4;        // different shatter patterns made per sprite
    private const float Lifetime = 0.55f;
    private const float Gravity = 6f;

    // Each sprite's shatter patterns are cut once, then reused for every burst of that sprite.
    private static readonly Dictionary<Sprite, Sprite[][]> patternsBySprite = new Dictionary<Sprite, Sprite[][]>();

    private SpriteRenderer[] pieces;
    private Vector3[] velocities;
    private float[] spins;
    private Color color;
    private float elapsed;

    public static void Spawn(SpriteRenderer source)
    {
        if (source == null || source.sprite == null) return;

        Sprite[] shards = ShardsOf(source.sprite);
        GameObject burstObject = new GameObject("Cookie Burst");
        CookieBurst burst = burstObject.AddComponent<CookieBurst>();
        burst.color = source.color;
        burst.pieces = new SpriteRenderer[shards.Length];
        burst.velocities = new Vector3[shards.Length];
        burst.spins = new float[shards.Length];

        Transform whole = source.transform;
        for (int i = 0; i < shards.Length; i++)
        {
            // Start each shard exactly where it sits in the whole cookie (its pivot is its middle),
            // turned and sized the same, so the first frame still looks like one cookie.
            Vector2 offset = (shards[i].pivot - source.sprite.pivot) / source.sprite.pixelsPerUnit;
            GameObject piece = new GameObject("Cookie Shard");
            piece.transform.SetParent(burst.transform, false);
            piece.transform.SetPositionAndRotation(whole.TransformPoint(offset), whole.rotation);
            piece.transform.localScale = whole.lossyScale;

            SpriteRenderer spriteRenderer = piece.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = shards[i];
            spriteRenderer.sharedMaterial = source.sharedMaterial;
            spriteRenderer.color = source.color;
            spriteRenderer.sortingLayerID = source.sortingLayerID;
            spriteRenderer.sortingOrder = source.sortingOrder + 1;
            burst.pieces[i] = spriteRenderer;

            // Fly out from the middle with a little upward pop; gravity pulls them down after.
            Vector3 outward = (piece.transform.position - whole.position).normalized;
            Vector3 jitter = Random.insideUnitCircle * 0.4f;
            burst.velocities[i] = (outward + jitter).normalized * Random.Range(1.5f, 3.5f) + Vector3.up * 1.5f;
            burst.spins[i] = Random.Range(-360f, 360f);
        }
    }

    private static Sprite[] ShardsOf(Sprite sprite)
    {
        if (!patternsBySprite.TryGetValue(sprite, out Sprite[][] patterns) || patterns[0][0] == null)
        {
            patterns = new Sprite[Patterns][];
            for (int i = 0; i < Patterns; i++) patterns[i] = Shatter(sprite);
            patternsBySprite[sprite] = patterns;
        }
        return patterns[Random.Range(0, Patterns)];
    }

    // Cuts the sprite into jagged triangles: a lattice whose points are nudged at random (edge points
    // only along their edge), with every cell split corner to corner along a random diagonal.
    private static Sprite[] Shatter(Sprite sprite)
    {
        Rect area = sprite.textureRect;
        Vector2 cell = area.size / Grid;
        Vector2[,] points = new Vector2[Grid + 1, Grid + 1];
        for (int y = 0; y <= Grid; y++)
        {
            for (int x = 0; x <= Grid; x++)
            {
                Vector2 point = new Vector2(x * cell.x, y * cell.y);
                if (x > 0 && x < Grid) point.x += Random.Range(-Jitter, Jitter) * cell.x;
                if (y > 0 && y < Grid) point.y += Random.Range(-Jitter, Jitter) * cell.y;
                points[x, y] = point;
            }
        }

        List<Sprite> shards = new List<Sprite>();
        for (int y = 0; y < Grid; y++)
        {
            for (int x = 0; x < Grid; x++)
            {
                Vector2 a = points[x, y], b = points[x + 1, y], c = points[x + 1, y + 1], d = points[x, y + 1];
                if (Random.value < 0.5f)
                {
                    shards.Add(Shard(sprite, area, a, b, c));
                    shards.Add(Shard(sprite, area, a, c, d));
                }
                else
                {
                    shards.Add(Shard(sprite, area, a, b, d));
                    shards.Add(Shard(sprite, area, b, c, d));
                }
            }
        }
        return shards.ToArray();
    }

    // One triangle of the sprite's picture (corners in pixels within its rect), pivoting on its middle.
    private static Sprite Shard(Sprite sprite, Rect area, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 middle = (a + b + c) / 3f;
        Sprite shard = Sprite.Create(sprite.texture, area, new Vector2(middle.x / area.width, middle.y / area.height),
            sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        shard.OverrideGeometry(new[] { a, b, c }, new ushort[] { 0, 1, 2 });
        return shard;
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

        // Stay solid while flying apart, fading out mostly toward the end.
        Color fadedColor = color;
        fadedColor.a *= 1f - Mathf.Pow(elapsed / Lifetime, 2f);
        for (int i = 0; i < pieces.Length; i++)
        {
            velocities[i] += Vector3.down * Gravity * Time.deltaTime;
            pieces[i].transform.position += velocities[i] * Time.deltaTime;
            pieces[i].transform.Rotate(0f, 0f, spins[i] * Time.deltaTime);
            pieces[i].color = fadedColor;
        }
    }
}
