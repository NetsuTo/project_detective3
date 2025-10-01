using System.Collections.Generic;
using UnityEngine;

public class SkillQTEBridge : MonoBehaviour
{
    public QTEManager qteManager;

    public void OnLetterChosen(char chosenLetter, List<char> originalLetters)
    {
        if (qteManager == null) return;

        List<KeyCode> sequence = new List<KeyCode>();
        bool addCurrent = false;

        foreach (char c in originalLetters)
        {
            if (c == chosenLetter) addCurrent = true;
            if (addCurrent)
            {
                if (System.Enum.TryParse(c.ToString().ToUpper(), out KeyCode code))
                {
                    sequence.Add(code);
                }
            }
        }

        Debug.Log("[SkillQTEBridge] StartQTE with sequence length: " + sequence.Count);
        qteManager.StartQTE(sequence);
    }
}
