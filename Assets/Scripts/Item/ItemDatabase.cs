using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    private static ItemDatabase _instance;
    public List<ItemData> allItems;
    private Dictionary<int, ItemData> cache;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                // Load từ Resources
                _instance = Resources.Load<ItemDatabase>("ItemDatabase");

                if (_instance == null)
                {
                    Debug.LogError("[ItemDatabase] Không tìm thấy ItemDatabase.asset trong Resources!");
                }
            }
            return _instance;
        }
    }

    private void OnEnable()
    {
        cache = new Dictionary<int, ItemData>();
        foreach (var item in allItems)
            cache[item.itemID] = item;
    }

    public ItemData GetItemByID(int id)
    {
        if (cache.TryGetValue(id, out var result))
            return result;

        Debug.LogWarning($"Item ID {id} not found");
        return null;
    }
}
