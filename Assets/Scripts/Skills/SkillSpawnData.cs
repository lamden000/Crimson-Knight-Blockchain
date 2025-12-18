
using System.Collections.Generic;
using UnityEngine;

public enum SpawnPattern
{
    None,
    Radial,
    Fan,
    RandomCircle,
    Circle,
    Line
}

public enum SpawnOrigin
{
    Sky,  
    CasterPosition,
    TargetPosition,
    MousePosition
}
public enum SkillMovementType
{
    None, // đứng yên chờ nổ
    Projectile, // bay thẳng
    Homing, // bay theo target
    PersistentArea, //nổ sau khi main animation kết thúc
    Wave,   
    Orbit   
}

[CreateAssetMenu(menuName = "Skill/SkillSpawnData")]
public class SkillSpawnData : ScriptableObject
{
    public List<SpawnEntry> spawnEntries;
}

[System.Serializable]
public class SpawnEntry
{
    public SkillObjectData skillToSpawn;

    public bool chainEntry = false;       // entry phải chờ entry trước nổ hết?

    public SpawnPattern pattern = SpawnPattern.None;
    public int count = 1;
    public float radius = 1f;
    public float angleOffset = 0f;

    public SpawnOrigin origin = SpawnOrigin.CasterPosition;
    public bool allObjectsExplode = true;

    public SkillMovementType movementType= SkillMovementType.None;

    // ⭐ NEW: Burst & Interval
    public bool useBurst = false;
    public int burstSize = 1;
    public float burstInterval = 0f;

    public bool useInterval = false;
    public float interval = 0f;
}
