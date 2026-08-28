using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class EyeCloseToggleManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก Camera หลักของตัวละครมาใส่ เพื่อให้อิงมุมก้ม-เงยตามสายตาผู้เล่นจริง")]
    [SerializeField] private Transform playerCamera;

    [Header("UI Settings")]
    [SerializeField] private CanvasGroup eyeOverlayCanvasGroup;

    [Header("Eye Close Settings")]
    [SerializeField] private float holdDuration = 3.0f;
    [SerializeField] private float fadeSpeed = 3.0f;

    [Header("Toggle Behavior Settings ✨")]
    [Tooltip("ติ๊กถูก = สิ่งของแต่ละชิ้นจะถูกสลับสถานะได้แค่ 1 ครั้งเท่านั้น (หลับตาซ้ำกี่ครั้งก็ได้ แต่ชิ้นที่เปลี่ยนไปแล้วจะไม่เปลี่ยนกลับ)")]
    [SerializeField] private bool toggleOnlyOncePerObject = true;

    [Header("Vision Settings (3D FOV Cone) 👁️")]
    [Tooltip("ระยะยาวของกรวยสายตาด้านหน้า (เมตร)")]
    [SerializeField] private float viewDistance = 15.0f;
    [Tooltip("องศาความกว้างกรวยสายตา (เช่น 60 ถึง 90 องศา)")]
    [Range(0f, 180f)]
    [SerializeField] private float viewAngle = 60.0f;

    [Header("Target Objects to Toggle")]
    [Tooltip("ใส่สิ่งของใน Scene ที่ต้องการให้สลับเปิด/ปิดเมื่อหลับตา-ลืมตา")]
    [SerializeField] private List<GameObject> targetObjects = new List<GameObject>();

    // Private State Variables
    private float currentHoldTimer = 0f;
    private bool isHoldingKey = false;
    private bool hasCompletedHold = false;
    private float targetAlpha = 0f;

    // ✨ ชุดข้อมูลเก็บบันทึกวัตถุที่เคยถูกสลับไปแล้วแบบรายชิ้น
    private HashSet<GameObject> toggledObjectsHistory = new HashSet<GameObject>();

    void Start()
    {
        if (eyeOverlayCanvasGroup != null)
        {
            eyeOverlayCanvasGroup.alpha = 0f;
        }

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        HandleInput();
        HandleEyeFade();
    }

    private void HandleInput()
    {
        // สามารถกด F หลับตาได้เรื่อยๆ ไม่มีตัวกั้นจำกัดจำนวนครั้งการกด
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

    private void ToggleTargetObjects()
    {
        foreach (GameObject obj in targetObjects)
        {
            if (obj == null) continue;

            // ✨ หากเปิดระบบสลับรายชิ้นครั้งเดียว แล้ววัตถุชิ้นนี้เคยโดนสลับไปแล้ว ให้ข้ามทันที
            if (toggleOnlyOncePerObject && toggledObjectsHistory.Contains(obj))
            {
                continue;
            }

            if (IsInCameraViewCone(obj.transform))
            {
                bool currentStatus = obj.activeSelf;
                obj.SetActive(!currentStatus);

                // บันทึกไว้ว่าวัตถุชิ้นนี้เคยถูกสลับแล้ว
                toggledObjectsHistory.Add(obj);
            }
        }
    }

    private bool IsInCameraViewCone(Transform targetTransform)
    {
        Transform eyeOrigin = (playerCamera != null) ? playerCamera : transform;

        Vector3 dirToTarget = targetTransform.position - eyeOrigin.position;
        float distance = dirToTarget.magnitude;

        if (distance > viewDistance) return false;

        float angle = Vector3.Angle(eyeOrigin.forward, dirToTarget.normalized);
        return angle <= (viewAngle / 2f);
    }

    private void OnDrawGizmosSelected()
    {
        Transform eyeOrigin = (playerCamera != null) ? playerCamera : transform;

        Gizmos.color = Color.yellow;
        Vector3 forward = eyeOrigin.forward;
        Vector3 right = eyeOrigin.right;
        Vector3 up = eyeOrigin.up;

        float halfAngle = viewAngle / 2f;

        Vector3 leftDir = Quaternion.AngleAxis(-halfAngle, up) * forward;
        Vector3 rightDir = Quaternion.AngleAxis(halfAngle, up) * forward;
        Vector3 topDir = Quaternion.AngleAxis(-halfAngle, right) * forward;
        Vector3 bottomDir = Quaternion.AngleAxis(halfAngle, right) * forward;

        Gizmos.DrawRay(eyeOrigin.position, leftDir * viewDistance);
        Gizmos.DrawRay(eyeOrigin.position, rightDir * viewDistance);
        Gizmos.DrawRay(eyeOrigin.position, topDir * viewDistance);
        Gizmos.DrawRay(eyeOrigin.position, bottomDir * viewDistance);

        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawRay(eyeOrigin.position, forward * viewDistance);
    }
}