using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class EyeCloseSceneLoader : MonoBehaviour
{
    [Header("Trigger Mode Settings ✨")]
    [Tooltip("ติ๊กถูก: เมื่อผู้เล่นเดินเหยียบ จะเริ่มปิดตาและย้ายซีนอัตโนมัติทันที")]
    [SerializeField] private bool autoTriggerOnStep = true;
    [Tooltip("ติ๊กถูก: ถ้าต้องการให้ต้อง 'เดินเหยียบเข้ามาในโซน + กดปุ่มค้าง' ถึงจะทำงาน")]
    [SerializeField] private bool requireKeyPressInsideZone = false;

    [Header("Scene Settings")]
    [Tooltip("ใส่ชื่อ Scene ที่ต้องการจะเปลี่ยนไป")]
    [SerializeField] private string sceneToLoad;

    [Header("Eye Close Sprite Settings")]
    [Tooltip("UI Image ที่ใช้แสดงผลภาพปิดตาบนหน้าจอ")]
    [SerializeField] private Image eyeCloseImage;
    [Tooltip("ใส่ลำดับภาพสไปร์ปิดตา เรียงจาก เฟรมแรก (ตาเปิด) ไปจนถึง เฟรมสุดท้าย (ตาปิดสนิท)")]
    [SerializeField] private Sprite[] eyeCloseSprites;

    [Header("Black Screen Fade Settings")]
    [Tooltip("(Optional) CanvasGroup สีดำสนิทรองพื้นหลังสไปร์ปิดตา เพื่อบังคับให้จอดำสนิทก่อนย้ายซีน")]
    [SerializeField] private CanvasGroup blackScreenCanvasGroup;

    [Header("Input & Timing Settings")]
    [Tooltip("ระยะเวลาที่ใช้ในการค่อยๆ ปิดตาสนิทแล้วเปลี่ยนซีน (วินาที)")]
    [SerializeField] private float holdDuration = 1.5f;
    [Tooltip("ความเร็วในการย้อนเฟรมกลับ (ค่อยๆ เปิดตาคืน) เมื่อปล่อยปุ่มหรือถอยออกจากโซนกลางทาง")]
    [SerializeField] private float fadeOutSpeed = 2f;
    [Tooltip("ปุ่มที่ใช้กดค้าง (กรณีใช้งานโหมดกดปุ่ม)")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    private float currentHoldTime = 0f;
    private bool isSceneLoading = false;
    private bool isPlayerInZone = false;
    private bool hasBeenTriggered = false;

    void Start()
    {
        // ตั้งค่า Collider ของวัตถุนี้ให้เป็น Trigger อัตโนมัติ
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;

        // ซ่อนภาพปิดตาในตอนเริ่มต้น
        if (eyeCloseImage != null)
        {
            eyeCloseImage.gameObject.SetActive(false);
        }

        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 0f;
        }
    }

    void Update()
    {
        if (isSceneLoading) return;

        bool shouldCloseEyes = false;

        if (autoTriggerOnStep)
        {
            if (requireKeyPressInsideZone)
            {
                // เงื่อนไข: ต้องอยู่ใน Trigger Zone + กดปุ่มค้างไว้
                shouldCloseEyes = isPlayerInZone && Input.GetKey(interactKey);
            }
            else
            {
                // เงื่อนไข: แค่เดินเหยียบเข้ามาใน Trigger ก็เริ่มปิดตาเปลี่ยนซีนทันที
                shouldCloseEyes = isPlayerInZone || hasBeenTriggered;
            }
        }
        else
        {
            // โหมดเดิม: กดปุ่มค้างไว้ตรงไหนก็ได้ในเกม
            shouldCloseEyes = Input.GetKey(interactKey);
        }

        // 1. กระบวนการค่อยๆ ปิดตา
        if (shouldCloseEyes)
        {
            currentHoldTime += Time.deltaTime;
            currentHoldTime = Mathf.Min(currentHoldTime, holdDuration);

            UpdateEyeCloseVisuals();

            // เมื่อหลับตาสนิทครบเวลา -> สั่งย้ายซีน
            if (currentHoldTime >= holdDuration)
            {
                LoadNextScene();
            }
        }
        // 2. ถ้ายกเลิก/เดินถอยออกมากลางคัน ให้ค่อยๆ ย้อนเฟรมเปิดตาคืนมา
        else
        {
            if (currentHoldTime > 0f)
            {
                currentHoldTime -= Time.deltaTime * fadeOutSpeed;
                currentHoldTime = Mathf.Max(currentHoldTime, 0f);

                UpdateEyeCloseVisuals();
            }
            else
            {
                if (eyeCloseImage != null && eyeCloseImage.gameObject.activeSelf)
                {
                    eyeCloseImage.gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            isPlayerInZone = true;

            // เมื่อเหยียบแล้ว ให้ล็อกสภาวะไว้เพื่อให้กระบวนการหลับตารันจนจบและเปลี่ยนซีน
            if (autoTriggerOnStep && !requireKeyPressInsideZone)
            {
                hasBeenTriggered = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            isPlayerInZone = false;
        }
    }

    private void UpdateEyeCloseVisuals()
    {
        float progress = currentHoldTime / holdDuration;

        if (eyeCloseImage != null)
        {
            if (!eyeCloseImage.gameObject.activeSelf)
            {
                eyeCloseImage.gameObject.SetActive(true);
            }

            if (eyeCloseSprites != null && eyeCloseSprites.Length > 0)
            {
                int spriteIndex = Mathf.Clamp(Mathf.FloorToInt(progress * eyeCloseSprites.Length), 0, eyeCloseSprites.Length - 1);
                eyeCloseImage.sprite = eyeCloseSprites[spriteIndex];
            }
        }

        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = progress;
        }
    }

    private void LoadNextScene()
    {
        isSceneLoading = true;

        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 1f;
        }

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("[EyeCloseSceneLoader] คุณยังไม่ได้ตั้งชื่อ Scene ในช่อง Scene To Load!");
        }
    }
}