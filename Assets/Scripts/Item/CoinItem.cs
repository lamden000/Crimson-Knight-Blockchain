using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Coin")]
public class CoinItem : ItemData
{
    public int coinValue;

    public override void Use()
    {
        // add coins, etc.
    }
}


