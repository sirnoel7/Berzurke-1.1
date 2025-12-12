using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns single meteors from the right. Delay scales with difficulty.
/// Ensures spawned meteors are set up for 2D triggers and one-hit behavior.
/// Stops when the world stops (game over or finish).
/// </summary>
public class MeteorSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] meteorPrefabs;

    [Header("Spawn Area")]
    public float spawnX = 12f;
    public float minY = -1.5f;
    public float maxY = 4.5f;

    [Header("Timing")]
    public float baseMinDelay = 0.8f;
    public float baseMaxDelay = 1.6f;

    private Coroutine loop;

    private void OnEnable() { loop = StartCoroutine(SpawnLoop()); }
    private void OnDisable() { if (loop != null) StopCoroutine(loop); loop = null; }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            // Stop spawning if the world is halted (game over or finish)
            var gm = GameManager.Instance;
            if (gm == null) yield break;
            if (gm.inFinalCinematic) yield break;
            if (gm.baseSpeed == 0f && gm.speedMultiplier == 0f) yield break;

            // Difficulty shortens delays as speedMultiplier rises (never below 0.5x base).
            float diff = Mathf.Max(0.5f, gm.speedMultiplier);
            float delay = Random.Range(baseMinDelay, baseMaxDelay) / diff;
            yield return new WaitForSeconds(delay);

            // Re-check stop condition after waiting
            if (gm.inFinalCinematic) yield break;
            if (gm.baseSpeed == 0f && gm.speedMultiplier == 0f) yield break;

            SpawnOne(Random.Range(minY, maxY));
        }
    }

    private void SpawnOne(float y)
    {
        if (meteorPrefabs == null || meteorPrefabs.Length == 0) return;

        GameObject prefab = meteorPrefabs[Random.Range(0, meteorPrefabs.Length)];
        if (prefab == null) return;

        var go = Instantiate(prefab, new Vector3(spawnX, y, 0f), Quaternion.identity);

        // Tag as obstacle (used by RocketPlayer)
        if (!go.CompareTag("Obstacle")) go.tag = "Obstacle";

        // Ensure it moves left with world speed (re-uses your Part 1 script)
        if (go.GetComponent<MoveLeft>() == null) go.AddComponent<MoveLeft>();

        // Ensure 2D trigger + one-hit behavior
        Ensure2DTrigger(go);
        EnsureHitOnce(go);
    }

    private static void Ensure2DTrigger(GameObject go)
    {
        // Collider2D (auto BoxCollider2D if missing)
        var col2D = go.GetComponent<Collider2D>();
        if (col2D == null) col2D = go.AddComponent<BoxCollider2D>();
        col2D.isTrigger = true;

        // Kinematic RB2D, no gravity
        var rb2D = go.GetComponent<Rigidbody2D>();
        if (rb2D == null) rb2D = go.AddComponent<Rigidbody2D>();
        rb2D.gravityScale = 0f;
        rb2D.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb2D.interpolation = RigidbodyInterpolation2D.None;
        rb2D.bodyType = RigidbodyType2D.Kinematic;
    }

    private static void EnsureHitOnce(GameObject go)
    {
        if (go.GetComponent<MeteorHitOnce>() == null)
            go.AddComponent<MeteorHitOnce>(); // default settings
    }
}
