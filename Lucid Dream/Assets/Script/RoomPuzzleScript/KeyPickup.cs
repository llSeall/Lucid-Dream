using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    [Header("Key Settings")]
    public string keyID = "MainKey";

    [Header("UI Settings")]
    [Tooltip("ลาก GameObject ของ Canvas ที่เป็นรูปภาพ/รูปปุ่ม E มาใส่")]
    public GameObject promptUI;

    private bool isPlayerNearby = false;

    void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
    }

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (PlayerKeyHolder.Instance != null)
            {
                PlayerKeyHolder.Instance.AddKey(keyID);
            }

            if (promptUI != null) promptUI.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (promptUI != null) promptUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }
}