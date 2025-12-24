using UnityEngine;

public class SkillBarUI : MonoBehaviour
{
    [Header("Skill Button References")]
    [SerializeField] private SkillButtonUI skillButton1;
    [SerializeField] private SkillButtonUI skillButton2;
    [SerializeField] private SkillButtonUI skillButton3;

    private void Start()
    {
        // Đảm bảo các button được set đúng slot
        if (skillButton1 != null)
            skillButton1.SetSkillSlot(1);
        if (skillButton2 != null)
            skillButton2.SetSkillSlot(2);
        if (skillButton3 != null)
            skillButton3.SetSkillSlot(3);
    }
}

