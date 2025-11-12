using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Game/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillID;
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;

    [Header("Prefab สำหรับ Success Symbol")]
    public GameObject successSymbolPrefab;  // ? prefab ที่จะ spawn ตอน QTE สำเร็จ
}
