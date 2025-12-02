using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    public PlayerData Data { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize(PlayerData initialData)
    {
        Data = initialData;
    }

    // === Ví dụ hàm set dữ liệu tự cập nhật ===

    public void AddGold(int amount)
    {
        Data.gold += amount;
        UpdateToDatabase("gold", Data.gold);
    }

    public void AddExp(int amount)
    {
        Data.exp += amount;
        UpdateToDatabase("exp", Data.exp);
    }

    public void LevelUp()
    {
        Data.level++;
        UpdateToDatabase("level", Data.level);
    }

    // === Hàm cập nhật lên database ===
    private void UpdateToDatabase(string field, object value)
    {
        Debug.Log($"Updating field '{field}' to '{value}' in database...");

        // gọi API / database thật
        // DatabaseAPI.UpdateField(Data.userId, field, value);
    }
}
