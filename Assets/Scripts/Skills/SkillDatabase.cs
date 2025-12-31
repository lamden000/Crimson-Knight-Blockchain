using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Skill/SkillDatabase")]
public class SkillDatabase : ScriptableObject
{
    private static SkillDatabase _instance;
    public List<SkillObjectData> allSkills;

    public static SkillDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                // Load từ Resources
                _instance = Resources.Load<SkillDatabase>("Skills/Data/Database/Skill Database");
                
                if (_instance == null)
                {
                    Debug.LogError("[SkillDatabase] Không tìm thấy SkillDatabase.asset trong Resources!");
                }
            }
            return _instance;
        }
    }

  /*  public SkillObjectData GetSkillByName(string name)
    {
     //   return allSkills.Find(s => s.skillName == name);
    }*/

    public SkillObjectData GetSkillByID(int id)
    {
        if (allSkills == null || id < 0 || id >= allSkills.Count) return null;
        return allSkills[id];
    }

    /// <summary>
    /// Tìm SkillObjectData trong database theo reference
    /// </summary>
    public int GetSkillID(SkillObjectData skillData)
    {
        if (allSkills == null || skillData == null) return -1;
        
        for (int i = 0; i < allSkills.Count; i++)
        {
            if (allSkills[i] == skillData)
            {
                return i;
            }
        }
        return -1;
    }
}
