using UnityEngine;
using DG.Tweening;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Sprite ที่จะ Fade")]
    [SerializeField] private SpriteRenderer spriteToShow;

    [Header("ตั้งค่าการตรวจจับ")]
    [SerializeField] private string playerTag = "Player";

    [Header("ตั้งค่า Fade")]
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;

    private Vector3 originalScale;

    private void Start()
    {
        if (spriteToShow != null)
        {
            originalScale = spriteToShow.transform.localScale;
            Color c = spriteToShow.color;
            c.a = 0f;
            spriteToShow.color = c;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            spriteToShow.DOKill();

            Sequence seq = DOTween.Sequence();
            seq.Append(spriteToShow.DOFade(1f, fadeDuration).SetEase(fadeEase));
            seq.Join(spriteToShow.transform.DOScale(originalScale * 1.1f, fadeDuration * 0.8f));
            seq.Append(spriteToShow.transform.DOScale(originalScale, 0.3f));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) && spriteToShow != null)
        {
            spriteToShow.DOKill();

            // Fade out แบบ smooth
            spriteToShow.DOFade(0f, fadeDuration * 0.7f).SetEase(Ease.InQuad);
        }
    }
}
