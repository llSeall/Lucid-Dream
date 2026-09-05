using System.Collections;
using UnityEngine;

public class ComputerInteraction : MonoBehaviour
{
    public static bool IsInteractingWithPC { get; private set; } = false;

    [Header("🎥 Camera Zoom Settings")]
    [SerializeField] private Transform computerScreenTarget;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private GameObject pcCanvasUI;

    [Header("🔑 Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private KeyCode exitKey = KeyCode.Escape;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private Transform playerTransform;

    private Camera mainCam;
    private Transform originalCamParent;
    private Vector3 originalCamLocalPos;
    private Quaternion originalCamLocalRot;
    private bool isTransitioning = false;

    private void Start()
    {
        mainCam = Camera.main;
        if (pcCanvasUI != null) pcCanvasUI.SetActive(false);
    }

    private void Update()
    {
        if (isTransitioning) return;

        if (!IsInteractingWithPC && IsPlayerInRange() && Input.GetKeyDown(interactKey))
        {
            StartCoroutine(TransitionToPC(true));
        }
        else if (IsInteractingWithPC && Input.GetKeyDown(exitKey))
        {
            StartCoroutine(TransitionToPC(false));
        }
    }

    private bool IsPlayerInRange()
    {
        if (playerTransform == null && mainCam != null)
            playerTransform = mainCam.transform;
        if (playerTransform == null) return false;

        return Vector3.Distance(transform.position, playerTransform.position) <= interactDistance;
    }

    private IEnumerator TransitionToPC(bool entering)
    {
        isTransitioning = true;

        if (entering)
        {
            IsInteractingWithPC = true;

            originalCamParent = mainCam.transform.parent;
            originalCamLocalPos = mainCam.transform.localPosition;
            originalCamLocalRot = mainCam.transform.localRotation;

            mainCam.transform.SetParent(null);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (ComputerUIManager.Instance != null)
            {
                ComputerUIManager.Instance.CloseAllWindows();
            }
        }
        else
        {
            if (pcCanvasUI != null) pcCanvasUI.SetActive(false);
        }

        Vector3 startPos = mainCam.transform.position;
        Quaternion startRot = mainCam.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * transitionSpeed;

            Vector3 targetPos;
            Quaternion targetRot;

            if (entering)
            {
                targetPos = computerScreenTarget.position;
                targetRot = computerScreenTarget.rotation;
            }
            else
            {
                targetPos = (originalCamParent != null) ? originalCamParent.TransformPoint(originalCamLocalPos) : startPos;
                targetRot = (originalCamParent != null) ? originalCamParent.rotation * originalCamLocalRot : startRot;
            }

            mainCam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        if (entering)
        {
            mainCam.transform.position = computerScreenTarget.position;
            mainCam.transform.rotation = computerScreenTarget.rotation;
            if (pcCanvasUI != null) pcCanvasUI.SetActive(true);

            // ✨ เมื่อเปิดคอมสำเร็จ สั่งทำงานแจ้งเตือนทันที (ถ้ายังไม่ได้แสดงในวันนั้น)
            if (ComputerUIManager.Instance != null)
            {
                ComputerUIManager.Instance.TryShowDailyNotification();
            }
        }
        else
        {
            mainCam.transform.SetParent(originalCamParent);
            mainCam.transform.localPosition = originalCamLocalPos;
            mainCam.transform.localRotation = originalCamLocalRot;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            IsInteractingWithPC = false;
        }

        isTransitioning = false;
    }
}