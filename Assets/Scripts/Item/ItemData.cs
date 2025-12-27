using UnityEngine;

public enum ItemCategory
{
    Collectible,
    Equipment,
    Consumable,
    Quest,
    Trash
}

public enum Rarity
{
    Common,
    Limited,
    Rare,
    currency,
}

public abstract class ItemData : ScriptableObject
{
    public int itemID;
    public string itemName;
    [TextArea(3, 5)] // Cho phép nhập nhiều dòng
    public string description = "";
    public Sprite icon;
    public ItemCategory category;
    public Rarity rarity;

    public bool withdrawable=false;

    [Header("NFT (if withdrawable)")]
    public string nftContractAddress;
    public string metadataCID;


    public abstract void Use();
}


