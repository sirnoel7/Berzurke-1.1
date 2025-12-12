using UnityEngine;

public class Spawner : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnableObject
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float spawnChance;
    }

    [Header("Spawnable Objects")]
    public SpawnableObject[] objects;

    [Header("Spawn Timing")]
    public float minSpawnTime = 1f;
    public float maxSpawnTime = 2f;

    [Header("Spawn Position")]
    public float spawnX = 12f;

    private void OnEnable()
    {
        ScheduleNextSpawn();
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void ScheduleNextSpawn()
    {
        if (objects == null || objects.Length == 0)
            return;

        float time = UnityEngine.Random.Range(minSpawnTime, maxSpawnTime);
        Invoke(nameof(SpawnObject), time);
    }

    private void SpawnObject()
    {
        if (objects == null || objects.Length == 0)
            return;

        float roll = UnityEngine.Random.value;
        float cumulative = 0f;
        GameObject chosen = null;

        foreach (var obj in objects)
        {
            cumulative += obj.spawnChance;
            if (roll <= cumulative)
            {
                chosen = obj.prefab;
                break;
            }
        }

        if (chosen == null)
            chosen = objects[objects.Length - 1].prefab;

        if (chosen == null)
            return;

        float prefabY = chosen.transform.position.y;

        Instantiate(
            chosen,
            new Vector3(spawnX, prefabY, 0f),
            Quaternion.identity
        );

        ScheduleNextSpawn();
    }
}
