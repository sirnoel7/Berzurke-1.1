using System.Collections;
using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    public GameObject[] cloudPrefabs;

    [Header("Spawn Settings")]
    public float spawnX = 25f;
    public float minDelay = 6f;
    public float maxDelay = 12f;

    [Header("Cloud Heights")]
    public float[] yPositions = { 4.2f, 4.6f, 5.0f, 5.4f };

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnOneCloud();
            float delay = UnityEngine.Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);
        }
    }

    void SpawnOneCloud()
    {
        if (cloudPrefabs.Length == 0 || yPositions.Length == 0)
            return;

        GameObject prefab = cloudPrefabs[UnityEngine.Random.Range(0, cloudPrefabs.Length)];

        // pick a random height lane
        float y = yPositions[UnityEngine.Random.Range(0, yPositions.Length)];

        Vector3 spawnPos = new Vector3(spawnX, y, 0f);

        // instantiate
        GameObject cloud = Instantiate(prefab, spawnPos, Quaternion.identity);

        // preserve prefab scale
        cloud.transform.localScale = prefab.transform.localScale;
    }
}
