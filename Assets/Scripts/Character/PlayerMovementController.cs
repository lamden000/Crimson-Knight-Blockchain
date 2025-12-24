using Assets.Scripts.Utils;
using NavMeshPlus.Components;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerAnimationController))]
public class PlayerMovementController : MovementControllerBase
{
    [Header("Movement Settings")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 0.8f;
    [Header("Target Settings")]
    [SerializeField] private float autoTargetRange = 3f; // Khoảng cách tự động chọn target
    [SerializeField] private float loseTargetRange = 5f; // Khoảng cách mất target
    private static long timeStartSendMove = 0;
    private Transform targetEnemy;
    private bool isMovingToEnemy = false;

    private bool isAttacking = false;
    private float attackTimer = 0f;
    private float attackCooldownTimer = 0f;

    private Rigidbody2D rb;
    private Vector2 moveAxisInput;
    private PlayerAnimationController anim;
    private float attackAnimDuration = 0.4f;
    private Vector2 moveInput;
    private bool isGettingHit = false;
    [Header("Interaction")]
    [SerializeField] private float npcInteractRange = 1.2f;
    private Coroutine npcTalkCoroutine;
    private Character character;

    protected override void Start()
    {
        base.Start();
        character = GetComponent<Character>();
    }

    private void Awake()
    {
        anim = GetComponent<PlayerAnimationController>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnMove(InputValue input)
    {
        if (!photonView.IsMine) return;
        moveInput = input.Get<Vector2>();
    }

    private void Update()
    {
        if (!photonView.IsMine) return;
        
        // Check if character is dead
        if (character != null && character.IsDead)
        {
            desiredVelocity = Vector2.zero;
            CancelAutoFollow();
            anim.SetAnimation(anim.GetCurrentDirection(), State.Idle);
            return;
        }
        
        if (!GameManager.Instance.CanPlayerMove)
        {
            desiredVelocity = Vector2.zero;
            CancelAutoFollow();
            if (npcTalkCoroutine != null)
            {
                StopCoroutine(npcTalkCoroutine);
                npcTalkCoroutine = null;
            }
            anim.SetAnimation(anim.GetCurrentDirection(), State.Idle);
            return;
        }

        if (isGettingHit)
        {
            desiredVelocity = Vector2.zero;
            return;
        }

        // Handle skill input (1, 2, 3)
        HandleSkillInput();

        // Auto-target logic: tự động chọn target khi đến gần hoặc mất target khi đi xa
        UpdateAutoTarget();

        moveAxisInput = new Vector2(moveInput.x, moveInput.y);

        UpdateAttackTimers();

        if (isMovingToEnemy && targetEnemy != null)
        {
            if (moveAxisInput != Vector2.zero)
            {
                CancelAutoFollow();
                ManualMove();
                return;
            }

            Monster enemy = targetEnemy.GetComponent<Monster>();
            if (enemy == null || enemy.IsDead)
            {
                CancelAutoFollow();
                return;
            }

            AutoMoveToEnemyPath();
            return;
        }

        if (npcTalkCoroutine != null)
        {
            if (moveAxisInput != Vector2.zero)
            {
                StopCoroutine(npcTalkCoroutine);
                npcTalkCoroutine = null;
                CancelAutoFollow();
                ManualMove();
                return;
            }
            return;
        }
        var mouse = Mouse.current;
        var touchScreen=Touchscreen.current;

        if (mouse != null && mouse.leftButton.wasPressedThisFrame ||
                    touchScreen != null && touchScreen.primaryTouch.press.wasPressedThisFrame)
        {
            Camera cam=Camera.main;
            Vector2 screenPos=Vector2.zero;
            if (touchScreen == null)
            {
                screenPos = Mouse.current.position.ReadValue();
            }
            else
            {
                screenPos= touchScreen.position.ReadValue();
            }
            Vector3 screenToWorld = cam.ScreenToWorldPoint(screenPos);
            Vector2 worldPos = new Vector2(screenToWorld.x, screenToWorld.y);
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("Enemy"))
                {
                    // Click để chọn target và tự động di chuyển đến
                    SetTargetAndMove(hit.transform);
                }
                else if (hit.collider.CompareTag("NPC"))
                {
                    var npc = hit.collider.GetComponent<NPCDialogueController>();
                    if (npc == null) return;
                    CancelAutoFollow();
                    if (npcTalkCoroutine != null)
                    {
                        StopCoroutine(npcTalkCoroutine);
                        npcTalkCoroutine = null;
                    }

                    Vector3 dirToNpc = (npc.transform.position - transform.position).normalized;
                    Direction dir;
                    if (Mathf.Abs(dirToNpc.x) > Mathf.Abs(dirToNpc.y))
                        dir = dirToNpc.x > 0 ? Direction.Right : Direction.Left;
                    else
                        dir = dirToNpc.y > 0 ? Direction.Up : Direction.Down;

                    anim.SetAnimation(dir, State.Walk);
                    npcTalkCoroutine = StartCoroutine(MoveToNPCAndTalk(npc));
                }
                return;
            }
        }

        ManualMove();
    }

    public void HandleGetHit()
    {
        if (isGettingHit) return;

        isGettingHit = true;
        CancelAutoFollow();
        anim.SetGetHitAnimation(true);
        rb.linearVelocity = Vector2.zero;

        StartCoroutine(GetHitDelayRoutine());
    }

    private IEnumerator GetHitDelayRoutine()
    {
        yield return new WaitForSeconds(attackAnimDuration);
        isGettingHit = false;
        anim.SetGetHitAnimation(false);
    }

    private void UpdateAttackTimers()
    {
        if (isAttacking)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackAnimDuration)
            {
                isAttacking = false;
                anim.SetAnimation(anim.GetCurrentDirection(), State.Idle);
            }
        }

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;
    }


    private void AutoMoveToEnemyPath()
    {
        if (targetEnemy == null)
        {
            Debug.LogWarning("[AutoMove] ❌ No target enemy!");
            return;
        }

        Vector3 dir = targetEnemy.position - transform.position;

        // Nếu trong tầm đánh
        if (dir.magnitude <= attackRange)
        {
            desiredVelocity = Vector2.zero;
            TryAttack(dir);
            return;
        }

        // Nếu chưa có path hoặc đã đến node hiện tại → tìm lại đường và bắt đầu coroutine follow
        if (currentPath == null || currentPath.Count == 0 || ReachedTargetTile() || pathfinder != null)
        {
            var startNode = pathfinder.GetTileFromWorld(transform.position);
            var endNode = pathfinder.GetTileFromWorld(targetEnemy.position);

            if (startNode == null || endNode == null)
            {
                Debug.LogWarning("[AutoMove] ❌ StartNode or EndNode is null!");
                return;
            }

            // pass the agent's BoxCollider2D size to pathfinder so obstacles are considered with agent's size
            var agentSize = GetAgentSizeFromCollider(boxCollider);
            var path = pathfinder.FindPath(startNode, endNode, agentSize);
            if (path == null || path.Count == 0)
            {
                return;
            }

            SetCurrentPathFromNodes(path);
            pathIndex = 0;

            // start base follow coroutine, but stop early when within attackRange of the enemy
            StartFollow(1f, attackRange, targetEnemy);
            return;
        }
    }


    override protected void MoveAlongPath()
    {
        if (currentPath == null || pathIndex >= currentPath.Count)
        {
            desiredVelocity = Vector2.zero;
            return;
        }

        Vector3 targetPos = currentPath[pathIndex];
        Vector3 dir = targetPos - transform.position;

        float dist = dir.magnitude;
        if (dist < 0.1f)
        {
            pathIndex++;
            return;
        }

        dir.Normalize();
        // only update desiredVelocity here; actual position change happens in FixedUpdate of base
        desiredVelocity = new Vector2(dir.x, dir.y) * moveSpeed;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            Direction direction= dir.x>0? Direction.Right : Direction.Left;
            anim.SetAnimation(direction, State.Walk);
        }
        else
        {
            if (dir.y > 0)
                anim.SetAnimation(Direction.Up, State.Walk);
            else
                anim.SetAnimation(Direction.Down, State.Walk);
        }
    }

    private bool ReachedTargetTile()
    {
        if (targetEnemy == null) return false;
        if (!EnsurePathfinder()) return false;

        var playerNode = pathfinder.GetTileFromWorld(transform.position);
        var enemyNode = pathfinder.GetTileFromWorld(targetEnemy.position);
        if (playerNode == null || enemyNode == null) return false;
        return playerNode.gridPos == enemyNode.gridPos;
    }

    private void TryAttack(Vector3 dir)
    {
        // Don't attack if dead
        if (character != null && character.IsDead) return;
        
        if (isAttacking || attackCooldownTimer > 0) return;

        isAttacking = true;
        attackTimer = 0f;
        attackCooldownTimer = attackCooldown;
        desiredVelocity = Vector2.zero;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            Direction direction = (dir.x > 0)
                ? Direction.Right
                : Direction.Left; ;
            anim.SetAnimation(direction, State.Attack);
        }
        else
        {
            if (dir.y > 0)
                anim.SetAnimation(Direction.Up, State.Attack);
            else
                anim.SetAnimation(Direction.Down, State.Attack);
        }
        targetEnemy.gameObject.GetComponent<Monster>().RequestDamage(character.damage, gameObject);
    }

    private void ManualMove()
    {
        float h = moveAxisInput.x;
        float v = moveAxisInput.y;
        bool moving = moveAxisInput != Vector2.zero;

        if (moving)
        {
            // manual input cancels any pending NPC interaction
            if (npcTalkCoroutine != null)
            {
                StopCoroutine(npcTalkCoroutine);
                npcTalkCoroutine = null;
            }
            if (Mathf.Abs(h) > Mathf.Abs(v))
            {
                desiredVelocity = new Vector2(h * moveSpeed, 0);
                Direction direction = (h > 0) ? Direction.Right: Direction.Left; 
                anim.SetAnimation(direction, State.Walk);
            }
            else
            {
                desiredVelocity = new Vector2(0, v * moveSpeed);
                if (v > 0)
                    anim.SetAnimation(Direction.Up, State.Walk);
                else
                    anim.SetAnimation(Direction.Down, State.Walk);
            }

            if (SystemUtil.CurrentTimeMillis() - timeStartSendMove > 2000)
            {
                timeStartSendMove = SystemUtil.CurrentTimeMillis();
            }
        }
        else
        {
            // Chỉ set idle nếu không đang attack (bao gồm cả skill attack)
            if (!isAttacking)
            {
                anim.SetAnimation(anim.GetCurrentDirection(), State.Idle);
            }
            desiredVelocity = Vector2.zero;
        }
    }

    private void CancelAutoFollow()
    {
        isMovingToEnemy = false;
        // Không clear targetEnemy ở đây, chỉ dừng auto-follow
        // Target sẽ được clear bởi UpdateAutoTarget() khi đi xa quá
        
        // stop any running follow coroutine from base
        if (followCoroutine != null)
        {
            StopCoroutine(followCoroutine);
            followCoroutine = null;
        }
        // stop any pending NPC interaction
        if (npcTalkCoroutine != null)
        {
            StopCoroutine(npcTalkCoroutine);
            npcTalkCoroutine = null;
        }
        ClearPath();
    }

    private IEnumerator MoveToNPCAndTalk(NPCDialogueController npc)
    {
        if (npc == null) yield break;

        if (Vector3.Distance(transform.position, npc.transform.position) <= npcInteractRange)
        {
            npc.StartDialogue();
            npcTalkCoroutine = null;
            yield break;
        }

        // use base MoveToTarget that stops when within npcInteractRange
        yield return StartCoroutine(MoveToTarget(npc.transform, npcInteractRange,arrivalDistance));

        // after arriving, ensure npc still exists then start dialogue
        if (npc != null)
        {
            npc.StartDialogue();
        }

        npcTalkCoroutine = null;
    }

    private void HandleSkillInput()
    {
        if (SkillManager.Instance == null) return;

        // Check for skill input (1, 2, 3)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            UseSkill(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            UseSkill(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            UseSkill(3);
        }
    }

    private void UseSkill(int skillSlot)
    {
        if (SkillManager.Instance == null) return;

        // Sử dụng target position thay vì mouse position
        Vector3 targetPosition = transform.position; // Fallback: vị trí player
        Transform target = null;

        if (targetEnemy != null)
        {
            Monster enemy = targetEnemy.GetComponent<Monster>();
            if (enemy != null && !enemy.IsDead)
            {
                target = targetEnemy;
                targetPosition = targetEnemy.position; // Dùng vị trí target
            }
        }

        // Try to use skill với target position
        bool skillUsed = SkillManager.Instance.TryUseSkill(skillSlot, transform.position, targetPosition, target);
        
        // Nếu skill được dùng thành công, set animation tấn công
        if (skillUsed)
        {
            PlayAttackAnimation(targetPosition);
        }
    }

    private void PlayAttackAnimation(Vector3 targetPos)
    {
        // Set flag để tránh animation bị override bởi idle
        isAttacking = true;
        attackTimer = 0f;
        desiredVelocity = Vector2.zero;

        Vector3 dir = targetPos - transform.position;
        
        // Set animation theo hướng target (giống như TryAttack)
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            Direction direction = (dir.x > 0)
                ? Direction.Right
                : Direction.Left;
            anim.SetAnimation(direction, State.Attack);
        }
        else
        {
            if (dir.y > 0)
                anim.SetAnimation(Direction.Up, State.Attack);
            else
                anim.SetAnimation(Direction.Down, State.Attack);
        }
    }

    private void UpdateAutoTarget()
    {
        // Kiểm tra target hiện tại
        if (targetEnemy != null)
        {
            Monster enemy = targetEnemy.GetComponent<Monster>();
            if (enemy == null || enemy.IsDead)
            {
                // Target đã chết hoặc không còn hợp lệ
                ClearTarget();
                return;
            }

            // Kiểm tra khoảng cách với target hiện tại
            float distance = Vector3.Distance(transform.position, targetEnemy.position);
            if (distance > loseTargetRange)
            {
                // Đi xa quá, mất target
                ClearTarget();
            }
        }
        else
        {
            // Không có target, tìm enemy gần nhất trong phạm vi
            Transform nearestEnemy = FindNearestEnemy(autoTargetRange);
            if (nearestEnemy != null)
            {
                SetTarget(nearestEnemy);
            }
        }
    }

    private Transform FindNearestEnemy(float maxRange)
    {
        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        // Tìm tất cả Monster trong scene
        Monster[] monsters = FindObjectsByType<Monster>(FindObjectsSortMode.None);
        
        foreach (Monster monster in monsters)
        {
            if (monster.IsDead) continue;

            float distance = Vector3.Distance(transform.position, monster.transform.position);
            if (distance <= maxRange && distance < nearestDistance)
            {
                nearest = monster.transform;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void SetTarget(Transform newTarget)
    {
        if (newTarget == null) return;

        Monster enemy = newTarget.GetComponent<Monster>();
        if (enemy == null || enemy.IsDead) return;

        targetEnemy = newTarget;
        // Chỉ set target, không tự động di chuyển
        // Tự động di chuyển chỉ khi người chơi click vào enemy
        isMovingToEnemy = false;
        ClearPath();

        // Update target indicator UI
        UpdateTargetIndicator();
    }

    private void SetTargetAndMove(Transform newTarget)
    {
        if (newTarget == null) return;

        Monster enemy = newTarget.GetComponent<Monster>();
        if (enemy == null || enemy.IsDead) return;

        targetEnemy = newTarget;
        // Click vào enemy thì tự động di chuyển đến
        isMovingToEnemy = true;
        ClearPath();

        // Update target indicator UI
        UpdateTargetIndicator();
    }

    private void ClearTarget()
    {
        targetEnemy = null;
        isMovingToEnemy = false;
        ClearPath();

        // Update target indicator UI
        UpdateTargetIndicator();
    }

    private void UpdateTargetIndicator()
    {
        TargetIndicatorUI indicator = FindAnyObjectByType<TargetIndicatorUI>();
        if (indicator != null)
        {
            indicator.SetTarget(targetEnemy);
        }
    }
}