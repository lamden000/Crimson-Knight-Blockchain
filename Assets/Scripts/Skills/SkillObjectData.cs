using System;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Skill/SkillObjectData")]
public class SkillObjectData : ScriptableObject
{
    [Header("Main Animation Frames")]
    public bool mainLoop = true;
    public bool autoDisableAfterMain = false;
    public Sprite[] mainFrames;
    public float mainFPS = 12f;

    [Header("Optional Sparkle Frame Animation")]
    public Sprite[] sparkleFrames;
    public float sparkleFPS = 12f;

    [Header("Aftermath Effect Frames")]
    public Sprite[] aftermathFrames;
    [Header("Leave play time 0 to play the animation only once")]
    public float aftermathPlayTime = 0;
    public bool aftermathLoop=false;
    public float aftermathFPS = 12f;
    public bool explosionRotatesWithMovement = true;

    [Header("Movement")]
    public float autoExplosionTime = 5f;
    public float speed = 100f;
    public float explosionDelay = 0f;

    [Header("Camera Shake")]
    public bool shakeCameraOnExplode = false; // Skill có làm camera rung khi explode không
    [Tooltip("Thời gian rung camera (giây)")]
    public float cameraShakeDuration = 0.2f;
    [Tooltip("Độ mạnh rung camera (càng lớn càng mạnh)")]
    public float cameraShakeMagnitude = 0.15f;

    public Vector3 scale = Vector3.one;
}
