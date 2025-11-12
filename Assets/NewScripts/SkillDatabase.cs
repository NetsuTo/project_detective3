using UnityEngine;
using System.Collections.Generic;

public class SkillDatabase : MonoBehaviour
{
    public List<SkillData> skillList = new List<SkillData>();
    private Dictionary<string, SkillData> skillDict = new Dictionary<string, SkillData>();

    private void Awake()
    {
        skillDict.Clear();
        foreach (var skill in skillList)
        {
            if (skill == null) continue;
            if (!string.IsNullOrEmpty(skill.skillID) && !skillDict.ContainsKey(skill.skillID))
                skillDict.Add(skill.skillID, skill);
        }
    }

    public SkillData GetSkill(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        skillDict.TryGetValue(id, out SkillData data);
        return data;
    }
}
