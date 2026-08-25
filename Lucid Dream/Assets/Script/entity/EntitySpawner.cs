using UnityEngine;

public class EntitySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject entityPrefab;
    public Transform[] spawnPoints;
    public Transform player;

    [Header("Debug Test Settings")]
    public KeyCode testSpawnKey = KeyCode.G;

    void Start()
    {
        // หากไม่ได้ลากจุดใส่ Inspector จะหา Tag "EntitySpawn" ให้อัตโนมัติ
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            GameObject[] points = GameObject.FindGameObjectsWithTag("EntitySpawn");
            spawnPoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                spawnPoints[i] = points[i].transform;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(testSpawnKey))
        {
            ForceSpawnEntity();
        }
    }

    public void ForceSpawnEntity()
    {
        Debug.Log("[Debug] Force Spawning Entity!");
        SpawnEntity();
    }

    public void SpawnEntity()
    {
        Transform bestSpawnPoint = GetSpawnPointOutsideCamera();

        if (bestSpawnPoint != null && entityPrefab != null)
        {
            GameObject entity = Instantiate(entityPrefab, bestSpawnPoint.position, Quaternion.identity);

            EntityAI aiScript = entity.GetComponent<EntityAI>();
            if (aiScript != null)
            {
                aiScript.playerTransform = player;
            }
        }
    }

    Transform GetSpawnPointOutsideCamera()
    {
        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;

            Vector3 screenPoint = Camera.main.WorldToViewportPoint(point.position);
            bool isOutsidePoint = screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1;

            if (isOutsidePoint)
            {
                return point;
            }
        }
        return spawnPoints.Length > 0 ? spawnPoints[0] : null;
    }
}