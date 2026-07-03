using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class MainMenuSaveTester : MonoBehaviour
{
    [Header("📦 UI Slot Buttons Text")]
    [Tooltip("ลาก Text หรือ TextMeshPro ของแต่ละปุ่มมาใส่ เพื่อแสดงสถานะเซฟ")]
    public Text slot1Text;
    public Text slot2Text;
    public Text slot3Text;

    [Header("Scene Configuration")]
    [SerializeField] private string gameplaySceneName = "DaytimeScene";

    private void Start()
    {
        RefreshSlotUI();
    }

    // 🔄 ฟังก์ชันอัปเดตตัวหนังสือบนปุ่มเซฟทั้ง 3 สล็อต
    public void RefreshSlotUI()
    {
        if (SaveManager.Instance == null) return;

        UpdateSlotDisplay(1, slot1Text);
        UpdateSlotDisplay(2, slot2Text);
        UpdateSlotDisplay(3, slot3Text);
    }

    private void UpdateSlotDisplay(int slotID, Text textComponent)
    {
        if (textComponent == null) return;

        // ค้นหาเส้นทางไฟล์เซฟประจำสล็อต
        string path = Path.Combine(Application.persistentDataPath, $"{SaveManager.Instance.saveFileNamePrefix}{slotID}.json");

        if (File.Exists(path))
        {
            try
            {
                // เปิดอ่านไฟล์เซฟชั่วคราวเพื่อดูข้อมูลข้างใน (ไม่โหลดลง RAM จริง)
                string json = File.ReadAllText(path);
                SlotData temporarySlotData = JsonUtility.FromJson<SlotData>(json);

                string phaseName = (temporarySlotData.latestPlayedState == GameState.Daytime) ? "กลางวัน" : "กลางคืนในฝัน";
                textComponent.text = $"สล็อต {slotID}\n[วันที่ {temporarySlotData.latestPlayedDay} - {phaseName}]";
            }
            catch
            {
                textComponent.text = $"สล็อต {slotID}\n[ข้อมูลเสียหาย]";
            }
        }
        else
        {
            textComponent.text = $"สล็อต {slotID}\n[--- ว่าง ---]";
        }
    }

    // 👆 ฟังก์ชันผูกกับปุ่ม: เมื่อผู้เล่นกดเลือกสล็อต (เช่น กดปุ่มสล็อต 1)
    public void OnSelectSlotAndPlay(int slotID)
    {
        if (SaveManager.Instance == null) return;

        string path = Path.Combine(Application.persistentDataPath, $"{SaveManager.Instance.saveFileNamePrefix}{slotID}.json");

        if (File.Exists(path))
        {
            // ถ้ามีไฟล์เก่า ให้โหลดสล็อตนั้นขึ้นมาเล่นต่อทันที (Continue)
            Debug.Log($"[Menu] โหลดไฟล์เซฟเล่นต่อจากสล็อต {slotID}");
            SaveManager.Instance.LoadGame(slotID, true);
        }
        else
        {
            // ถ้าเป็นสล็อตว่าง ให้ตั้งค่าไอดีสล็อต แล้วสั่งเริ่มเกมใหม่ (New Game)
            Debug.Log($"[Menu] สร้างเกมใหม่บนสล็อต {slotID}");
            SaveManager.Instance.currentSlot = slotID;
            SaveManager.Instance.ClearSave(slotID); // รีเซ็ตเดต้าให้สะอาดก่อนเริ่ม
            SceneManager.LoadScene(gameplaySceneName);
        }
    }

    // 🗑️ ฟังก์ชันปุ่มลบเซฟ (สำหรับใช้ทำปุ่ม "ลบข้อมูล" เพื่อเทสระบบใหม่)
    public void OnClickDeleteSlot(int slotID)
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.ClearSave(slotID);
        RefreshSlotUI(); // รีเฟรชหน้าจอทันทีหลังลบ
        Debug.Log($"[Menu] ลบข้อมูลสล็อต {slotID} เรียบร้อย");
    }
}