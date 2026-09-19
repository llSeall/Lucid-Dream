using UnityEngine;
using UnityEngine.Localization;
using TMPro;

public class WallTextUpdater : MonoBehaviour
{
    [SerializeField] private TMP_Text wallTextMesh;
    [SerializeField] private LocalizedString localizedString;

    private void OnEnable()
    {
        // ดักฟัง Event เมื่อมีการสั่งเปลี่ยนภาษาจากระบบ
        localizedString.StringChanged += OnStringChanged;
    }

    private void OnDisable()
    {
        localizedString.StringChanged -= OnStringChanged;
    }

    private void OnStringChanged(string translatedText)
    {
        if (wallTextMesh != null)
        {
            wallTextMesh.text = translatedText;
        }
    }
}