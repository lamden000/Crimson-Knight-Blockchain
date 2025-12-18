using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Skill/SkillDatabase")]
public class SkillDatabase : ScriptableObject
{
    public List<SkillObjectData> allSkills;

  /*  public SkillObjectData GetSkillByName(string name)
    {
     //   return allSkills.Find(s => s.skillName == name);
    }*/

    public SkillObjectData GetSkillByID(int id)
    {
        if (id < 0 || id >= allSkills.Count) return null;
        return allSkills[id];
    }
}
