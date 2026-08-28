using System.Collections.Generic;
using UnityEngine;

public class EntitySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject entityPrefab;
    public Transform[] spawnPoints;

    [Header("Distance & Room Filter Settings ✨")]
    [Tooltip("ระยะห่างขั้นต่ำจากผู้เล่นที่ผีสามารถเกิดได้ (หน่วยเป็นเมตร)")]
    public float minSpawnDistance = 10f;
    [Tooltip("เปิดใช้การเช็กระยะทางร่วมกับการเช็กมุมมองกล้อง")]
    public bool useDistanceFilter = true;

    [Header("Target & UI References ✨")]
    public Transform player;
    [Tooltip("ลาก Canvas / Panel หน้า GameOver ใน Scene มาใส่ตรงนี้")]
    public GameObject gameOverUI;

    [Header("Debug Test Settings")]
    public KeyCode testSpawnKey = KeyCode.G;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (gameOverUI == null)
        {
            gameOverUI = GameObject.FindWithTag("GameOverUI");
        }

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
        Transform bestSpawnPoint = GetValidSpawnPoint();

        if (bestSpawnPoint != null && entityPrefab != null)
        {
            GameObject entity = Instantiate(entityPrefab, bestSpawnPoint.position, Quaternion.identity);

            EntityAI aiScript = entity.GetComponent<EntityAI>();
            if (aiScript != null)
            {
                if (player != null) aiScript.playerTransform = player;
                if (gameOverUI != null) aiScript.gameOverUI = gameOverUI;
            }
        }
        else
        {
            Debug.LogWarning("[EntitySpawner] ไม่พบจุดเกิดที่ผ่านเงื่อนไขความปลอดภัย!");
        }
    }

    /// <summary>
    /// ค้นหาและสุ่มจุดเกิดที่อยู่นอกสายตา และห่างจากผู้เล่นเกินระยะที่กำหนด ✨
    /// </summary>
    Transform GetValidSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        List<Transform> validPoints = new List<Transform>();
        Camera mainCam = Camera.main;

        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;

            // 1. เช็กระยะห่างระหว่างจุดเกิดกับผู้เล่น
            if (player != null && useDistanceFilter)
            {
                float distToPlayer = Vector3.Distance(point.position, player.position);
                if (distToPlayer < minSpawnDistance)
                {
                    continue; // ข้ามจุดที่อยู่ใกล้ผู้เล่นเกินไป (รวมถึงห้องเดียวกันที่ระยะไม่ถึง)
                }
            }

            // 2. เช็กว่าจุดเกิดอยู่นอกสายตากล้องหรือไม่
            bool isOutsideCamera = true;
            if (mainCam != null)
            {
                Vector3 screenPoint = mainCam.WorldToViewportPoint(point.position);
                // เช็กทั้งขอบจอ X, Y และค่า Z (Z < 0 คืออยู่หลังกล้อง)
                bool isInFrontOfCamera = screenPoint.z > 0;
                bool isInsideScreen = screenPoint.x >= 0 && screenPoint.x <= 1 && screenPoint.y >= 0 && screenPoint.y <= 1;

                if (isInFrontOfCamera && isInsideScreen)
                {
                    isOutsideCamera = false;
                }
            }

            if (isOutsideCamera)
            {
                validPoints.Add(point);
            }
        }

        // หากมีจุดเกิดที่ผ่านเงื่อนไข ให้สุ่มเลือกมา 1 จุด (เพื่อไม่ให้ผีเกิดซ้ำที่เดิมตลอด)
        if (validPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, validPoints.Count);
            return validPoints[randomIndex];
        }

        // Fallback: หากไม่มีจุดไหนผ่านเงื่อนไขเลย ให้เลือกจุดที่อยู่ไกลผู้เล่นมากที่สุดแทน
        return GetFarthestSpawnPoint();
    }

    /// <summary>
    /// ฟังก์ชันสำรอง: หาจุดเกิดที่อยู่ไกลจากผู้เล่นมากที่สุด
    /// </summary>
    Transform GetFarthestSpawnPoint()
    {
        if (player == null || spawnPoints.Length == 0) return spawnPoints.Length > 0 ? spawnPoints[0] : null;

        Transform farthestPoint = spawnPoints[0];
        float maxDistance = 0f;

        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;
            float dist = Vector3.Distance(point.position, player.position);
            if (dist > maxDistance)
            {
                maxDistance = dist;
                farthestPoint = point;
            }
        }

        return farthestPoint;
    }
}