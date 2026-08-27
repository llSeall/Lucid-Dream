using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EyeCloseSceneLoader : MonoBehaviour
{
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
    [Tooltip("ระยะเวลาที่ต้องกด F ค้างไว้จนปิดตาสนิทแล้วเปลี่ยนซีน (วินาที)")]
    [SerializeField] private float holdDuration = 1.5f;
    [Tooltip("ความเร็วในการย้อนเฟรมกลับ (ค่อยๆ เปิดตาคืน) เมื่อปล่อยปุ่มกลางทาง")]
    [SerializeField] private float fadeOutSpeed = 2f;
    [Tooltip("ปุ่มที่ใช้กดค้าง")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    private float currentHoldTime = 0f;
    private bool isSceneLoading = false;

    void Start()
    {
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

        // 1. เมื่อผู้เล่นกดปุ่มค้างไว้ (Input.GetKey)
        if (Input.GetKey(interactKey))
        {
            currentHoldTime += Time.deltaTime;
            currentHoldTime = Mathf.Min(currentHoldTime, holdDuration);

            UpdateEyeCloseVisuals();

            // เมื่อกดค้างครบเวลา (ตาปิดสนิทแล้ว) -> สั่งย้ายซีน
            if (currentHoldTime >= holdDuration)
            {
                LoadNextScene();
            }
        }
        // 2. ถ้าปล่อยปุ่มก่อนกดครบเวลา ให้ค่อยๆ ย้อนเฟรมกลับ (เปิดตาคืนมา)
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
                // เมื่อเปิดตาสนิท (0%) ให้ปิดการแสดงผล UI Image
                if (eyeCloseImage != null && eyeCloseImage.gameObject.activeSelf)
                {
                    eyeCloseImage.gameObject.SetActive(false);
                }
            }
        }
    }

    private void UpdateEyeCloseVisuals()
    {
        // คำนวณ เปอร์เซ็นต์ความคืบหน้า (0.0 ถึง 1.0)
        float progress = currentHoldTime / holdDuration;

        // แสดงและเปลี่ยนสไปร์ปิดตาตาม Progress
        if (eyeCloseImage != null)
        {
            if (!eyeCloseImage.gameObject.activeSelf)
            {
                eyeCloseImage.gameObject.SetActive(true);
            }

            if (eyeCloseSprites != null && eyeCloseSprites.Length > 0)
            {
                // คำนวณหา Index ของภาพสไปร์ตามเปอร์เซ็นต์การกด
                int spriteIndex = Mathf.Clamp(Mathf.FloorToInt(progress * eyeCloseSprites.Length), 0, eyeCloseSprites.Length - 1);
                eyeCloseImage.sprite = eyeCloseSprites[spriteIndex];
            }
        }

        // ปรับจอดำสนิทควบคู่ไปด้วย เพื่อรองพื้นในกรณีสไปร์ไม่ได้บังมืดทั้งหน้าจอ
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = progress;
        }
    }

    private void LoadNextScene()
    {
        isSceneLoading = true;

        // บังคับให้หน้าจอดำสนิทแน่นอนก่อนเปลี่ยนซีน
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