using UnityEngine;

public class InventoryTester : MonoBehaviour
{
    private int selectedIndex = 0; // ลำดับไอเทมที่กำลังเลือกอยู่ (เริ่มที่ชิ้นแรกคือ 0)

    void Update()
    {
        // 1. ถ้าไม่มีของในกระเป๋าเลย ให้กด I แล้วเตือนเฉยๆ และไม่ต้องทำลอจิกข้างล่างต่อ
        if (InventoryManager.Instance == null || InventoryManager.Instance.ownedItemIDs.Count == 0)
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                Debug.Log(" [Tester] ในกระเป๋าว่างเปล่า ไม่มีของให้เลือกอ่านเลยครับ!");
            }
            selectedIndex = 0; // รีเซ็ตตัวเลือกกลับเป็น 0
            return;
        }

        int totalItems = InventoryManager.Instance.ownedItemIDs.Count;

        // ⬅️ 2. กดปุ่มลูกศรซ้าย เพื่อเลื่อนไปเลือกไอเทมก่อนหน้า
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectedIndex--;
            if (selectedIndex < 0)
            {
                selectedIndex = totalItems - 1; // ถ้าลดจนเลยชิ้นแรก ให้วนกลับไปเลือกชิ้นสุดท้าย
            }
            LogCurrentSelection();
        }

        // ➡️ 3. กดปุ่มลูกศรขวา เพื่อเลื่อนไปเลือกไอเทมถัดไป
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectedIndex++;
            if (selectedIndex >= totalItems)
            {
                selectedIndex = 0; // ถ้ายาวจนเกินชิ้นสุดท้าย ให้วนกลับมาเริ่มชิ้นแรกใหม่
            }
            LogCurrentSelection();
        }

        //  4. กดปุ่ม "I" บนคีย์บอร์ดเพื่อเปิดอ่านไอเทมชิ้นที่เราเลือกไว้
        if (Input.GetKeyDown(KeyCode.I))
        {
            // ความปลอดภัย: ป้องกันกรณีมีการลบของระหว่างเกมแล้วดัชนีเกินตัวเลขจริง
            if (selectedIndex >= totalItems) selectedIndex = 0;

            string targetItemID = InventoryManager.Instance.ownedItemIDs[selectedIndex];

            // เรียกคำสั่งเปิดอ่าน/ตรวจสอบของชิ้นนั้นๆ
            InventoryManager.Instance.InspectItem(targetItemID);
        }
    }

    // ฟังก์ชันช่วยแสดงข้อความใน Console ว่าตอนนี้เรากำลังเลือกชิ้นไหนอยู่
    private void LogCurrentSelection()
    {
        int totalItems = InventoryManager.Instance.ownedItemIDs.Count;
        string currentID = InventoryManager.Instance.ownedItemIDs[selectedIndex];

        Debug.Log($" [Tester] กำลังเลื่อนดูไอเทมชิ้นที่ [{selectedIndex + 1} / {totalItems}]: ID = <color=yellow>{currentID}</color> (กด I เพื่อเปิดอ่าน)");
    }
}