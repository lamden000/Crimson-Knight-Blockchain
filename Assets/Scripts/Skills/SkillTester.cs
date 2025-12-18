using UnityEngine;

public class SkillTester : MonoBehaviour
{
    [Header("Assign Data")]
    public SkillSpawnData spawnData;          // skill spawn config
    public GameObject skillPrefab;       // shared prefab chứa component Skill


    [Header("Tester Settings")]
    public Transform target;             // optional, cho thử spawn target-based
    public Camera cam;                   // camera để raycast mousepos
    public Transform caster;          // optional, vị trí caster (nếu không dùng chuột)


    void Update()
    {
        // Test bằng chuột trái
        if (Input.GetMouseButtonDown(0))
        {
            SpawnSkillAtMouse();
        }
    }


    void SpawnSkillAtMouse()
    {
        if (skillPrefab == null)
        {
            Debug.LogError("SkillPrefab chưa được assign!");
            return;
        }

        Vector3 mousePos = GetMouseWorldPosition();

        // Tạo skill cha
        var obj = Instantiate(skillPrefab, mousePos, Quaternion.identity);

        var skill = obj.GetComponent<SkillSpawnmer>();

        // Init skill cha (nó sẽ spawn children nếu spawnOnInit = true)
        skill.Init(
            spawnData,
            caster.position,   
            mousePos,      // mouse position
            target         // optional target
        );
    }


    Vector3 GetMouseWorldPosition()
    {
        if (cam == null)
            cam = Camera.main;

        Vector3 mouse = Input.mousePosition;
        mouse.z = 10f; // khoảng cách từ camera (tùy bạn chỉnh)
        return cam.ScreenToWorldPoint(mouse);
    }
}
