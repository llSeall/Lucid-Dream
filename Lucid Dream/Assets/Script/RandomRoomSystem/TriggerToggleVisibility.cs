using System.Collections.Generic;
using UnityEngine;

public class TriggerToggleVisibility : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("ลาก GameObject ที่ต้องการเปิด/ปิด มาใส่ในรายการนี้ (ใส่ได้หลายอัน)")]
    [SerializeField] private List<GameObject> targetObjects = new List<GameObject>();

    [Header("Trigger Settings")]
    [Tooltip("Tag ของตัวละครผู้เล่น")]
    [SerializeField] private string playerTag = "Player";

    public enum ToggleType
    {
        Toggle,     // สลับ เปิด <-> ปิด
        TurnOn,     // เปิดอย่างเดียว (Show)
        TurnOff     // ปิดอย่างเดียว (Hide)
    }

    [Tooltip("เลือกโหมดการทำงานเมื่อชน")]
    [SerializeField] private ToggleType actionType = ToggleType.Toggle;

    [Tooltip("ทำงานเฉพาะครั้งแรกที่ชนเท่านั้น")]
    [SerializeField] private bool triggerOnce = false;

    [Tooltip("ติ๊กถูกถ้าต้องการซ่อนแค่ตัวภาพ (Renderer) โดยให้ Collider/Script ของวัตถุยังทำงานอยู่")]
    [SerializeField] private bool hideMeshOnly = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            ExecuteToggle();

            if (triggerOnce)
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }

    private void ExecuteToggle()
    {
        if (targetObjects == null || targetObjects.Count == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] ยังไม่ได้ใส่ Target Objects ใน Inspector!", gameObject);
            return;
        }

        foreach (GameObject obj in targetObjects)
        {
            if (obj == null) continue;

            if (hideMeshOnly)
            {
                // ซ่อน/แสดง เฉพาะภาพ (Renderer)
                Renderer rend = obj.GetComponent<Renderer>();
                if (rend != null)
                {
                    switch (actionType)
                    {
                        case ToggleType.Toggle:
                            rend.enabled = !rend.enabled;
                            break;
                        case ToggleType.TurnOn:
                            rend.enabled = true;
                            break;
                        case ToggleType.TurnOff:
                            rend.enabled = false;
                            break;
                    }
                }
            }
            else
            {
                // เปิด/ปิดการทำงานทั้ง GameObject (เหมือนการกดปิดตาใน Hierarchy)
                switch (actionType)
                {
                    case ToggleType.Toggle:
                        obj.SetActive(!obj.activeSelf);
                        break;
                    case ToggleType.TurnOn:
                        obj.SetActive(true);
                        break;
                    case ToggleType.TurnOff:
                        obj.SetActive(false);
                        break;
                }
            }
        }
    }
}