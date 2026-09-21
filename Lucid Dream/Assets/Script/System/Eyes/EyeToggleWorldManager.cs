using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EyeToggleWorldManager : MonoBehaviour
{
    [Header("🎥 Camera & Feature Settings")]
    [SerializeField] private Transform playerCamera;

    [Header("🎨 Full Screen Pass Renderer Feature")]
    [SerializeField] private UniversalRendererData rendererData;
    [SerializeField] private string featureName = "Pencil Sketch Pass";

    [Header("👁️ Eye Blink Settings")]
    [SerializeField] private CanvasGroup eyeOverlayCanvasGroup;
    [SerializeField] private float fadeCloseDuration = 0.25f;
    [SerializeField] private float eyeClosedPause = 0.1f;
    [SerializeField] private float fadeOpenDuration = 0.35f;

    [Header("🔋 Eye Stamina Settings ✨")]
    [SerializeField] private float maxEyeStamina = 100f;
    [SerializeField] private float staminaDrainRate = 20f;   // อัตราการลดลงเมื่อหลับตา
    [SerializeField] private float staminaRegenRate = 25f;   // อัตราการฟื้นฟูเมื่อเปิดตา
    [SerializeField] private bool hideBarWhenFull = true;     // ซ่อนหลอดเมื่อเต็มหลอด
    [SerializeField] private float uiFadeSpeed = 5f;          // ความเร็วในการจางเข้า/ออกของ UI

    [Header("📊 Eye Stamina UI ✨")]
    [Tooltip("RectTransform ของรูปหลอด Stamina (ให้ตั้ง Pivot X = 0.5 ใน Unity เพื่อให้หุบเข้าตรงกลาง)")]
    [SerializeField] private RectTransform eyeStaminaFillRect;
    [Tooltip("CanvasGroup ของหลอด UI สำหรับควบคุมการจางเข้า/ออก")]
    [SerializeField] private CanvasGroup eyeStaminaCanvasGroup;
    [Header("🩸 Eye Blood Vessels UI (4 Levels) ✨")]
    [Tooltip("Component Image บน Screen Overlay สำหรับแสดงสไปร์ทเส้นเลือด")]
    [SerializeField] private Image bloodVesselsImage;
    [Tooltip("ใส่ Sprite เส้นเลือด 4 ระดับ (0 = น้อยสุด/เพิ่งเริ่มหลับตา, 3 = เยอะสุด/ใกล้หมดหลอด)")]
    [SerializeField] private Sprite[] bloodVesselSprites = new Sprite[4];

    [Header("👁️ Blood Vessels Fade Settings ✨")]
    [Tooltip("ความชัด/จางเริ่มต้นตอนเพิ่งหลับตา (หลอดเต็ม) เช่น 0.1 = จางมากๆ")]
    [Range(0f, 1f)][SerializeField] private float minBloodAlpha = 0.1f;
    [Tooltip("ความชัด/จางสูงสุดตอนใกล้หมดหลอด เช่น 1.0 = ชัดเต็มร้อย")]
    [Range(0f, 1f)][SerializeField] private float maxBloodAlpha = 1.0f;
    [System.Serializable]
    public struct AlternateObjectPair
    {
        public string pairName;
        public GameObject normalWorldObject;
        public GameObject closedEyeWorldObject;
    }

    [Header("🔄 Object Switching Lists")]
    [SerializeField] private List<AlternateObjectPair> objectPairs = new List<AlternateObjectPair>();
    [SerializeField] private List<GameObject> closedEyeOnlyObjects = new List<GameObject>();
    [SerializeField] private List<GameObject> normalOnlyObjects = new List<GameObject>();

    private ScriptableRendererFeature sketchFeature;
    private bool isEyesClosed = false;
    private bool isTransitioning = false;

    // ระบบ Stamina
    private float currentEyeStamina;
    private bool isExhausted = false; // ติดสถานะหมดหลอด ต้องรอเต็ม 100% ถึงจะใช้อีกรอบได้

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

        if (rendererData != null)
        {
            sketchFeature = rendererData.rendererFeatures.Find(f => f.name == featureName);
        }

        currentEyeStamina = maxEyeStamina;

        if (eyeStaminaCanvasGroup != null && hideBarWhenFull)
        {
            eyeStaminaCanvasGroup.alpha = 0f;
        }

        ToggleSketchEffect(false);
        ApplyWorldState(false);
    }

    private void Update()
    {
        HandleEyeStamina();
        HandleInput();
    }

    private void HandleEyeStamina()
    {
        // 1. คำนวณการลดลง / ฟื้นฟู Stamina
        if (isEyesClosed && !isTransitioning)
        {
            currentEyeStamina -= staminaDrainRate * Time.deltaTime;

            // เมื่อพลังการใช้ตาหมดหลอด
            if (currentEyeStamina <= 0f)
            {
                currentEyeStamina = 0f;
                isExhausted = true; // ล็อคการใช้งานจนกว่าจะเต็ม
                StartCoroutine(ForceOpenEyesRoutine()); // บังคับเปิดตาอัตโนมัติ
            }
        }
        else if (!isEyesClosed)
        {
            currentEyeStamina += staminaRegenRate * Time.deltaTime;

            if (currentEyeStamina >= maxEyeStamina)
            {
                currentEyeStamina = maxEyeStamina;
                isExhausted = false; // ฟื้นฟูเต็มหลอดแล้ว ปลดล็อคให้ใช้ได้อีกครั้ง
            }
        }

        // 2. อัปเดตขนาด UI หลอด Stamina (หุบเข้าตรงกลาง)
        if (eyeStaminaFillRect != null)
        {
            float ratio = currentEyeStamina / maxEyeStamina;
            eyeStaminaFillRect.localScale = new Vector3(ratio, 1f, 1f);
        }

        // 3. ควบคุมการแสดงผล UI หลอด Stamina (จางเข้า/ออก นุ่มนวล)
        if (eyeStaminaCanvasGroup != null)
        {
            bool shouldShow = isEyesClosed || (hideBarWhenFull && currentEyeStamina < maxEyeStamina);
            float targetAlpha = shouldShow ? 1f : 0f;
            eyeStaminaCanvasGroup.alpha = Mathf.MoveTowards(eyeStaminaCanvasGroup.alpha, targetAlpha, uiFadeSpeed * Time.deltaTime);
        }

        // 4. แสดงผลเส้นเลือดขึ้นตาตามระดับ Stamina
        UpdateBloodVesselsUI();
    }

    private void UpdateBloodVesselsUI()
    {
        if (bloodVesselsImage == null || bloodVesselSprites == null || bloodVesselSprites.Length == 0) return;

        if (isEyesClosed)
        {
            bloodVesselsImage.enabled = true;

            // 1. คำนวณอัตราส่วน Stamina (1.0 คือเต็มหลอด / 0.0 คือหมดหลอด)
            float staminaRatio = Mathf.Clamp01(currentEyeStamina / maxEyeStamina);

            // 2. คำนวณอัตราการใช้พลังงาน (0.0 ตอนเพิ่งหลับตา -> 1.0 ตอนใกล้หมดหลอด)
            float depletionRatio = 1f - staminaRatio;

            // 3. เปลี่ยน Sprite ตามระดับ Stamina (4 ระดับ)
            int spriteIndex = 3 - Mathf.Clamp(Mathf.FloorToInt(staminaRatio * 4f), 0, 3);
            if (spriteIndex < bloodVesselSprites.Length && bloodVesselSprites[spriteIndex] != null)
            {
                bloodVesselsImage.sprite = bloodVesselSprites[spriteIndex];
            }

            // 4. ✨ ปรับความชัด (Alpha) ให้ค่อยๆ เพิ่มขึ้นตามอัตราการถูกใช้งาน
            Color currentColor = bloodVesselsImage.color;
            float targetAlpha = Mathf.Lerp(minBloodAlpha, maxBloodAlpha, depletionRatio);
            bloodVesselsImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);
        }
        else
        {
            bloodVesselsImage.enabled = false;
        }
    }

    private void HandleInput()
    {
        if (isTransitioning) return;

        bool leftClickPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            leftClickPressed = true;
#else
        if (Input.GetMouseButtonDown(0))
            leftClickPressed = true;
#endif

        if (leftClickPressed)
        {
            // ถ้าหลอดหมดและยังไม่เต็ม 100% จะไม่อนุญาตให้กดหลับตา
            if (!isEyesClosed && isExhausted)
            {
                Debug.Log("⚠️ พลังการใช้ตาหมด! ต้องรอชาร์จจนเต็มหลอดก่อน");
                return;
            }

            StartCoroutine(BlinkAndToggleWorldRoutine());
        }
    }

    private IEnumerator BlinkAndToggleWorldRoutine()
    {
        isTransitioning = true;

        float timer = 0f;
        while (timer < fadeCloseDuration)
        {
            timer += Time.deltaTime;
            if (eyeOverlayCanvasGroup != null)
                eyeOverlayCanvasGroup.alpha = Mathf.Clamp01(timer / fadeCloseDuration);
            yield return null;
        }

        isEyesClosed = !isEyesClosed;
        ApplyWorldState(isEyesClosed);
        ToggleSketchEffect(isEyesClosed);

        yield return new WaitForSeconds(eyeClosedPause);

        timer = 0f;
        while (timer < fadeOpenDuration)
        {
            timer += Time.deltaTime;
            if (eyeOverlayCanvasGroup != null)
                eyeOverlayCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / fadeOpenDuration));
            yield return null;
        }

        isTransitioning = false;
    }

    private IEnumerator ForceOpenEyesRoutine()
    {
        if (!isEyesClosed || isTransitioning) yield break;

        isTransitioning = true;

        // สลับสถานะโลกกลับเป็นปกติทันทีเมื่อหมดหลอด
        isEyesClosed = false;
        ApplyWorldState(false);
        ToggleSketchEffect(false);

        // จางแผ่นดำออก (เปิดตาอัตโนมัติ)
        float timer = 0f;
        while (timer < fadeOpenDuration)
        {
            timer += Time.deltaTime;
            if (eyeOverlayCanvasGroup != null)
                eyeOverlayCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / fadeOpenDuration));
            yield return null;
        }

        isTransitioning = false;
    }

    private void ToggleSketchEffect(bool active)
    {
        if (sketchFeature != null)
        {
            sketchFeature.SetActive(active);
        }
    }

    private void ApplyWorldState(bool closedState)
    {
        foreach (var pair in objectPairs)
        {
            if (pair.normalWorldObject != null) pair.normalWorldObject.SetActive(!closedState);
            if (pair.closedEyeWorldObject != null) pair.closedEyeWorldObject.SetActive(closedState);
        }

        foreach (var obj in closedEyeOnlyObjects)
        {
            if (obj != null) obj.SetActive(closedState);
        }

        foreach (var obj in normalOnlyObjects)
        {
            if (obj != null) obj.SetActive(!closedState);
        }
    }

    private void OnDisable()
    {
        ToggleSketchEffect(false);
    }
    /// <summary>
    /// ✨ ถอดวัตถุออกจากระบบ EyeToggle เพื่อไม่ให้โดนสั่งเปิด/ปิดสลับไปมาอีกต่อไป
    /// </summary>
    public void UnregisterObject(GameObject obj)
    {
        if (obj == null) return;

        closedEyeOnlyObjects.Remove(obj);
        normalOnlyObjects.Remove(obj);
        objectPairs.RemoveAll(pair => pair.normalWorldObject == obj || pair.closedEyeWorldObject == obj);
    }
}