using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("UI Canvas Panels")]
    [Tooltip("Panel หน้าเมนูหยุดเกม (Resume / Settings / Main Menu)")]
    [SerializeField] private GameObject pauseMenuUI;

    [Tooltip("Panel หน้าตั้งค่าเกม (Settings)")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // ปิด UI ทั้งหมดเมื่อเริ่มฉาก
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    private void Update()
    {
        // ตรวจจับการกดปุ่ม Esc
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ✨ [เพิ่มส่วนนี้] ถ้าผู้เล่นกำลังส่องจอคอมพิวเตอร์อยู่ ให้ข้ามการเปิด Pause Menu 
            // เพื่อปล่อยให้ ComputerInteraction.cs ทำหน้าที่พาผู้เล่นออกจากหน้าจอคอมก่อน
            if (ComputerInteraction.IsInteractingWithPC)
            {
                return;
            }

            // ถ้าหน้า Settings กำลังเปิดอยู่ ให้ปิด Settings แล้วกลับมาหน้า Pause Menu
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            // ถ้าอยู่ในหน้า Pause Menu ให้กลับเข้าเกม
            else if (isPaused)
            {
                Resume();
            }
            // ถ้ากำลังเล่นเกมอยู่ ให้หยุดเกมและเปิดหน้า Pause Menu
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Pause()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// สั่งเปิดหน้า Settings (ผูกกับปุ่ม Settings บน Pause Menu UI)
    /// </summary>
    public void OpenSettings()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    /// <summary>
    /// สั่งปิดหน้า Settings ย้อนกลับมา Pause Menu (ผูกกับปุ่ม Back บน Settings UI หรือกด Esc)
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}