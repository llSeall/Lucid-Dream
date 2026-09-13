using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    // ✨ เพิ่ม Property เปิด public ให้สคริปต์อื่นอ่านสถานะหลับตาได้
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

        ToggleSketchEffect(false);
        ApplyWorldState(false);
    }

    private void Update()
    {
        HandleInput();
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

    private void ToggleSketchEffect(bool active)
    {
        if (sketchFeature != null)
        {
            sketchFeature.SetActive(active);
        }
        else
        {
            Debug.LogWarning($"⚠️ ไม่พบ Renderer Feature ชื่อ '{featureName}' ใน UniversalRendererData!");
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
}