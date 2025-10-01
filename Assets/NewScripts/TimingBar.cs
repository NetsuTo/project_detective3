using System;
using UnityEngine;
using UnityEngine.UI;

public class TimingBar : MonoBehaviour
{
    public RectTransform pointer;     // เข็ม
    public RectTransform targetZone;  // Perfect zone
    public float speed = 200f;        // pixels/sec
    public bool loop = true;

    private float direction = 1f;
    private Action<bool> onComplete;
    private bool isActive = false;

    public void StartTiming(Action<bool> callback)
    {
        onComplete = callback;
        isActive = true;
        pointer.anchoredPosition = Vector2.zero; // เริ่มตรงกลาง bar
    }

    void Update()
    {
        if (!isActive) return;

        // move pointer
        pointer.anchoredPosition += Vector2.right * speed * direction * Time.deltaTime;

        // reverse direction
        if (loop)
        {
            float halfWidth = ((RectTransform)transform).rect.width / 2f;
            if (pointer.anchoredPosition.x > halfWidth)
            {
                pointer.anchoredPosition = new Vector2(halfWidth, pointer.anchoredPosition.y);
                direction *= -1f;
            }
            else if (pointer.anchoredPosition.x < -halfWidth)
            {
                pointer.anchoredPosition = new Vector2(-halfWidth, pointer.anchoredPosition.y);
                direction *= -1f;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool success = IsPointerInTarget();
            onComplete?.Invoke(success);
            isActive = false;
        }

    }

    bool IsPointerInTarget()
    {
        float pointerX = pointer.anchoredPosition.x;
        float targetLeft = targetZone.anchoredPosition.x - targetZone.rect.width / 2f;
        float targetRight = targetZone.anchoredPosition.x + targetZone.rect.width / 2f;
        return pointerX >= targetLeft && pointerX <= targetRight;
    }
}
