using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class FadeBlackSceneLoader : MonoBehaviour
{
    [Header("Trigger Mode Settings ✨")]
    [Tooltip("ติ๊กถูก: เมื่อผู้เล่นเดินเหยียบ จะเริ่มค่อยๆ เฟดดำและย้ายซีนอัตโนมัติทันที")]
    [SerializeField] private bool autoTriggerOnStep = true;
    [Tooltip("ติ๊กถูก: ถ้าต้องการให้ต้อง 'เดินเหยียบเข้ามาในโซน + กดปุ่มค้าง' ถึงจะเฟดดำ")]
    [SerializeField] private bool requireKeyPressInsideZone = false;

    [Header("Scene Settings")]
    [Tooltip("ใส่ชื่อ Scene ที่ต้องการจะเปลี่ยนไป")]
    [SerializeField] private string sceneToLoad;

    [Header("Fade Settings ⬛")]
    [Tooltip("CanvasGroup สีดำสนิทที่ใช้ทำเอฟเฟกต์เฟด (ลาก UI Panel สีดำที่มี CanvasGroup มาใส่)")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Input & Timing Settings")]
    [Tooltip("ระยะเวลาที่ใช้ในการค่อยๆ เฟดจนจอดำสนิทแล้วเปลี่ยนซีน (วินาที)")]
    [SerializeField] private float fadeDuration = 1.5f;
    [Tooltip("ความเร็วในการย้อนกลับ (ค่อยๆ สว่างคืน) เมื่อปล่อยปุ่มหรือถอยออกจากโซนกลางทาง")]
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

        // รีเซ็ตค่าความโปร่งใสของหน้าจอเป็น 0 (สว่างปกติ)
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false; // ป้องกันไม่ให้บังการคลิก UI อื่นๆ
        }
    }

    void Update()
    {
        if (isSceneLoading) return;

        bool shouldFade = false;

        if (autoTriggerOnStep)
        {
            if (requireKeyPressInsideZone)
            {
                // เงื่อนไข: ต้องอยู่ใน Trigger Zone + กดปุ่มค้างไว้
                shouldFade = isPlayerInZone && Input.GetKey(interactKey);
            }
            else
            {
                // เงื่อนไข: แค่เดินเหยียบเข้ามาใน Trigger ก็เริ่มเฟดดำเปลี่ยนซีนทันที
                shouldFade = isPlayerInZone || hasBeenTriggered;
            }
        }
        else
        {
            // โหมดกดปุ่มค้างไว้ตรงไหนก็ได้ในเกม
            shouldFade = Input.GetKey(interactKey);
        }

        // 1. กระบวนการค่อยๆ เฟดดำ
        if (shouldFade)
        {
            currentHoldTime += Time.deltaTime;
            currentHoldTime = Mathf.Min(currentHoldTime, fadeDuration);

            UpdateFadeVisuals();

            // เมื่อจอดำสนิทครบเวลา -> สั่งย้ายซีน
            if (currentHoldTime >= fadeDuration)
            {
                LoadNextScene();
            }
        }
        // 2. ถ้ายกเลิก/เดินถอยออกมากลางคัน ให้ค่อยๆ ย้อนกลับ ค่อยๆ สว่างขึ้น
        else
        {
            if (currentHoldTime > 0f)
            {
                currentHoldTime -= Time.deltaTime * fadeOutSpeed;
                currentHoldTime = Mathf.Max(currentHoldTime, 0f);

                UpdateFadeVisuals();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            isPlayerInZone = true;

            // เมื่อเหยียบแล้ว ให้ล็อกสภาวะไว้เพื่อให้กระบวนการเฟดดำรันจนจบและเปลี่ยนซีน
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

    private void UpdateFadeVisuals()
    {
        float progress = currentHoldTime / fadeDuration;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = progress;
            fadeCanvasGroup.blocksRaycasts = progress > 0f;
        }
    }

    private void LoadNextScene()
    {
        isSceneLoading = true;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
        }

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("[FadeBlackSceneLoader] คุณยังไม่ได้ตั้งชื่อ Scene ในช่อง Scene To Load!");
        }
    }
}