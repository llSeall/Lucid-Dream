using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFaderLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("ใส่ชื่อ Scene ที่ต้องการจะเปลี่ยนไป")]
    [SerializeField] private string sceneToLoad;

    [Header("Fade Settings")]
    [Tooltip("CanvasGroup ภาพสีดำที่จะใช้ทำจอมืด")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [Tooltip("ระยะเวลาที่ต้องกด F ค้างไว้จนจอมืดสนิทแล้วเปลี่ยนซีน (วินาที)")]
    [SerializeField] private float holdDuration = 1.5f;
    [Tooltip("ความเร็วในการย้อนจอสว่างกลับคืนมาเมื่อปล่อยปุ่มกลางทาง")]
    [SerializeField] private float fadeOutSpeed = 2f;
    [Tooltip("ปุ่มที่ใช้กดค้าง")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    private float currentHoldTime = 0f;
    private bool isSceneLoading = false;

    void Start()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
        }
    }

    void Update()
    {
        // ถ้ากำลังย้ายซีนอยู่ ให้ข้ามการทำงาน
        if (isSceneLoading) return;

        // 1. ตรวจสอบการกดปุ่มค้าง (Input.GetKey)
        if (Input.GetKey(interactKey))
        {
            currentHoldTime += Time.deltaTime;
            currentHoldTime = Mathf.Min(currentHoldTime, holdDuration);

            // ปรับความมืดตามเวลาที่กดค้าง
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = currentHoldTime / holdDuration;
            }

            // เมื่อกดค้างครบเวลา ย้ายซีนทันที
            if (currentHoldTime >= holdDuration)
            {
                LoadNextScene();
            }
        }
        // 2. ถ้าปล่อยปุ่มก่อนกดครบเวลา ให้ค่อยๆ จอสว่างคืนมา
        else
        {
            if (currentHoldTime > 0f)
            {
                currentHoldTime -= Time.deltaTime * fadeOutSpeed;
                currentHoldTime = Mathf.Max(currentHoldTime, 0f);

                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.alpha = currentHoldTime / holdDuration;
                }
            }
        }
    }

    private void LoadNextScene()
    {
        isSceneLoading = true;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("[SceneFaderLoader] คุณยังไม่ได้ตั้งชื่อ Scene ในช่อง Scene To Load!");
        }
    }
}