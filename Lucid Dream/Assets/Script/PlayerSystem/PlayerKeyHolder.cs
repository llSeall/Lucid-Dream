using System.Collections.Generic;
using UnityEngine;

public class PlayerKeyHolder : MonoBehaviour
{
    public static PlayerKeyHolder Instance;

    private HashSet<string> collectedKeys = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddKey(string keyID)
    {
        if (!collectedKeys.Contains(keyID))
        {
            collectedKeys.Add(keyID);
            Debug.Log($"[Key System] เก็บกุญแจ: {keyID} เรียบร้อยแล้ว");
        }
    }

    /// <summary>
    /// ✨ ฟังก์ชันลบกุญแจเฉพาะดอกออกจากกระเป๋าผู้เล่น
    /// </summary>
    public void RemoveKey(string keyID)
    {
        if (collectedKeys.Contains(keyID))
        {
            collectedKeys.Remove(keyID);
            Debug.Log($"[Key System] 🗝️ ริบกุญแจคืน: '{keyID}' ออกจากตัวผู้เล่นเรียบร้อยแล้ว");
        }
    }

    public bool HasKey(string keyID)
    {
        return collectedKeys.Contains(keyID);
    }
}