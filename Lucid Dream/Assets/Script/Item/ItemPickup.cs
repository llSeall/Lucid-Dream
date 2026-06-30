using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Item Data Link")]
    //  วิธีใช้: สร้างวัตถุ 3D ในฉาก แปะสคริปต์นี้ แล้วลากไฟล์ ItemData ชิ้นที่ต้องการระบุมาใส่ช่องนี้
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
            // ส่ง ID ของไอเทมนี้เข้าคลังกระเป๋าหลัก
            InventoryManager.Instance.AddItem(itemData.itemID);

            // ทำลายวัตถุนี้ออกจากฉาก (ถือว่าโดนเก็บไปแล้ว)
            Destroy(gameObject);
        }
    }

    // ตัวอย่างลอจิกเดินชนแล้วเก็บทันที (คุณสามารถเปลี่ยนเป็นระบบเดินไปใกล้แล้วกดปุ่ม E ทีหลังได้ครับ)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Pickup();
        }
    }
}