using UnityEngine;

public class AttackAnimationController : MonoBehaviour
{
    public Animator animator;
    public RuntimeAnimatorController wizardAttackAC;
    public AnimatorOverrideController knightAttackAC;
    public AnimatorOverrideController assassinAttackAC;
    public AnimatorOverrideController markmanAttackAC;

    public void PlayAttackAnimation(Direction dir)
    {
        animator.SetTrigger(dir.ToString());
    }

    public void SetWeaponType(CharacterPart weaponType)
    {
        switch (weaponType)
        {
            case CharacterPart.Sword:
                animator.runtimeAnimatorController = knightAttackAC;
                break;
            case CharacterPart.Knive:
                animator.runtimeAnimatorController = assassinAttackAC;
                break;
            case CharacterPart.Staff:
                animator.runtimeAnimatorController = wizardAttackAC;
                break;
            case CharacterPart.Gun:
                animator.runtimeAnimatorController = markmanAttackAC;
                break;
            default:
                Debug.LogWarning("Unsupported weapon type for attack animation.");
                break;
        }
    }
}
