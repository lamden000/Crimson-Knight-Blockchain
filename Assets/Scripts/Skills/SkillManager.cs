using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

public class SkillManager : MonoBehaviour
{
    [Header("Skill Assignments")]
    [SerializeField] private SkillSpawnData skill1;
    [SerializeField] private SkillSpawnData skill2;
    [SerializeField] private SkillSpawnData skill3;

    [Header("Skill Prefab")]
    [SerializeField] private GameObject skillPrefab; // Prefab chứa SkillSpawnmer component
    
    public GameObject SkillPrefab => skillPrefab; // Public property để WeaponItem có thể access

    private Dictionary<int, float> skillCooldowns = new Dictionary<int, float>();
    private Dictionary<int, float> skillCooldownTimers = new Dictionary<int, float>();

    public static SkillManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Initialize cooldown timers
        if (skill1 != null)
        {
            skillCooldowns[1] = skill1.cooldown;
            skillCooldownTimers[1] = 0f;
        }
        if (skill2 != null)
        {
            skillCooldowns[2] = skill2.cooldown;
            skillCooldownTimers[2] = 0f;
        }
        if (skill3 != null)
        {
            skillCooldowns[3] = skill3.cooldown;
            skillCooldownTimers[3] = 0f;
        }
    }

    private void Update()
    {
        // Update cooldown timers - tạo list copy để tránh lỗi "Collection was modified"
        List<int> keys = new List<int>(skillCooldownTimers.Keys);
        foreach (var key in keys)
        {
            if (skillCooldownTimers.ContainsKey(key) && skillCooldownTimers[key] > 0f)
            {
                skillCooldownTimers[key] -= Time.deltaTime;
                if (skillCooldownTimers[key] < 0f)
                    skillCooldownTimers[key] = 0f;
            }
        }
    }

    public bool TryUseSkill(int skillSlot, Vector3 casterPosition, Vector3 mousePosition, Transform target = null)
    {
        SkillSpawnData skillData = GetSkillData(skillSlot);
        if (skillData == null)
        {
            Debug.LogWarning($"[SkillManager] Skill {skillSlot} chưa được assign!");
            return false;
        }

        // Check cooldown
        if (skillCooldownTimers.ContainsKey(skillSlot) && skillCooldownTimers[skillSlot] > 0f)
        {
            return false;
        }

        // Check target requirement
        if (skillData.requiresTarget && target == null)
        {
            Debug.Log($"[SkillManager] Skill {skillSlot} yêu cầu target nhưng không có target!");
            return false;
        }

        // Use skill
        UseSkill(skillData, casterPosition, mousePosition, target);

        // Start cooldown
        if (skillCooldowns.ContainsKey(skillSlot))
        {
            skillCooldownTimers[skillSlot] = skillCooldowns[skillSlot];
        }

        return true;
    }

    private void UseSkill(SkillSpawnData skillData, Vector3 casterPosition, Vector3 mousePosition, Transform target)
    {
        if (skillPrefab == null)
        {
            Debug.LogError("[SkillManager] SkillPrefab chưa được assign!");
            return;
        }

        // Chỉ local player spawn skill (để sync cho các client khác)
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            // Spawn qua PhotonNetwork
            GameObject obj = PhotonNetwork.Instantiate("Prefabs/Skill", casterPosition, Quaternion.identity);
            var skillSpawner = obj.GetComponent<SkillSpawnmer>();

            if (skillSpawner == null)
            {
                Debug.LogError("[SkillManager] SkillPrefab không có SkillSpawnmer component!");
                PhotonNetwork.Destroy(obj);
                return;
            }

            // Init skill (chỉ local player sẽ spawn, remote sẽ nhận qua RPC)
            skillSpawner.Init(skillData, casterPosition, mousePosition, target);
        }
        else
        {
            // Single player mode: instantiate thường
            var obj = Instantiate(skillPrefab, casterPosition, Quaternion.identity);
            var skillSpawner = obj.GetComponent<SkillSpawnmer>();

            if (skillSpawner == null)
            {
                Debug.LogError("[SkillManager] SkillPrefab không có SkillSpawnmer component!");
                Destroy(obj);
                return;
            }

            skillSpawner.Init(skillData, casterPosition, mousePosition, target);
        }
    }

    public SkillSpawnData GetSkillData(int skillSlot)
    {
        switch (skillSlot)
        {
            case 1: return skill1;
            case 2: return skill2;
            case 3: return skill3;
            default: return null;
        }
    }

    public float GetCooldownProgress(int skillSlot)
    {
        if (!skillCooldownTimers.ContainsKey(skillSlot) || !skillCooldowns.ContainsKey(skillSlot))
            return 0f;

        if (skillCooldowns[skillSlot] <= 0f)
            return 0f;

        return 1f - (skillCooldownTimers[skillSlot] / skillCooldowns[skillSlot]);
    }

    public bool IsSkillReady(int skillSlot)
    {
        if (!skillCooldownTimers.ContainsKey(skillSlot))
            return false;

        return skillCooldownTimers[skillSlot] <= 0f;
    }

    public float GetRemainingCooldown(int skillSlot)
    {
        if (!skillCooldownTimers.ContainsKey(skillSlot))
            return 0f;

        return skillCooldownTimers[skillSlot];
    }
}

