using System.Collections;
using UnityEngine;

public class OutlineGuideSystem : MonoBehaviour
{
    [Header("🎯 Guide Config")]
    public KeyCode guideKey = KeyCode.V;
    public float guideDuration = 5f;

    [Header("👁️ Eye Blink UI Settings")]
    [Tooltip("ลาก CanvasGroup ของ Panel สีดำที่คลุมทั้งหน้าจอมาใส่ตรงนี้")]
    [SerializeField] private CanvasGroup eyeBlinkCanvasGroup;
    [Tooltip("ความเร็วในการกระพริบตา (หลับตา/ลืมตา) หน่วยเป็นวินาที")]
    [SerializeField] private float blinkDuration = 0.25f;

    [Header("Outline Targets")]
    public Outline[] targetOutlines;

    private bool isGuideActive = false;

    void Start()
    {
        DisableAllOutlines();

        // เซ็ตให้หน้าจอมองเห็นปกติเมื่อเริ่มเกม
        if (eyeBlinkCanvasGroup != null)
        {
            eyeBlinkCanvasGroup.alpha = 0f;
        }
    }

    void Update()
    {
        // ทำงานได้เฉพาะตอนที่ยังไม่อยู่ในสถานะ Guide เท่านั้น (ป้องกันกดซ้ำ)
        if (Input.GetKeyDown(guideKey) && !isGuideActive)
        {
            StartCoroutine(BlinkAndActivateRoutine());
        }
    }

    private IEnumerator BlinkAndActivateRoutine()
    {
        isGuideActive = true;

        // 1. 👁️ กระพริบตาหลับ (Fade หน้าจอเป็นสีดำ)
        if (eyeBlinkCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < blinkDuration)
            {
                timer += Time.deltaTime;
                eyeBlinkCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / blinkDuration);
                yield return null;
            }
            eyeBlinkCanvasGroup.alpha = 1f;
        }

        // 2. ✨ เปิดการทำงานของ Outline (เปิดในจังหวะที่ตามืดสนิท)
        EnableAllOutlines();

        // 3. 👁️ กระพริบตาลืม (Fade หน้าจอกลับมาสว่าง)
        if (eyeBlinkCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < blinkDuration)
            {
                timer += Time.deltaTime;
                eyeBlinkCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / blinkDuration);
                yield return null;
            }
            eyeBlinkCanvasGroup.alpha = 0f;
        }

        // 4. ⏳ แสดงผล Outline ตามระยะเวลาที่กำหนด (เช่น 5 วินาที)
        yield return new WaitForSeconds(guideDuration);

        // 5. ❌ ปิดการทำงานของ Outline เมื่อหมดเวลา
        DisableAllOutlines();
        isGuideActive = false;
    }

    private void EnableAllOutlines()
    {
        foreach (Outline outline in targetOutlines)
        {
            if (outline == null) continue;
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.enabled = true;
        }
    }

    private void DisableAllOutlines()
    {
        foreach (Outline outline in targetOutlines)
        {
            if (outline == null) continue;
            outline.enabled = false;
        }
    }
}