using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Item Data Link")]
    // ลากไฟล์ ItemData ชิ้นที่ต้องการสร้างจากโปรเจกต์มาใส่ในช่องนี้
    public ItemData itemData;

    // ฟังก์ชันการเก็บไอเทม
    public void Pickup()
    {
        if (itemData == null)
        {
            Debug.LogError($"[ItemPickup] วัตถุ {gameObject.name} ในฉากยังไม่ได้ใส่ข้อมูลไอเทมในช่อง itemData!");
            return;
        }

        if (InventoryManager.Instance != null)
        {
            // ส่ง ID ของไอเทมชิ้นนี้เข้าไปเก็บในคลังกระเป๋าหลัก
            InventoryManager.Instance.AddItem(itemData.itemID);

            // ทำลายวัตถุนี้ออกจากฉาก (ถือว่าโดนผู้เล่นเก็บไปแล้ว)
            Destroy(gameObject);
        }
    }

    // ลอจิกเดินชนแล้วเรียกฟังก์ชันเก็บไอเทมทันที
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Pickup();
        }
    }
}