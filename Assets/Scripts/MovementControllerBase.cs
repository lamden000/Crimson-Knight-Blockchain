using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public abstract class MovementControllerBase : MonoBehaviourPun
{
    [Header("Movement (base)")]
    public float moveSpeed = 3f;

    protected Pathfinder pathfinder;
    protected List<Vector3> currentPath = new List<Vector3>();
    protected int pathIndex = 0;
    // desiredVelocity represents the movement velocity (units/sec) to be applied in FixedUpdate
    protected Vector2 desiredVelocity = Vector2.zero;
    // cached Rigidbody2D if present on the agent
    protected Rigidbody2D physicsRigidbody;
    public float arrivalDistance = 10f;
    protected BoxCollider2D boxCollider;

    protected virtual void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        pathfinder = Pathfinder.Instance;
        physicsRigidbody = GetComponent<Rigidbody2D>();
    }

    // Apply physics-based movement in fixed timestep so movement is framerate-independent.
    protected virtual void FixedUpdate()
    {
        if (physicsRigidbody != null)
        {
            // Move by velocity * fixedDeltaTime to keep behavior consistent with previous MovePosition usage
            Vector2 nextPos = physicsRigidbody.position + desiredVelocity * Time.fixedDeltaTime;
            physicsRigidbody.MovePosition(nextPos);
        }
        else
        {
            if (desiredVelocity != Vector2.zero)
            {
                Vector3 delta = (Vector3)(desiredVelocity * Time.fixedDeltaTime);
                transform.position = transform.position + delta;
            }
        }
    }

    protected bool EnsurePathfinder()
    {
        if (pathfinder == null)
            pathfinder = Pathfinder.Instance;
        return pathfinder != null;
    }

    // Tạo path từ vị trí hiện tại tới worldPos (trả về danh sách Vector3 world positions)
    protected virtual List<Vector3> BuildPathToWorld(Vector3 targetWorldPos)
    {
        if (!EnsurePathfinder()) return null;
        var startNode = pathfinder.GetTileFromWorld(transform.position);
        var endNode = pathfinder.GetTileFromWorld(targetWorldPos);
        if (startNode == null || endNode == null) return null;
        var agentSize = GetAgentSizeFromCollider(boxCollider);
        var nodePath = pathfinder.FindPath(startNode, endNode, agentSize);
        if (nodePath == null || nodePath.Count == 0) return null;
        return nodePath.Select(n => n.worldPos).ToList();
    }

    // Thiết lập currentPath từ danh sách TileNode (dùng khi pathfinder trả về nodes)
    protected virtual void SetCurrentPathFromNodes(List<TileNode> nodePath)
    {
        if (nodePath == null)
        {
            currentPath = null;
            pathIndex = 0;
            return;
        }
        currentPath = nodePath.Select(n => n.worldPos).ToList();
        pathIndex = 0;
    }

    protected Vector3 GetCurrentTargetWorldPos()
    {
        if (currentPath == null || currentPath.Count == 0)
            return transform.position;
        if (pathIndex >= currentPath.Count)
            return currentPath[currentPath.Count - 1];
        return currentPath[pathIndex];
    }

    protected bool AtCurrentNode(float threshold = 10f)
    {
        return Vector3.Distance(transform.position, GetCurrentTargetWorldPos()) < threshold;
    }

    protected void AdvanceNodeIfNeeded(float threshold = 10f)
    {
        if (currentPath == null) return;
        if (AtCurrentNode(threshold))
            pathIndex++;
    }

    // Child override để thực hiện di chuyển thực tế (rigidbody vs transform)
    protected virtual void MoveAlongPath()
    {
        // default no-op; child should override
    }

    protected Vector2 GetAgentSizeFromCollider(BoxCollider2D collider)
    {
        if (collider == null)
        {
            Debug.LogWarning("Agent collider is null, defaulting to (1,1)");
            return Vector2.one;
        }

        // Collider.size là kích thước local, nên cần nhân với scale để ra kích thước thật
        Vector3 lossyScale = collider.transform.lossyScale;
        float width = collider.size.x * Mathf.Abs(lossyScale.x);
        float height = collider.size.y * Mathf.Abs(lossyScale.y);

        return new Vector2(width, height);
    }

    // Reference to running follow coroutine so children can cancel if needed.
    protected Coroutine followCoroutine;

    // Generic coroutine that moves along currentPath until completion. If a dynamic stopTarget
    // and stopRange are provided, the coroutine will exit early when within stopRange of stopTarget.
    // Child classes must implement MoveAlongPath() to perform a single-step movement.
    protected IEnumerator FollowPath(float arrivalDistance, float stopRange, Transform stopTarget = null)
    {
        while (currentPath != null && pathIndex < currentPath.Count)
        {
            // if a dynamic target and range are provided, break when within range
            if (stopTarget != null && stopRange > 0f)
            {
                if (Vector3.Distance(transform.position, stopTarget.position) <= stopRange)
                {
                    yield break;
                }
            }

            if (Vector3.Distance(transform.position, GetCurrentTargetWorldPos()) < arrivalDistance)
            {
                pathIndex++;
                yield return null;
                continue;
            }

            MoveAlongPath();
            yield return null;
        }
    }

    // Start (and cancel previous) follow coroutine. Optional dynamic stop target/range supported.
    protected Coroutine StartFollow(float arrivalDistance, float stopRange, Transform stopTarget = null)
    {
        if (followCoroutine != null)
        {
            StopCoroutine(followCoroutine);
            followCoroutine = null;
        }
        followCoroutine = StartCoroutine(FollowPath(arrivalDistance, stopRange, stopTarget));
        return followCoroutine;
    }

    // Build path to a static world position and follow it until arrival.
    protected IEnumerator MoveToTarget(Vector3 targetWorldPos, float arrivalDistance, float stopRange)
    {
        if (!EnsurePathfinder()) yield break;

        var startNode = pathfinder.GetTileFromWorld(transform.position);
        var endNode = pathfinder.GetTileFromWorld(targetWorldPos);
        if (startNode == null || endNode == null) yield break;

        var nodePath = pathfinder.FindPath(startNode, endNode,GetAgentSizeFromCollider(boxCollider));
        if (nodePath == null || nodePath.Count == 0) yield break;

        // set current path and start following
        currentPath = nodePath.Select(n => n.worldPos).ToList();
        pathIndex = 0;

        // run follow coroutine and keep reference so callers can cancel/check it
        followCoroutine = StartCoroutine(FollowPath(arrivalDistance,stopRange));
        yield return followCoroutine;
        followCoroutine = null;
    }

    // Build path to a dynamic target (Transform) and follow it until within stopRange of the target.
    protected IEnumerator MoveToTarget(Transform target, float stopRange, float arrivalDistance = 10f)
    {
        if (target == null) yield break;
        if (!EnsurePathfinder()) yield break;

        var startNode = pathfinder.GetTileFromWorld(transform.position);
        var endNode = pathfinder.GetTileFromWorld(target.position);
        if (startNode == null || endNode == null) yield break;

        var agentSize = GetAgentSizeFromCollider(boxCollider);
        var nodePath = pathfinder.FindPath(startNode, endNode, agentSize);
        if (nodePath == null || nodePath.Count == 0) yield break;

        SetCurrentPathFromNodes(nodePath);
        pathIndex = 0;

        // run follow coroutine and keep reference so callers can cancel/check it
        followCoroutine = StartCoroutine(FollowPath(arrivalDistance, stopRange, target));
        yield return followCoroutine;
        followCoroutine = null;
    }

    protected void ClearPath()
    {
        currentPath?.Clear();
        pathIndex = 0;
        desiredVelocity = Vector2.zero;
    }
}