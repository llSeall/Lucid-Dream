using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Sanity Settings")]
    public float maxSanity = 100f;
    public float currentSanity;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SyncWithSaveManager();
    }

    private void Update()
    {
        HandleCheatKeys();
    }

    public void ModifySanity(float amount)
    {
        currentSanity += amount;
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);


        OnSanityChanged();
    }

    public void SyncWithSaveManager()
    {
        if (SaveManager.Instance != null)
        {
            currentSanity = SaveManager.Instance.gameData.currentSanity;
            Debug.Log($"<color=lime>❤️ [PlayerStats] ซิงค์ค่าสติจาก SaveManager สำเร็จ! ปัจจุบันคือ: {currentSanity}</color>");
        }
    }

    private void OnSanityChanged()
    {
        if (currentSanity <= 30f)
        {
            Debug.LogWarning($"<color=red><b>[PlayerStats] เตือนภัย! ค่าสติวิกฤต ({currentSanity:0.0}): ผีจะเริ่มคลั่งแล้ว!</b></color>");
        }
    }

    public void HitByGhost(float sanityDamage)
    {
        Debug.Log($"<color=purple>--- [PlayerStats] โดนผีชน เสียค่าสติ: {sanityDamage} ---</color>");
        ModifySanity(-sanityDamage);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ghost")) HitByGhost(10f);
    }

    private void HandleCheatKeys()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current == null) return;
        if (UnityEngine.InputSystem.Keyboard.current.f5Key.wasPressedThisFrame) ModifySanity(-15f); 
        if (UnityEngine.InputSystem.Keyboard.current.f6Key.wasPressedThisFrame) ModifySanity(20f);  
#else
        if (Input.GetKeyDown(KeyCode.F5)) ModifySanity(-15f);
        if (Input.GetKeyDown(KeyCode.F6)) ModifySanity(20f);
#endif
    }
}