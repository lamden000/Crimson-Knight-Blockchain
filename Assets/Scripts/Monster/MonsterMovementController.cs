using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;

public class MonsterMovementController : MovementControllerBase, IInRoomCallbacks
{
    [Header("Patrol Area Settings")]
    public int R = 5;
    public float waitTime = 1f;

    [Header("Combat Settings")]
    public float attackRange = 100f;
    public float attackCooldown = 2f;
    public float animationDelay = 0.2f;

    private Transform playerTarget;
    private bool isAttackOnCooldown = false;
    private Coroutine chaseCoroutine;
    private int chaseFailCount = 0;
    private int maxChaseFail = 3;

    private TileNode centerNode;
    private Monster enemy;
    private MonsterSync monsterSync;
    private int minX, maxX, minZ, maxZ;

    // dùng currentPath (List<Vector3>) và pathIndex từ base
    public bool isPatrolling = false;
    public bool isDebugging = false;

    private Coroutine patrolCoroutine;


    protected void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    protected void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    override protected void Start()
    {
        base.Start();

        monsterSync = GetComponent<MonsterSync>();
        enemy = GetComponent<Monster>();

        // If not master, do not initialize AI/pathfinder — client only interpolates
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (!EnsurePathfinder())
        {
            Debug.LogError("Pathfinder Instance chưa có. Kẻ địch không thể tuần tra.");
            enabled = false;
            return;
        }

        BecomeMasterControlled();
    }

    protected override void FixedUpdate()
    {
        // Movement & physics must be driven only by MasterClient
        if (!PhotonNetwork.IsMasterClient)
            return;

        base.FixedUpdate();

        // sync velocity so clients can interpolate
        if (monsterSync != null)
            monsterSync.SetVelocity(desiredVelocity);
    }

    void Update()
    {
        // AI Update only for MasterClient
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (isPatrolling && followCoroutine == null && currentPath != null && pathIndex < currentPath.Count)
        {
            MoveAlongPath();
        }
    }

    // --- AI control methods (only master should call these) ---
    public void StartChase(Transform target)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        StopPatrol();
        playerTarget = target;
        chaseFailCount = 0;
        if (isDebugging) Debug.Log($"[Chase] Bắt đầu truy đuổi mục tiêu: {target.name}");
        if (chaseCoroutine != null) StopCoroutine(chaseCoroutine);
        chaseCoroutine = StartCoroutine(ChaseRoutine());
    }

    public void HandleGetHit(Transform attacker)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        StopPatrol();
        if (chaseCoroutine != null) StopCoroutine(chaseCoroutine);

        if (isDebugging) Debug.Log("[GetHit] Kẻ địch bị đánh. Dừng mọi Coroutine di chuyển.");

        enemy.SetState(MonsterState.GetHit);

        StartCoroutine(GetHitDelay(attacker));
    }

    private IEnumerator GetHitDelay(Transform attacker)
    {
        if (!PhotonNetwork.IsMasterClient)
            yield break;

        if (isDebugging) Debug.Log($"[GetHitDelay] Bắt đầu chờ animation GetHit ({animationDelay}s).");
        yield return new WaitForSeconds(animationDelay);
        StopMoving();
        if (attacker != null)
        {
            if (isDebugging) Debug.Log("[GetHitDelay] Animation GetHit kết thúc. Bắt đầu CHASE.");
            StartChase(attacker);
        }
        else
        {
            if (isDebugging) Debug.Log("[GetHitDelay] Không có Attacker. Quay lại Patrol.");
            patrolCoroutine = StartCoroutine(PatrolRoutine());
        }
    }

    private IEnumerator ChaseRoutine()
    {
        if (!PhotonNetwork.IsMasterClient)
            yield break;

        if (isDebugging) Debug.Log("[ChaseRoutine] Bắt đầu vòng lặp truy đuổi.");

        while (playerTarget != null && enemy.currentState != MonsterState.GetHit)
        {
            float distance = Vector3.Distance(transform.position, playerTarget.position);

            if (distance <= attackRange && !isAttackOnCooldown)
            {
                if (isDebugging) Debug.Log($"[ChaseRoutine] Khoảng cách {distance:F2} <= AttackRange. Bắt đầu TẤN CÔNG.");
                yield return StartCoroutine(AttackAction());
            }
            else if (distance > attackRange)
            {
                if (isDebugging) Debug.Log($"[ChaseRoutine] Khoảng cách {distance:F2} > AttackRange. Bắt đầu DI CHUYỂN.");

                // Check whether a path to the dynamic target exists before attempting to move.
                var testPath = BuildPathToWorld(playerTarget.position);
                if (testPath == null || testPath.Count == 0)
                {
                    chaseFailCount++;
                    if (isDebugging) Debug.Log($"[ChaseRoutine] Không thể tìm đường tới mục tiêu (thất bại {chaseFailCount}/{maxChaseFail}).");
                    if (chaseFailCount >= maxChaseFail)
                    {
                        // Return to center and resume patrol
                        yield return StartCoroutine(ReturnToCenterAndResumePatrol());
                        yield break;
                    }
                    // wait a bit before retrying
                    yield return new WaitForSeconds(waitTime);
                    continue;
                }

                // valid path found — reset fail counter and proceed with movement
                chaseFailCount = 0;
                enemy.SetState(MonsterState.Walk);
                yield return StartCoroutine(MoveToTarget(playerTarget, attackRange, arrivalDistance));
                StopMoving();
            }
            else
            {
                if (isDebugging) Debug.Log("[ChaseRoutine] Đang chờ Cooldown. Trạng thái Idle.");
                StopMoving();
                yield return null;
            }
            yield return null;
        }
        if (isDebugging) Debug.Log("[ChaseRoutine] Vòng lặp truy đuổi kết thúc.");
    }

    private IEnumerator AttackAction()
    {
        if (!PhotonNetwork.IsMasterClient)
            yield break;

        if (isDebugging) Debug.Log("[AttackAction] Thiết lập Attack. Cooldown BẮT ĐẦU.");

        ClearPath();
        enemy.SetState(MonsterState.Attack);
        isAttackOnCooldown = true;

        FlipSprite(playerTarget.position.x > transform.position.x);

        yield return new WaitForSeconds(animationDelay);

        // Damage must be handled via RPCs to ensure authoritative updates (Master does actual damage).
        playerTarget.gameObject.GetComponent<Character>().TakeDamage(enemy.damage, gameObject);

        StopMoving();

        yield return new WaitForSeconds(attackCooldown - animationDelay);
        isAttackOnCooldown = false;
    }

    IEnumerator WaitForPathfinderInitialization()
    {
        // Ensure only master runs this initialization coroutine
        if (!PhotonNetwork.IsMasterClient)
            yield break;

        while (!Pathfinder.IsInitialized)
        {
            yield return null;
        }

        centerNode = pathfinder.GetTileFromWorld(transform.position);
        if (centerNode == null)
        {
            Debug.LogError("Không thể xác định TileNode tâm sau khi Pathfinder khởi tạo.");
            enabled = false;
            yield break;
        }

        CalculatePatrolBounds();
        patrolCoroutine = StartCoroutine(PatrolRoutine());
    }

    void CalculatePatrolBounds() { 
        int centerX = centerNode.gridPos.x; 
        int centerZ = centerNode.gridPos.y; minX = centerX;
        minZ = centerZ; maxX = centerX + R - 1; 
        maxZ = centerZ + R - 1; 
        if (isDebugging) 
            Debug.Log($". Center Node: {centerX},{centerZ};" +$" Khu vực tuần tra {R}x{R} đã thiết lập: X [{minX}, {maxX}], Z [{minZ}, {maxZ}]. Tổng số tile: {R * R}");
    }

    private IEnumerator ReturnToCenterAndResumePatrol()
    {
        if (!PhotonNetwork.IsMasterClient)
            yield break;

        if (isDebugging) Debug.Log("[Chase] Quá nhiều lần không tìm thấy đường. Quay trở lại tâm để tuần tra.");

        if (chaseCoroutine != null)
        {
            StopCoroutine(chaseCoroutine);
            chaseCoroutine = null;
        }

        playerTarget = null;
        ClearPath();
        StopMoving();

        if (centerNode != null)
        {
            // Move back to the spawn/center node, then resume patrol
            yield return StartCoroutine(MoveToTarget(centerNode.worldPos, arrivalDistance, 0f));
        }

        isPatrolling = true;
        patrolCoroutine = StartCoroutine(PatrolRoutine());
    }

    public void StopPatrol()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (!isPatrolling) return;

        isPatrolling = false;
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }
        GetComponent<Monster>().SetState(MonsterState.Idle);
        ClearPath();
        if (isDebugging) Debug.Log("[Patrol] Tuần tra đã bị ngắt.");
    }

    private TileNode debugEndNote;
    TileNode GetRandomPatrolNode() { 
        TileNode targetNode = null;
        int maxAttempts = 50; 
        int attempt = 0; 
        while (targetNode == null && attempt < maxAttempts){
            int randomX = Random.Range(minX, maxX + 1); 
            int randomZ = Random.Range(minZ, maxZ + 1); 
            TileNode checkNode = pathfinder.GetTile(randomZ, randomX); 
            if (checkNode != null && checkNode.walkable) { 
                if (checkNode != pathfinder.GetTileFromWorld(transform.position)) { 
                    targetNode = checkNode; 
                } 
            } attempt++; 
        } 
        if (attempt >= maxAttempts) 
            { if (isDebugging) Debug.LogWarning("Không tìm được node tuần tra ngẫu nhiên hợp lệ sau nhiều lần thử."); } 
        return targetNode; 
    }
    IEnumerator PatrolRoutine()
    {
        if (!PhotonNetwork.IsMasterClient)
            yield break;

        isPatrolling = true;
        int failCount = 0; // số lần liên tiếp không tìm được đường

        while (isPatrolling)
        {
            TileNode endNode = GetRandomPatrolNode();
            debugEndNote = endNode;

            if (endNode == null)
            {
                StopMoving();
                yield return new WaitForSeconds(waitTime);
                continue;
            }

            TileNode startNode = pathfinder.GetTileFromWorld(transform.position);

            if (startNode == null || startNode == endNode)
            {
                StopMoving();
                yield return new WaitForSeconds(waitTime);
                continue;
            }

            var nodePath = pathfinder.FindPath(startNode, endNode, GetAgentSizeFromCollider(boxCollider));
            SetCurrentPathFromNodes(nodePath);
            pathIndex = 0;

            if (currentPath == null || currentPath.Count <= 1)
            {
                failCount++;
                if (isDebugging)
                {
                    Debug.LogWarning($"[PatrolRoutine] Không tìm thấy đường ({failCount} lần liên tiếp) tới node {endNode.gridPos}.Start node walkable: {startNode.walkable}");
                }

                if (failCount >= 3)
                {
                    if (isDebugging)
                        Debug.LogWarning("[PatrolRoutine] Thất bại nhiều lần. Random lại node mới ngay lập tức.");

                    failCount = 0;
                    yield return null;
                    continue;
                }

                desiredVelocity = Vector3.zero;
                StopMoving();
                yield return new WaitForSeconds(waitTime);
                continue;
            }

            failCount = 0;
            if (isDebugging)
                Debug.Log($"Bắt đầu di chuyển ngẫu nhiên đến node: {endNode.gridPos}");

            enemy.SetState(MonsterState.Walk);

            followCoroutine = StartCoroutine(FollowPath(arrivalDistance, 0));
            yield return followCoroutine;
            followCoroutine = null;

            StopMoving();
            yield return new WaitForSeconds(waitTime);
        }
    }

    protected override void MoveAlongPath()
    {
        if (currentPath == null || pathIndex >= currentPath.Count)
        {
            desiredVelocity = Vector2.zero;
            return;
        }

        Vector3 targetWorldPos = GetCurrentTargetWorldPos();
        Vector3 direction = targetWorldPos - transform.position;

        if (direction.sqrMagnitude > 0.01f)
            desiredVelocity = new Vector2(direction.x, direction.y).normalized * moveSpeed;
        else
            desiredVelocity = Vector2.zero;

        if (Mathf.Abs(direction.x) > 0.01f)
        {
            FlipSprite(direction.x > 0);
        }
    }

    public void StopMoving()
    {
        desiredVelocity = Vector2.zero;
        enemy.SetState(MonsterState.Idle);
    }

    void FlipSprite(bool flip)
    {
        // Master flip local
        Vector3 scale = transform.localScale;
        float targetScaleX = flip ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        if (scale.x != targetScaleX)
        {
            scale.x = targetScaleX;
            transform.localScale = scale;

            // Notify all clients
            GetComponent<PhotonView>().RPC("RPC_Flip", RpcTarget.Others, flip);
        }
    }

    [PunRPC]
    void RPC_Flip(bool flip)
    {
        var scale = transform.localScale;
        scale.x = flip ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isDebugging)
            return;
        // Chỉ vẽ khi đang có đường đi hợp lệ
        if (currentPath != null && currentPath.Count > 0)
        {
            // Vẽ toàn bộ đường đi (màu vàng)
            Gizmos.color = Color.yellow;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Vector3 from = currentPath[i];
                Vector3 to = currentPath[i + 1];
                Gizmos.DrawLine(from, to);
            }

        }
        if (debugEndNote != null)
        {
            Vector3 targetPos = debugEndNote.worldPos;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetPos, 5f);
        }
    }

    // --- Photon callback: handle master switch ---
    public void OnMasterClientSwitched(Player newMaster)
    {
        // If this client becomes master, initialize AI.
        if (PhotonNetwork.IsMasterClient)
        {
            // Start initialization if not already running
            if (!EnsurePathfinder())
            {
                // Try to ensure pathfinder again; don't kill component here but log
                StartCoroutine(WaitForPathfinderInitialization());
            }
            else
            {
                // If pathfinder is ready, start patrol if not already
                if (patrolCoroutine == null && !isPatrolling)
                {
                    patrolCoroutine = StartCoroutine(PatrolRoutine());
                }
            }
        }
        else
        {
            // We lost master role — stop AI coroutines to prevent duplicate execution.
            Debug.Log($"Lost MasterClient role. Stopping AI on this client.");
            StopAllAICoroutinesAndReset();
        }
    }

    private void StopAllAICoroutinesAndReset()
    {
        // Stop only AI related coroutines
        if (patrolCoroutine != null) { StopCoroutine(patrolCoroutine); patrolCoroutine = null; }
        if (chaseCoroutine != null) { StopCoroutine(chaseCoroutine); chaseCoroutine = null; }
        if (followCoroutine != null) { StopCoroutine(followCoroutine); followCoroutine = null; }

        // Reset movement and AI flags
        isPatrolling = false;
        playerTarget = null;
        ClearPath();
        StopMoving();
    }
    public void BecomeMasterControlled()
    {
        StartCoroutine(WaitForPathfinderInitialization());
    }

    public void OnPlayerEnteredRoom(Player newPlayer) { }
    public void OnPlayerLeftRoom(Player otherPlayer) { }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
    }
}