using UnityEngine;

public enum EquipmentSlot
{
    Head, Body, Legs, Feet
}

[CreateAssetMenu(menuName = "Game/Items/Equipment")]
public class EquipmentItem : ItemData
{
    public EquipmentSlot slot;
    public int defense;
    public int stylePoints; // đại loại bạn thích gì thì bỏ vô

    public override void Use()
    {
        // equip this item
    }
}


