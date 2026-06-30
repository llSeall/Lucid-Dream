using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Current Inventory")]
    public List<string> currentItems = new List<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SyncFromSaveManager();
    }

    private void Update()
    {
        HandleCheatKeys();
    }

    public void AddItem(string itemName)
    {
        if (!currentItems.Contains(itemName))
        {
            currentItems.Add(itemName);
            Debug.Log($"<color=yellow><b>[Inventory] เก็บไอเทมสำเร็จ: + {itemName} ในกระเป๋าแล้ว!</b></color>");
            UpdateSaveManagerInventory();
        }
        else
        {
            Debug.Log($"[Inventory] คุณมี {itemName} อยู่ในกระเป๋าแล้ว");
        }
    }

    public void RemoveItem(string itemName)
    {
        if (currentItems.Contains(itemName))
        {
            currentItems.Remove(itemName);
            Debug.Log($"<color=orange><b>[Inventory] ใช้/ลบ ไอเทม: - {itemName} ออกจากกระเป๋าแล้ว</b></color>");
            UpdateSaveManagerInventory();
        }
    }

    public bool HasItem(string itemName)
    {
        return currentItems.Contains(itemName);
    }

    private void UpdateSaveManagerInventory()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.gameData != null)
        {
            SaveManager.Instance.gameData.collectedItems = new List<string>(currentItems);
        }
    }

    public void SyncFromSaveManager()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.gameData != null)
        {
            currentItems = new List<string>(SaveManager.Instance.gameData.collectedItems);
            Debug.Log($"<color=lime>🎒 [Inventory] ซิงค์ของในกระเป๋าจากเซฟสำเร็จ! มีของทั้งหมด {currentItems.Count} ชิ้น</color>");
        }
    }

    private void HandleCheatKeys()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current == null) return;
        if (UnityEngine.InputSystem.Keyboard.current.f7Key.wasPressedThisFrame) AddItem("Camera_Item"); 
        if (UnityEngine.InputSystem.Keyboard.current.f8Key.wasPressedThisFrame) ListAllItems(); 
#else
        if (Input.GetKeyDown(KeyCode.F7)) AddItem("Camera_Item");
        if (Input.GetKeyDown(KeyCode.F8)) ListAllItems();
#endif
    }

    private void ListAllItems()
    {
        if (currentItems.Count == 0)
        {
            Debug.Log("[Inventory] กระเป๋าว่างเปล่า... ไม่มีไอเทมเลย");
            return;
        }

        string allItems = "[Inventory] ของในกระเป๋าตอนนี้: ";
        foreach (string item in currentItems)
        {
            allItems += $"[{item}] ";
        }
        Debug.Log($"<color=cyan>{allItems}</color>");
    }
}