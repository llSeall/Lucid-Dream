using UnityEngine;

public class OutlineGuideSystem : MonoBehaviour
{
    public KeyCode guideKey = KeyCode.V;

    public float guideDuration = 5f;

    public Outline[] targetOutlines;

    private bool isGuideActive = false;
    private float guideTimer;

    void Start()
    {
        DisableAllOutlines();
    }

    void Update()
    {
        if (Input.GetKeyDown(guideKey))
        {
            ActivateGuide();
        }

        if (isGuideActive)
        {
            guideTimer -= Time.deltaTime;

            if (guideTimer <= 0f)
            {
                DeactivateGuide();
            }
        }
    }

    void ActivateGuide()
    {
        isGuideActive = true;
        guideTimer = guideDuration;

        foreach (Outline outline in targetOutlines)
        {
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.enabled = true;
        }
    }

    void DeactivateGuide()
    {
        isGuideActive = false;

        DisableAllOutlines();
    }

    void DisableAllOutlines()
    {
        foreach (Outline outline in targetOutlines)
        {
            outline.enabled = false;
        }
    }
}