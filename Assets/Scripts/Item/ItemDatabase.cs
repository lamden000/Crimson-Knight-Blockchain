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
                _instance = Resources.Load<ItemDatabase>("Items/Database/Item Database");

                if (_instance == null)
                {
                    Debug.LogError("[ItemDatabase] Không tìm thấy ItemDatabase.asset trong Resources!");
                }
                else
                {
                    // Đảm bảo cache được build ngay khi load
                    _instance.BuildCache();
                }
            }
            return _instance;
        }
    }

    private void OnEnable()
    {
        BuildCache();
    }

    private void BuildCache()
    {
        if (cache == null)
            cache = new Dictionary<int, ItemData>();
        else
            cache.Clear();

        if (allItems == null)
        {
            Debug.LogWarning("[ItemDatabase] allItems is null!");
            return;
        }

        foreach (var item in allItems)
        {
            if (item != null)
            {
                cache[item.itemID] = item;
            }
        }
    }

    public ItemData GetItemByID(int id)
    {
        // Đảm bảo cache được build
        if (cache == null || cache.Count == 0)
        {
            BuildCache();
        }

        if (cache.TryGetValue(id, out var result))
            return result;

        Debug.LogWarning($"[ItemDatabase] Item ID {id} not found in cache. Total items in database: {allItems?.Count ?? 0}");
        return null;
    }
}
