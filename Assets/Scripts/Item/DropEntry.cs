using UnityEngine;

[System.Serializable]
public class DropEntry
{
    public ItemData item;
    [Range(0f, 1f)]
    public float dropRate;
}


