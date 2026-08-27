using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Vision Settings (Triangle FOV) ✨")]
    [Tooltip("ระยะยาวของสามเหลี่ยมการมองเห็นด้านหน้า (หน่วยเป็นเมตร)")]
    [SerializeField] private float viewDistance = 15.0f;
    [Tooltip("องศาความกว้างของสามเหลี่ยมการมองเห็น (เช่น 60 ถึง 90 องศา)")]
    [Range(0f, 180f)]
    [SerializeField] private float viewAngle = 60.0f;

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
            targetAlpha = Mathf.Clamp01(currentHoldTimer / holdDuration);

            if (currentHoldTimer >= holdDuration)
            {
                hasCompletedHold = true;
            }
        }
        else
        {
            if (isHoldingKey)
            {
                if (hasCompletedHold)
                {
                    ToggleTargetObjects();
                }

                isHoldingKey = false;
                hasCompletedHold = false;
                currentHoldTimer = 0f;
            }

            targetAlpha = 0f;
        }
    }

    private void HandleEyeFade()
    {
        if (eyeOverlayCanvasGroup != null)
        {
            eyeOverlayCanvasGroup.alpha = Mathf.MoveTowards(
                eyeOverlayCanvasGroup.alpha,
                targetAlpha,
                fadeSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// สลับสถานะเฉพาะวัตถุที่อยู่ในขอบเขตสามเหลี่ยมการมองเห็นเท่านั้น
    /// </summary>
    private void ToggleTargetObjects()
    {
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null && IsInViewTriangle(obj.transform))
            {
                bool currentStatus = obj.activeSelf;
                obj.SetActive(!currentStatus);
            }
        }
    }

    /// <summary>
    /// ฟังก์ชันคำนวณว่าเป้าหมายอยู่ในสามเหลี่ยมสายตาหรือไม่
    /// </summary>
    private bool IsInViewTriangle(Transform target)
    {
        Vector3 dirToTarget = target.position - transform.position;
        float distance = dirToTarget.magnitude;

        // 1. เช็กว่าเกินระยะความยาวของสามเหลี่ยมหรือไม่
        if (distance > viewDistance) return false;

        // 2. คำนวณองศาระหว่างทิศทางหันหน้าผู้เล่น กับตำแหน่งวัตถุ
        float angle = Vector3.Angle(transform.forward, dirToTarget.normalized);

        // 3. วัตถุต้องอยู่ในขอบเขตครึ่งหนึ่งของ View Angle
        return angle <= (viewAngle / 2f);
    }

    // วาดเส้นขอบเขตสามเหลี่ยมสายตาสีเหลืองในหน้า Scene View
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        // คำนวณทิศทางเส้นขอบซ้ายและขวา
        Vector3 leftDir = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Vector3 leftEndpoint = transform.position + leftDir * viewDistance;
        Vector3 rightEndpoint = transform.position + rightDir * viewDistance;

        // วาดเส้นสามเหลี่ยม
        Gizmos.DrawRay(transform.position, leftDir * viewDistance);
        Gizmos.DrawRay(transform.position, rightDir * viewDistance);
        Gizmos.DrawLine(leftEndpoint, rightEndpoint);

        // เส้นประศูนย์กลางสายตา
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawRay(transform.position, transform.forward * viewDistance);
    }
}