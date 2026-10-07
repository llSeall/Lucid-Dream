using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CheatManager : MonoBehaviour
{
    [Header("General Settings 🛠️")]
    [Tooltip("เปิด/ปิดการใช้งานระบบสูตรโกงทั้งหมด")]
    [SerializeField] private bool enableCheatSystem = true;

    [Header("Player Reference")]
    [Tooltip("ใส่ PlayerController3D หากลากใส่เองไม่ได้ ระบบจะหาให้อัตโนมัติในฉาก")]
    [SerializeField] private PlayerController3D_InputAction playerController;

    [Header("1. Infinite Stamina Cheat ⚡")]
    [SerializeField] private bool isInfiniteStamina = false;
    [Tooltip("ปุ่มสำหรับกด สลับเปิด/ปิด สูตรสเตมิน่าไม่จำกัด (เช่น F1)")]
    [SerializeField] private KeyCode toggleStaminaKey = KeyCode.F1;

    [System.Serializable]
    public struct SceneWarpData
    {
        [Tooltip("ชื่อฉาก/ซีนที่ต้องการย้ายไป (ต้องพิมพ์ให้ตรงกับใน Build Settings)")]
        public string sceneName;
        [Tooltip("ปุ่ม F1, F2, F3... ที่ใช้กดเพื่อวาร์ปไปฉากนี้")]
        public KeyCode warpKey;
    }

    [Header("2. Scene Warp Cheat 🌀")]
    [Tooltip("กดปุ่ม + ด้านล่างเพื่อเพิ่มฉากและกำหนดปุ่มกดย้ายซีนได้ไม่จำกัด")]
    [SerializeField]
    private SceneWarpData[] sceneWarps = new SceneWarpData[]
    {
        new SceneWarpData { sceneName = "Scene_01", warpKey = KeyCode.F2 },
        new SceneWarpData { sceneName = "Scene_02", warpKey = KeyCode.F3 },
        new SceneWarpData { sceneName = "Scene_03", warpKey = KeyCode.F4 }
    };

    void Awake()
    {
        // ค้นหา PlayerController3D อัตโนมัติหากยังไม่ได้ใส่ใน Inspector
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController3D_InputAction>();
        }
    }

    void Update()
    {
        if (!enableCheatSystem) return;

        // ---------------------------------------------
        // 1. ระบบโกงสเตมิน่า (Toggle กดเปิด/ปิด)
        // ---------------------------------------------
        if (Input.GetKeyDown(toggleStaminaKey))
        {
            isInfiniteStamina = !isInfiniteStamina;
            Debug.Log($"<color=cyan><b>[Cheat] Infinite Stamina: {(isInfiniteStamina ? "ON 🟢" : "OFF 🔴")}</b></color>");
        }

        if (isInfiniteStamina)
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController3D_InputAction>();
            }

            if (playerController != null)
            {
                playerController.RefillStaminaToMax(); // เติมสเตมิน่าให้เต็มตลอดเวลา
            }
        }

        // ---------------------------------------------
        // 2. ระบบโกงย้ายฉาก (Scene Warping)
        // ---------------------------------------------
        if (sceneWarps != null && sceneWarps.Length > 0)
        {
            foreach (var warp in sceneWarps)
            {
                if (warp.warpKey != KeyCode.None && Input.GetKeyDown(warp.warpKey))
                {
                    if (!string.IsNullOrEmpty(warp.sceneName))
                    {
                        Debug.Log($"<color=green><b>[Cheat] Warping to scene: {warp.sceneName}</b></color>");
                        SceneManager.LoadScene(warp.sceneName);
                        break;
                    }
                }
            }
        }
    }
}