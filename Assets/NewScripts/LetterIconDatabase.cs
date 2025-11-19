using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LetterIconDB", menuName = "QTE/Letter Icon Database")]
public class LetterIconDatabase : ScriptableObject
{
    public List<LetterIcon> icons;

    [System.Serializable]
    public class LetterIcon
    {
        public string letter;   // "A"
        public Sprite sprite;   // รูป A.png
    }

    private Dictionary<string, Sprite> dict;

    void OnEnable()
    {
        dict = new Dictionary<string, Sprite>();
        foreach (var i in icons)
        {
            if (!dict.ContainsKey(i.letter.ToUpper()))
                dict.Add(i.letter.ToUpper(), i.sprite);
        }
    }

    public Sprite GetSprite(string letter)
    {
        dict.TryGetValue(letter.ToUpper(), out Sprite s);
        return s;
    }
}
