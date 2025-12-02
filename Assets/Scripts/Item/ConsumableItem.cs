using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Consumable")]
public class ConsumableItem : ItemData
{
    public int healAmount;

    public override void Use()
    {
        // heal player
    }
}


