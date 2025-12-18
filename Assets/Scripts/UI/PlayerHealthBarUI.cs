using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthFillImage; // Image component với Image Type = Filled
    
    private Character playerCharacter;
    private bool isInitialized = false;

    void Start()
    {
        // Tìm local player character
        FindLocalPlayer();
    }

    void Update()
    {
        // Nếu chưa tìm thấy player, thử tìm lại
        if (!isInitialized)
        {
            FindLocalPlayer();
        }
        
        // Nếu đã tìm thấy player, update health bar mỗi frame (để đảm bảo sync)
        if (isInitialized && playerCharacter != null && healthFillImage != null)
        {
            // Health sẽ được update qua OnHealthChanged, nhưng cũng có thể update trực tiếp ở đây
            // để đảm bảo UI luôn sync với Character
        }
    }

    private void FindLocalPlayer()
    {
        if (playerCharacter != null) return;

        // Tìm tất cả Character trong scene
        Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        
        foreach (Character character in characters)
        {
            Photon.Pun.PhotonView pv = character.GetComponent<Photon.Pun.PhotonView>();
            if (pv != null && pv.IsMine)
            {
                playerCharacter = character;
                isInitialized = true;
                
                // Subscribe to health updates
                UpdateHealthBar();
                
                Debug.Log("[PlayerHealthBarUI] Đã tìm thấy local player character");
                return;
            }
        }
    }

    public void UpdateHealthBar()
    {
        // Method này sẽ được gọi khi tìm thấy player
        // Health sẽ được update qua OnHealthChanged từ Character
        // Nên không cần làm gì ở đây, chỉ cần đảm bảo reference đã được set
    }

    // Method này sẽ được gọi từ Character khi health thay đổi
    public void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (healthFillImage == null) return;

        if (maxHealth > 0)
        {
            float fillAmount = (float)currentHealth / maxHealth;
            healthFillImage.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }

    // Public method để set reference trực tiếp (nếu cần)
    public void SetPlayerCharacter(Character character)
    {
        playerCharacter = character;
        isInitialized = character != null;
        UpdateHealthBar();
    }
}

