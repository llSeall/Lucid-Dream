using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // หากใช้ New Input System

public class EyeCloseToggleManager : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("CanvasGroup ของภาพสีดำเต็มจอสำหรับทำ Effect หลับตา")]
    [SerializeField] private CanvasGroup eyeOverlayCanvasGroup;

    [Header("Eye Close Settings")]
    [Tooltip("ระยะเวลาที่ต้องกด F ค้างเพื่อสลับสถานะของสิ่งของ (วินาที)")]
    [SerializeField] private float holdDuration = 3.0f;
    [Tooltip("ความเร็วในการลืมตา/หลับตา")]
    [SerializeField] private float fadeSpeed = 3.0f;

    [Header("Distance Settings ✨")]
    [Tooltip("รัศมีรอบตัวผู้เล่นที่จะส่งผลกระทบต่อสิ่งของ (หน่วยเป็นเมตร)")]
    [SerializeField] private float interactionRadius = 5.0f;

    [Header("Target Objects to Toggle")]
    [Tooltip("ใส่สิ่งของหรือวัตถุใน Scene ที่ต้องการให้สลับเปิด/ปิดเมื่อลืมตา")]
    [SerializeField] private List<GameObject> targetObjects = new List<GameObject>();

    // Private State Variables
    private float currentHoldTimer = 0f;
    private bool isHoldingKey = false;
    private bool hasCompletedHold = false;
    private float targetAlpha = 0f;

    void Start()
    {
        if (eyeOverlayCanvasGroup != null)
        {
            eyeOverlayCanvasGroup.alpha = 0f;
        }
    }

    void Update()
    {
        HandleInput();
        HandleEyeFade();
    }

    private void HandleInput()
    {
        // ตรวจจับการกดปุ่ม F (รองรับทั้ง New Input System และ Legacy Input System)
        bool fKeyPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.fKey.isPressed)
        {
            fKeyPressed = true;
        }
#else
        if (Input.GetKey(KeyCode.F))
        {
            fKeyPressed = true;
        }
#endif

        if (fKeyPressed)
        {
            isHoldingKey = true;
            currentHoldTimer += Time.deltaTime;

            // ค่อยๆ ปรับหน้าจอให้มืดลงตามเวลาที่กดค้าง
            targetAlpha = Mathf.Clamp01(currentHoldTimer / holdDuration);

            // เช็คว่ากดค้างครบกำหนดเวลาหรือยัง
            if (currentHoldTimer >= holdDuration)
            {
                hasCompletedHold = true;
            }
        }
        else
        {
            // เมื่อปล่อยปุ่ม F
            if (isHoldingKey)
            {
                // ถ้ากดค้างครบตามเวลาที่กำหนดแล้วค่อยปล่อยปุ่ม ให้ทำการสลับสถานะวัตถุ
                if (hasCompletedHold)
                {
                    ToggleTargetObjects();
                }

                // รีเซ็ตค่าการกด
                isHoldingKey = false;
                hasCompletedHold = false;
                currentHoldTimer = 0f;
            }

            // ค่อยๆ ปรับหน้าจอให้สว่างขึ้น (ลืมตา)
            targetAlpha = 0f;
        }
    }

    private void HandleEyeFade()
    {
        if (eyeOverlayCanvasGroup != null)
        {
            // เฟดค่า Alpha ของ CanvasGroup ให้จางเข้า-ออกอย่างนุ่มนวล
            eyeOverlayCanvasGroup.alpha = Mathf.MoveTowards(
                eyeOverlayCanvasGroup.alpha,
                targetAlpha,
                fadeSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// สลับสถานะ (Invert Active State) ของวัตถุที่อยู่ในระยะ interactionRadius
    /// </summary>
    private void ToggleTargetObjects()
    {
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null)
            {
                // คำนวณระยะห่างระหว่างตัวผู้เล่นกับวัตถุชิ้นนั้นๆ
                float distance = Vector3.Distance(transform.position, obj.transform.position);

                // สลับสถานะเฉพาะวัตถุที่อยู่ในระยะรัศมีเท่านั้น
                if (distance <= interactionRadius)
                {
                    bool currentStatus = obj.activeSelf;
                    obj.SetActive(!currentStatus);
                }
            }
        }
    }

    // วาดเส้นรัศมีสีเหลืองรอบตัวผู้เล่นในหน้าต่าง Scene View เพื่อความสะดวกในการตั้งค่า
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}