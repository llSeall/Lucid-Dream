using UnityEngine;

public class JumpscareTarget : MonoBehaviour
{
    [Header("🔊 Audio FX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip scareSound;

    [Header("⚙️ Settings")]
    [Tooltip("เล่นเสียงแค่ครั้งเดียวหรือไม่ (ถ้าไม่ติ๊ก เสียงจะเล่นใหม่ได้เมื่อมองอีกรอบ)")]
    [SerializeField] private bool triggerOnce = true;

    public bool HasBeenTriggered { get; private set; } = false;

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void TriggerJumpscare()
    {
        if (triggerOnce && HasBeenTriggered) return;

        HasBeenTriggered = true;

        if (audioSource != null && scareSound != null)
        {
            audioSource.PlayOneShot(scareSound);
        }

        Debug.Log($"👻 Jumpscare Triggered on: {gameObject.name}");
    }
}