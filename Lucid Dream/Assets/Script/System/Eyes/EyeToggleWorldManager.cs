using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EyeToggleWorldManager : MonoBehaviour
{
    [Header("🎥 Camera Reference")]
    [SerializeField] private Transform playerCamera;

    [Header("👁️ Eye Blink & Overlay Settings")]
    [Tooltip("CanvasGroup ของภาพมืด/เปลือกตาหลับ")]
    [SerializeField] private CanvasGroup eyeOverlayCanvasGroup;
    [Tooltip("ระยะเวลาในการหลับตาลงมามืดสนิท (วินาที)")]
    [SerializeField] private float fadeCloseDuration = 0.25f;
    [Tooltip("ระยะเวลาค้างไว้ตอนตาปิดสนิทก่อนจะลืมตาขึ้น (วินาที)")]
    [SerializeField] private float eyeClosedPause = 0.1f;
    [Tooltip("ระยะเวลาในการลืมตากลับมามองเห็น (วินาที)")]
    [SerializeField] private float fadeOpenDuration = 0.35f;

    [Header("🎨 Post-Processing Volume (Black & White)")]
    [Tooltip("Volume ของโลกขาวดำ (ให้ตั้ง Priority ใน Inspector ให้สูงกว่า Volume หลัก)")]
    [SerializeField] private Volume blackAndWhiteVolume;

    [System.Serializable]
    public struct AlternateObjectPair
    {
        public string pairName;
        [Tooltip("วัตถุโลกปกติ (ลืมตา)")]
        public GameObject normalWorldObject;
        [Tooltip("วัตถุโลกขาวดำ (หลับตา)")]
        public GameObject closedEyeWorldObject;
    }

    [Header("🔄 Object Switching Lists")]
    [SerializeField] private List<AlternateObjectPair> objectPairs = new List<AlternateObjectPair>();
    [SerializeField] private List<GameObject> closedEyeOnlyObjects = new List<GameObject>();
    [SerializeField] private List<GameObject> normalOnlyObjects = new List<GameObject>();

    // Private States
    private bool isEyesClosed = false;
    private bool isTransitioning = false; // กันผู้เล่นคลิกรัวระหว่างกระพริบตา

    public bool IsEyesClosed => isEyesClosed;

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        if (eyeOverlayCanvasGroup != null)
        {
            eyeOverlayCanvasGroup.alpha = 0f;
        }

        if (blackAndWhiteVolume != null)
        {
            blackAndWhiteVolume.weight = 0f;
        }

        ApplyWorldState(false);
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // ถ้ากำลังอยู่ในช่วงกระพริบตา ห้ามรับ Input ซ้ำ
        if (isTransitioning) return;

        bool leftClickPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            leftClickPressed = true;
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            leftClickPressed = true;
        }
#endif

        if (leftClickPressed)
        {
            StartCoroutine(BlinkAndToggleWorldRoutine());
        }
    }

    private IEnumerator BlinkAndToggleWorldRoutine()
    {
        isTransitioning = true;

        // -------------------------------------------------------------
        // ขั้นที่ 1: หลับตาลง (Fade Overlay เป็นดำสนิท)
        // -------------------------------------------------------------
        float timer = 0f;
        while (timer < fadeCloseDuration)
        {
            timer += Time.deltaTime;
            if (eyeOverlayCanvasGroup != null)
            {
                eyeOverlayCanvasGroup.alpha = Mathf.Clamp01(timer / fadeCloseDuration);
            }
            yield return null;
        }

        if (eyeOverlayCanvasGroup != null) eyeOverlayCanvasGroup.alpha = 1f;

        // -------------------------------------------------------------
        // ขั้นที่ 2: ช่วงตาปิดสนิท -> สลับสถานะโลก + สลับ Volume Filter
        // -------------------------------------------------------------
        isEyesClosed = !isEyesClosed;

        // สลับวัตถุในฉาก
        ApplyWorldState(isEyesClosed);

        // สลับ Volume Filter
        if (blackAndWhiteVolume != null)
        {
            blackAndWhiteVolume.weight = isEyesClosed ? 1f : 0f;
        }

        // ค้างไว้แป๊บหนึ่งให้รู้สึกถึงการหลับตา
        yield return new WaitForSeconds(eyeClosedPause);

        // -------------------------------------------------------------
        // ขั้นที่ 3: ลืมตาขึ้น (Fade Overlay กลับเป็นโปร่งใส เผยโลกใหม่)
        // -------------------------------------------------------------
        timer = 0f;
        while (timer < fadeOpenDuration)
        {
            timer += Time.deltaTime;
            if (eyeOverlayCanvasGroup != null)
            {
                eyeOverlayCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / fadeOpenDuration));
            }
            yield return null;
        }

        if (eyeOverlayCanvasGroup != null) eyeOverlayCanvasGroup.alpha = 0f;

        isTransitioning = false;
    }

    private void ApplyWorldState(bool closedState)
    {
        // 1. วัตถุแบบจับคู่สลับร่าง
        foreach (var pair in objectPairs)
        {
            if (pair.normalWorldObject != null)
                pair.normalWorldObject.SetActive(!closedState);

            if (pair.closedEyeWorldObject != null)
                pair.closedEyeWorldObject.SetActive(closedState);
        }

        // 2. วัตถุโผล่เฉพาะโลกขาวดำ
        foreach (var obj in closedEyeOnlyObjects)
        {
            if (obj != null) obj.SetActive(closedState);
        }

        // 3. วัตถุหายไปในโลกขาวดำ
        foreach (var obj in normalOnlyObjects)
        {
            if (obj != null) obj.SetActive(!closedState);
        }
    }
}