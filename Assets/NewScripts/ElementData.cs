using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Element Recipe Database", menuName = "Element System/Recipe Database")]
public class ElementRecipeDatabase : ScriptableObject
{
    [Header("All Recipes")]
    [Tooltip("รายการสูตรทั้งหมดในเกม")]
    public List<ElementRecipe> allRecipes = new List<ElementRecipe>();

    // ? หาสูตรที่ตรงกับ sequence (รองรับทั้ง string และ KeyCode)
    public ElementRecipe FindMatchingRecipe(List<string> sequence)
    {
        foreach (ElementRecipe recipe in allRecipes)
        {
            if (recipe.MatchesString(sequence))
                return recipe;
        }
        return null;
    }

    // สำหรับ backward compatibility กับ KeyCode
    public ElementRecipe FindMatchingRecipe(List<KeyCode> sequence)
    {
        foreach (ElementRecipe recipe in allRecipes)
        {
            if (recipe.MatchesKeyCode(sequence))
                return recipe;
        }
        return null;
    }
}

[System.Serializable]
public class ElementRecipe
{
    [Header("Recipe Info")]
    [Tooltip("ชื่อธาตุ เช่น 'Water (H2O)', 'Nitrous Oxide (N2O)'")]
    public string elementName;

    [Tooltip("สูตรการผสม เช่น H, H, O (ใช้ตัวอักษรธาตุ)")]
    public List<string> elementSequence = new List<string>();

    [Header("Visual")]
    [Tooltip("รูปขวดสำหรับสูตรนี้โดยเฉพาะ")]
    public Sprite bottleSprite;

    [Tooltip("สีของขวด")]
    public Color bottleColor = Color.white;

    [Header("Optional")]
    public GameObject particleEffect;

    // ? เช็คว่า sequence ตรงกับสูตรนี้หรือไม่ (string version)
    public bool MatchesString(List<string> inputSequence)
    {
        if (inputSequence == null || elementSequence == null) return false;
        if (inputSequence.Count != elementSequence.Count) return false;

        for (int i = 0; i < elementSequence.Count; i++)
        {
            if (inputSequence[i] != elementSequence[i]) return false;
        }
        return true;
    }

    // สำหรับ backward compatibility กับ KeyCode
    public bool MatchesKeyCode(List<KeyCode> inputSequence)
    {
        if (inputSequence == null || elementSequence == null) return false;
        if (inputSequence.Count != elementSequence.Count) return false;

        for (int i = 0; i < elementSequence.Count; i++)
        {
            if (inputSequence[i].ToString() != elementSequence[i]) return false;
        }
        return true;
    }
}