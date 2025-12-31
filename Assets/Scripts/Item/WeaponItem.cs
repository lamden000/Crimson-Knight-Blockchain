using UnityEngine;
using System.Collections;

/// <summary>
/// Weapon item - kế thừa từ EquipmentItem, có thêm khả năng spawn skill khi tấn công
/// </summary>
[CreateAssetMenu(menuName = "Game/Items/Weapon")]
public class WeaponItem : EquipmentItem
{
    [Header("Weapon Attack Skill")]
    [Tooltip("Skill được spawn khi tấn công thường (để null nếu không có)")]
    public SkillSpawnData attackSkill;
    
    [Tooltip("Xác suất spawn skill khi tấn công (0-1, ví dụ: 0.3 = 30%)")]
    [Range(0f, 1f)]
    public float attackSkillChance = 0f;
    
    [Tooltip("Delay trước khi spawn skill (giây) - để sync với animation tấn công")]
    [Range(0f, 2f)]
    public float attackSkillDelay = 0f;
    
    [Tooltip("Damage bonus từ weapon")]
    public int attackDamage = 0;

    /// <summary>
    /// Kiểm tra xem có spawn skill khi tấn công không
    /// </summary>
    public bool ShouldSpawnSkillOnAttack()
    {
        if (attackSkill == null) return false;
        if (attackSkillChance <= 0f) return false;
        
        // Random theo xác suất
        return Random.value <= attackSkillChance;
    }

    /// <summary>
    /// Spawn skill khi tấn công (với delay)
    /// Cần gọi từ MonoBehaviour để có thể dùng coroutine
    /// </summary>
    public void SpawnAttackSkill(MonoBehaviour coroutineRunner, Vector3 casterPosition, Vector3 targetPosition, Transform target = null)
    {
        if (attackSkill == null || coroutineRunner == null) return;

        // Spawn skill với delay để sync với animation
        coroutineRunner.StartCoroutine(SpawnWithDelay(casterPosition, targetPosition, target));
    }

    /// <summary>
    /// Coroutine để spawn skill với delay
    /// </summary>
    private IEnumerator SpawnWithDelay(Vector3 casterPosition, Vector3 targetPosition, Transform target)
    {
        // Đợi delay
        if (attackSkillDelay > 0f)
        {
            yield return new WaitForSeconds(attackSkillDelay);
        }

        // Spawn skill
        if (SkillManager.Instance == null)
        {
            Debug.LogWarning("[WeaponItem] SkillManager.Instance is null!");
            yield break;
        }

        GameObject skillPrefab = SkillManager.Instance.SkillPrefab;
        if (skillPrefab == null)
        {
            Debug.LogWarning("[WeaponItem] Không tìm thấy skillPrefab!");
            yield break;
        }
        
        // Spawn qua PhotonNetwork nếu trong multiplayer
        GameObject obj;
        if (Photon.Pun.PhotonNetwork.IsConnected && Photon.Pun.PhotonNetwork.InRoom)
        {
            obj = Photon.Pun.PhotonNetwork.Instantiate("Prefabs/Skill", casterPosition, Quaternion.identity);
        }
        else
        {
            obj = Instantiate(skillPrefab, casterPosition, Quaternion.identity);
        }

        var skillSpawner = obj.GetComponent<SkillSpawnmer>();
        if (skillSpawner != null)
        {
            skillSpawner.Init(attackSkill, casterPosition, targetPosition, target);
        }
        else
        {
            Debug.LogError("[WeaponItem] SkillPrefab không có SkillSpawnmer component!");
            if (Photon.Pun.PhotonNetwork.IsConnected && Photon.Pun.PhotonNetwork.InRoom)
            {
                Photon.Pun.PhotonNetwork.Destroy(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
    }
}

