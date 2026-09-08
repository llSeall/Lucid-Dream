using System;
using UnityEngine;

public class StressManager : MonoBehaviour
{
    private static StressManager instance;
    public static StressManager Instance
    {
        get
        {
            // ✨ ลบเซมิโคลอนออกแล้ว ทำงานเป็น get accessor ปกติ
            if (instance == null)
            {
                GameObject go = new GameObject("StressManager (Auto-Created)");
                instance = go.AddComponent<StressManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("⚙️ Stress Settings")]
    [SerializeField] private float maxStress = 100f;
    [SerializeField] private float stressPerGameOver = 25f;
    [Range(0f, 100f)]
    [SerializeField] private float currentStress = 0f;

    [Header("📊 Stress Thresholds")]
    [SerializeField] private float mediumStressThreshold = 34f;
    [SerializeField] private float highStressThreshold = 67f;

    public enum StressStage { Low, Medium, High }

    public event Action<StressStage> OnStressStageChanged;
    public event Action OnHighStressTrigger;

    private StressStage currentStage = StressStage.Low;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void IncreaseStressOnGameOver()
    {
        AddStress(stressPerGameOver);
    }

    public void AddStress(float amount)
    {
        currentStress = Mathf.Clamp(currentStress + amount, 0f, maxStress);
        CheckStageChange();
    }

    public StressStage GetCurrentStage()
    {
        if (currentStress >= highStressThreshold) return StressStage.High;
        if (currentStress >= mediumStressThreshold) return StressStage.Medium;
        return StressStage.Low;
    }

    private void CheckStageChange()
    {
        StressStage newStage = GetCurrentStage();
        if (newStage != currentStage)
        {
            currentStage = newStage;
            OnStressStageChanged?.Invoke(currentStage);
            Debug.Log($"🧠 Stress Stage Changed to: <color=yellow>{currentStage}</color>");

            if (currentStage == StressStage.High)
            {
                OnHighStressTrigger?.Invoke();
            }
        }
    }

    public float CurrentStress => currentStress;

    [ContextMenu("🧪 Test: Add 25 Stress")]
    private void TestAddStress()
    {
        AddStress(25f);
    }
}