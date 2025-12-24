using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SkillButtonUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage; // Icon của skill
    [SerializeField] private Image cooldownFillImage; // Fill image để hiển thị cooldown (mờ đi khi cooldown)

    [Header("Skill Settings")]
    [SerializeField] private int skillSlot = 1; // Slot 1, 2, hoặc 3

    private SkillManager skillManager;

    private void Start()
    {
        // Đợi một frame để đảm bảo SkillManager đã được khởi tạo
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        // Đợi cho đến khi SkillManager.Instance được set
        while (SkillManager.Instance == null)
        {
            yield return null;
        }

        skillManager = SkillManager.Instance;
        UpdateIcon();
    }

    private void Update()
    {
        // Nếu chưa có skillManager, thử tìm lại
        if (skillManager == null)
        {
            skillManager = SkillManager.Instance;
            if (skillManager != null)
            {
                UpdateIcon();
            }
            return;
        }

        UpdateCooldownDisplay();
    }

    private void UpdateIcon()
    {
        if (skillManager == null) return;

        SkillSpawnData skillData = skillManager.GetSkillData(skillSlot);
        if (skillData != null && skillData.icon != null && iconImage != null)
        {
            iconImage.sprite = skillData.icon;
        }
    }

    private void UpdateCooldownDisplay()
    {
        if (skillManager == null || cooldownFillImage == null) return;

        float progress = skillManager.GetCooldownProgress(skillSlot);
        
        // Fill amount: 0 = trống (cooldown hết), 1 = đầy (đang cooldown)
        // Đảo ngược: fill = 1 - progress
        cooldownFillImage.fillAmount = 1f - progress;

        // Có thể thêm hiệu ứng mờ đi khi đang cooldown
        if (iconImage != null)
        {
            Color iconColor = iconImage.color;
            iconColor.a = progress >= 1f ? 1f : 0.5f; // Mờ đi khi đang cooldown
            iconImage.color = iconColor;
        }
    }

    // Method để gọi từ button click (nếu muốn click button để dùng skill)
    public void OnButtonClick()
    {
        // Có thể thêm logic click button ở đây nếu cần
        // Nhưng hiện tại dùng phím 1, 2, 3
    }

    public void SetSkillSlot(int slot)
    {
        skillSlot = slot;
        UpdateIcon();
    }
}

