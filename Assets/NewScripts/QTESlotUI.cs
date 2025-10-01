using UnityEngine;
using UnityEngine.UI;

public class QTESlotUI : MonoBehaviour
{
    public Text keyText;
    public Image progressBar;   // ต้องเป็น Image แบบ Filled
    public Image background;

    private float timer;
    private float timeLimit;
    private bool isActive = false;

    public void Init(KeyCode key, float limit)
    {
        if (keyText != null)
            keyText.text = key.ToString();

        timeLimit = limit;
        timer = 0f;
        isActive = true;

        if (progressBar != null)
            progressBar.fillAmount = 0f;

        if (background != null)
            background.color = Color.white;
    }

    void Update()
    {
        if (!isActive) return;

        timer += Time.deltaTime;

        if (progressBar != null)
            progressBar.fillAmount = Mathf.Clamp01(timer / timeLimit);

        if (timer >= timeLimit)
        {
            Fail();
        }
    }

    public void Success()
    {
        isActive = false;
        if (background != null) background.color = Color.green;
    }

    public void Fail()
    {
        isActive = false;
        if (background != null) background.color = Color.red;
    }
}
