using UnityEngine;

public class EntitySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject entityPrefab;
    public Transform[] spawnPoints;

    [Header("Target & UI References ✨")]
    public Transform player;
    [Tooltip("ลาก Canvas / Panel หน้า GameOver ใน Scene มาใส่ตรงนี้")]
    public GameObject gameOverUI;

    [Header("Debug Test Settings")]
    public KeyCode testSpawnKey = KeyCode.G;

    void Start()
    {
        // ค้นหา Player อัตโนมัติหากไม่ได้ลากใส่ Inspector
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        // ค้นหา GameOver UI อัตโนมัติหากไม่ได้ลากใส่ Inspector
        if (gameOverUI == null)
        {
            gameOverUI = GameObject.FindWithTag("GameOverUI");
        }

        // หากไม่ได้ลากจุดใส่ Inspector จะหา Tag "EntitySpawn" ให้อัตโนมัติ[cite: 3]
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

            // ส่งอ้างอิงทั้ง Player และ GameOver UI ไปให้ผีที่เพิ่งเสกขึ้นมา ✨[cite: 3]
            EntityAI aiScript = entity.GetComponent<EntityAI>(); 
            if (aiScript != null)
            {
                if (player != null) aiScript.playerTransform = player; 
                if (gameOverUI != null) aiScript.gameOverUI = gameOverUI;
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