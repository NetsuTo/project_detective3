using UnityEngine;
using UnityEngine.UI;

public class SkillLetterUI : MonoBehaviour
{
    public Image letterImage;         // รูปตัวอักษร
    public LetterIconDatabase iconDB; // Database A-Z

    // เซ็ตภาพครั้งแรก (ตอนเริ่ม)
    public void SetLetter(char c)
    {
        string letter = c.ToString().ToUpper();

        Sprite s = iconDB.GetSprite(letter);
        if (s == null)
        {
            Debug.LogError($"[SkillLetterUI] Sprite not found for letter {letter}");
            return;
        }

        letterImage.sprite = s;
    }

    // ใช้สำหรับตอน cycle เปลี่ยนตัวอักษร
    public void UpdateLetter(char c)
    {
        SetLetter(c);
    }
}
