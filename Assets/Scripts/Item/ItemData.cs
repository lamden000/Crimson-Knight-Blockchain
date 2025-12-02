using UnityEngine;

public enum ItemCategory
{
    Collectible,
    Equipment,
    Consumable,
    Quest,
    Trash
}

public abstract class ItemData : ScriptableObject
{
    public int itemID;
    public string itemName;
    public Sprite icon;
    public ItemCategory category;

    public abstract void Use(); // mỗi loại item tự định nghĩa
}


