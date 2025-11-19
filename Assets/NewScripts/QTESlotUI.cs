using UnityEngine;
using UnityEngine.UI;

public class QTESlotUI : MonoBehaviour
{
    public Text keyText;
    public Image progressBar;   // ต้องเป็น Image แบบ Filled
    public Image background;

    public Image letterImage;
    public LetterIconDatabase iconDB;

    private float timer;
    private float timeLimit;
    private bool isActive = false;

    public void Init(KeyCode key, float limit)
    {
        // แปลง KeyCode ? ตัวอักษร เช่น KeyCode.H ? "H"
        string letter = key.ToString().ToUpper();

        // ถ้า KeyCode เป็นรูปแบบ Alpha1, Alpha2 ? ตัดคำหน้าออกให้เหลือตัวเลข
        if (letter.StartsWith("ALPHA"))
            letter = letter.Replace("ALPHA", "");

        // ถ้าเป็น KeyCode.None หรือมีความยาวไม่ใช่ 1 ? ถือว่าผิด ให้กันไว้
        if (letter.Length > 1)
            letter = letter.Substring(0, 1);

        // Debug ช่วยตรวจสอบ
        Debug.Log($"[QTE] Init slot ? Key = {key}, FinalLetter = {letter}");

        // เซ็ต sprite จาก database
        if (letterImage != null && iconDB != null)
        {
            Sprite s = iconDB.GetSprite(letter);

            if (s == null)
            {
                Debug.LogError($"[QTE] ? Sprite for letter '{letter}' NOT FOUND in database!");
                // ป้องกันไม่ให้ขึ้นเป็นสีขาวล้วน ? ใช้ sprite default แทนถ้ามี
                // letterImage.sprite = defaultPlaceholder;
            }
            else
            {
                letterImage.sprite = s;
            }
        }
        else
        {
            Debug.LogError("[QTE] ? letterImage หรือ iconDB = NULL");
        }

        // ตั้งค่าพื้นฐานของ slot
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
