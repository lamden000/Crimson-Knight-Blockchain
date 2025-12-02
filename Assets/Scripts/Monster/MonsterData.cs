using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Monster")]
public class MonsterData : ScriptableObject
{
    public int maxHP;
    public int damage;
    public float moveSpeed;

    public List<DropEntry> dropTable; 
}
