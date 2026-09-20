using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class JumpscareUI : MonoBehaviour
{
    public static JumpscareUI Instance { get; private set; }

    [Header("UI Components")]
    public Image jumpscareImage;
    public AudioSource audioSource;

    private Coroutine animCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// สั่งแสดง Jumpscare แบบภาพเคลื่อนไหว (รับ Array ของ Sprite)
    /// </summary>
    public void ShowJumpscareAnimation(Sprite[] ghostFrames, float frameRate, AudioClip screamSound)
    {
        gameObject.SetActive(true);

        // เล่นเสียงกรี๊ด
        if (audioSource != null && screamSound != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.PlayOneShot(screamSound);
        }

        // เริ่มเล่นอนิเมชันเปลี่ยนเฟรมภาพ
        if (ghostFrames != null && ghostFrames.Length > 0)
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(PlaySpriteSequence(ghostFrames, frameRate));
        }
    }

    private IEnumerator PlaySpriteSequence(Sprite[] frames, float frameRate)
    {
        float delayBetweenFrames = 1f / frameRate;
        int currentFrame = 0;

        while (true)
        {
            if (jumpscareImage != null)
            {
                jumpscareImage.sprite = frames[currentFrame];
            }

            // วนลูปเฟรมภาพไปเรื่อยๆ (0, 1, 2, ..., กลับมา 0)
            currentFrame = (currentFrame + 1) % frames.Length;

            yield return new WaitForSeconds(delayBetweenFrames);
        }
    }

    public void HideJumpscare()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        gameObject.SetActive(false);
    }
}