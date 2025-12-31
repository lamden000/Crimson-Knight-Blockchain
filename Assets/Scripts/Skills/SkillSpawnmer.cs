using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class SkillSpawnmer : MonoBehaviourPun
{
    public SkillSpawnData spawnData;

    private Vector3 casterPos;
    private Vector3 mousePos;
    private Transform target;
    private int targetViewID = -1; // PhotonView ID của target

    public GameObject skillObjectPrefab;
    public float skyHeight = 200f;
    public int lineSpawnSpacing = 100;

    public void Init(
        SkillSpawnData spData,
        Vector3 casterPosition,
        Vector3 mousePosition,
        Transform targetFollow = null)
    {
        spawnData = spData;

        casterPos = casterPosition;
        mousePos = mousePosition;
        target = targetFollow;

        // Lưu target ViewID để sync
        if (target != null)
        {
            PhotonView targetPV = target.GetComponent<PhotonView>();
            if (targetPV != null)
            {
                targetViewID = targetPV.ViewID;
            }
        }

        transform.position = casterPosition;

        // Chỉ local player spawn, sau đó sync cho các client khác
        if (photonView != null && photonView.IsMine)
        {
            if (spawnData != null)
                StartCoroutine(SpawnEntriesSequentially());
        }
        else if (photonView != null)
        {
            // Remote client: nhận spawn data từ RPC
            // Sẽ được gọi từ RPC_SyncSpawnData
        }
    }

    /// <summary>
    /// RPC để sync spawn data từ local player tới các client khác
    /// </summary>
    [PunRPC]
    void RPC_SyncSpawnData(Vector3 casterPos, Vector3 mousePos, int targetViewID, string spawnDataPath)
    {
        this.casterPos = casterPos;
        this.mousePos = mousePos;
        this.targetViewID = targetViewID;

        // Tìm target từ ViewID
        if (targetViewID >= 0)
        {
            PhotonView targetPV = PhotonView.Find(targetViewID);
            if (targetPV != null)
            {
                target = targetPV.transform;
            }
        }

        // Load spawn data từ Resources (giả sử có path)
        // Nếu spawnData là ScriptableObject, cần load từ Resources hoặc dùng reference
        // Tạm thời giả định spawnData đã được set trước đó hoặc cần cách khác để sync

        if (spawnData != null)
            StartCoroutine(SpawnEntriesSequentially());
    }


    // ===============================================================
    //                  1) CHAIN PER ENTRY
    // ===============================================================
    IEnumerator SpawnEntriesSequentially()
    {
        List<Coroutine> runningEntries = new List<Coroutine>();

        foreach (var entry in spawnData.spawnEntries)
        {
            if (entry.chainEntry)
            {
                // CHỜ entry này hoàn thành
                yield return StartCoroutine(SpawnEntryChain(entry));
            }
            else
            {
                // KHÔNG CHỜ – chạy song song
                Coroutine c = StartCoroutine(SpawnEntryChain(entry));
                runningEntries.Add(c);
            }
        }

        // CHỜ TẤT CẢ ENTRY SONG SONG HOÀN THÀNH
        foreach (var c in runningEntries)
            yield return c;

        Destroy(gameObject);
    }



    // ===============================================================
    //                  2) SPAWN ONE ENTRY
    // ===============================================================
    IEnumerator SpawnEntryChain(SpawnEntry entry)
    {
        Vector3 origin = GetOrigin(entry);

        // Nếu dùng burst
        if (entry.useBurst)
        {
            yield return StartCoroutine(SpawnWithBurst(entry, origin));
        }
        // Nếu dùng interval
        else if (entry.useInterval)
        {
            yield return StartCoroutine(SpawnWithInterval(entry, origin));
        }
        // Spawn tất cả ngay lập tức
        else
        {
            yield return StartCoroutine(SpawnAllAtOnce(entry, origin));
        }
    }
    IEnumerator SpawnAllAtOnce(SpawnEntry entry, Vector3 origin)
    {
        List<SkillObject> spawned = new List<SkillObject>();

        for (int i = 0; i < entry.count; i++)
        {
            Vector3 pos = CalculatePosition(i, entry, origin);
            bool explode = ShouldExplode(i, entry);

            var obj = CreateSkillInstance(entry.skillToSpawn, pos, explode, entry.movementType);
            spawned.Add(obj);
        }

        // chờ tất cả nổ
        yield return StartCoroutine(WaitForAllExplode(spawned));
    }
    IEnumerator SpawnWithInterval(SpawnEntry entry, Vector3 origin)
    {
        List<SkillObject> spawned = new List<SkillObject>();

        for (int i = 0; i < entry.count; i++)
        {
            Vector3 pos = CalculatePosition(i, entry, origin);
            bool explode = ShouldExplode(i, entry);

            var obj = CreateSkillInstance(entry.skillToSpawn, pos, explode, entry.movementType);
            spawned.Add(obj);

            yield return new WaitForSeconds(entry.interval);
        }

        yield return StartCoroutine(WaitForAllExplode(spawned));
    }
    IEnumerator SpawnWithBurst(SpawnEntry entry, Vector3 origin)
    {
        List<SkillObject> spawned = new List<SkillObject>();

        int spawnedCount = 0;

        while (spawnedCount < entry.count)
        {
            int amount = Mathf.Min(entry.burstSize, entry.count - spawnedCount);

            // spawn 1 burst
            for (int i = 0; i < amount; i++)
            {
                int index = spawnedCount + i;

                Vector3 pos = CalculatePosition(index, entry, origin);
                bool explode = ShouldExplode(index, entry);

                var obj = CreateSkillInstance(entry.skillToSpawn, pos, explode, entry.movementType);
                spawned.Add(obj);
            }

            spawnedCount += amount;

            // nếu còn burst tiếp theo → delay
            if (spawnedCount < entry.count)
                yield return new WaitForSeconds(entry.burstInterval);
        }

        yield return StartCoroutine(WaitForAllExplode(spawned));
    }


    IEnumerator WaitForAllExplode(List<SkillObject> objs)
    {
        HashSet<SkillObject> exploded = new HashSet<SkillObject>();

        foreach (var o in objs)
        {
            if (o.exploded)  
                exploded.Add(o);

            o.onExplode += (_) => exploded.Add(o);
        }

        // 3. Đợi đến khi exploded.Count == objs.Count
        yield return new WaitUntil(() => exploded.Count == objs.Count);
    }

    // ===============================================================
    //                EXPLOSION DECISION (CLEAN VERSION)
    // ===============================================================
    bool ShouldExplode(int index, SpawnEntry entry)
    {
        if (entry.allObjectsExplode) return true;
        return index == 0; // chỉ object đầu tiên được explode
    }


    // ===============================================================
    //                   PATTERN POSITION CALCULATION
    // ===============================================================
    Vector3 CalculatePosition(int index, SpawnEntry entry, Vector3 origin)
    {
        switch (entry.pattern)
        {
            case SpawnPattern.None:
                return origin;

            case SpawnPattern.Circle:
            case SpawnPattern.Radial:
                {
                    float step = 360f / entry.count;
                    float angle = (step * index + entry.angleOffset) * Mathf.Deg2Rad;

                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle),
                        Mathf.Sin(angle),
                        0f
                    ) * entry.radius;

                    return origin + offset;
                }

            case SpawnPattern.RandomCircle:
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float r = Mathf.Sqrt(Random.value) * entry.radius;

                    Vector3 offset = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0);
                    return origin + offset;
                }
            case SpawnPattern.Line:
                {
                    Vector3 dir = (target.position - origin).normalized;
                    dir=new Vector3(dir.x, dir.y, 0f);

                    return origin + dir * lineSpawnSpacing * (index+1);
                }


        }

        return origin;
    }


    // ===============================================================
    //                   GET SPAWN ORIGIN
    // ===============================================================
    Vector3 GetOrigin(SpawnEntry entry)
    {
        switch (entry.origin)
        {
            case SpawnOrigin.CasterPosition:
                return casterPos;

            case SpawnOrigin.TargetPosition:
                return target ? target.position : mousePos;

            case SpawnOrigin.MousePosition:
                return mousePos;

            default:
            case SpawnOrigin.Sky:
                return new Vector3(target.position.x, target.position.y + skyHeight, 0f);
        }
    }


    // ===============================================================
    //                   CREATE SKILL INSTANCE
    // ===============================================================
    SkillObject CreateSkillInstance(
     SkillObjectData data,
     Vector3 pos,
     bool isExplosive,
     SkillMovementType mtype,
     HashSet<SkillObject> exploded = null)
    {
        GameObject obj;
        
        // Chỉ spawn qua PhotonNetwork nếu đang trong multiplayer và là owner
        if (photonView != null && photonView.IsMine && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            // Tìm skill ID để pass qua InstantiationData
            int skillID = -1;
            if (SkillDatabase.Instance != null && data != null)
            {
                skillID = SkillDatabase.Instance.GetSkillID(data);
            }

            // Spawn qua PhotonNetwork với InstantiationData (skillID, targetViewID, casterPos, mousePos, isExplosive, movementType)
            object[] instantiationData = new object[]
            {
                skillID,
                targetViewID,
                casterPos,
                mousePos,
                isExplosive,
                (int)mtype
            };

            obj = PhotonNetwork.Instantiate("Prefabs/SkillObject", pos, Quaternion.identity, 0, instantiationData);
        }
        else
        {
            // Single player hoặc remote client: instantiate thường
            obj = Instantiate(skillObjectPrefab, pos, Quaternion.identity);
        }

        var sk = obj.GetComponent<SkillObject>();
        if (sk == null)
        {
            Debug.LogError("[SkillSpawnmer] SkillObject component not found!");
            if (photonView != null && photonView.IsMine && PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Destroy(obj);
            }
            else
            {
                Destroy(obj);
            }
            return null;
        }

        // REGISTER EVENT BEFORE PLAYING LOGIC
        if (exploded != null)
        {
            sk.onExplode += (_) =>
            {
                if (!exploded.Contains(sk))
                    exploded.Add(sk);
            };
        }

        // Tìm lại target từ ViewID nếu cần
        Transform targetTransform = target;
        if (targetTransform == null && targetViewID >= 0)
        {
            PhotonView targetPV = PhotonView.Find(targetViewID);
            if (targetPV != null)
            {
                targetTransform = targetPV.transform;
            }
        }

        // Chỉ gọi Init() nếu là owner hoặc không có PhotonView
        // Remote client sẽ nhận data qua OnPhotonInstantiate
        if (photonView == null || photonView.IsMine)
        {
            sk.Init(data, casterPos, mousePos, isExplosive, mtype, targetTransform);
        }
        
        return sk;
    }

}
