using UnityEngine;

public class Script_Outline : MonoBehaviour
{
    public float interactionDistance = 4f;

    private Outline _currentOutline;
    void Start()
    {
        Outline[] allOutlines = FindObjectsOfType<Outline>();

        foreach (Outline outline in allOutlines)
        {
            outline.enabled = false;
        }
    }
    void Update()
    {
        Ray ray = Camera.main.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0)
        );

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            Outline foundOutline = hit.collider.GetComponent<Outline>();

            if (foundOutline != null)
            {
                if (_currentOutline == foundOutline)
                    return;

                if (_currentOutline != null)
                    _currentOutline.enabled = false;

                _currentOutline = foundOutline;
                _currentOutline.enabled = true;
            }
            else
            {
                Clear();
            }
        }
        else
        {
            Clear();
        }
    }

    void Clear()
    {
        if (_currentOutline != null)
        {
            _currentOutline.enabled = false;
            _currentOutline = null;
        }
    }
}