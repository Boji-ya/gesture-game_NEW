using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Weeping-Angel style enemy.
/// Patrols, chases the player when it sees them, searches the last known position
/// when it loses them, and FREEZES whenever the player is looking at it.
///
/// SETUP
///  1. Bake a NavMesh in your scene (Window > AI > Navigation).
///  2. Put this script on the Angel (a NavMeshAgent is added automatically).
///  3. Give the Angel a Collider (so the player's look-raycast can hit it).
///  4. Tag your player "Player" (or drag it into the Player slot).
///  5. Drag the player's camera into Player Camera (falls back to Camera.main).
///  6. Set Is Active On This Floor to true, or call SetActiveOnFloor(true) from your floor manager.
///  7. Hook damage / jumpscare into the On Player Caught event.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class FallenAngelAI : MonoBehaviour
{
    // =========================================================
    // INSPECTOR
    // =========================================================

    [Header("References")]
    public Transform player;
    public Camera playerCamera;
    public NavMeshAgent agent;

    [Header("Movement")]
    public float chaseSpeed = 3.5f;
    public float patrolSpeed = 1.5f;
    public float searchSpeed = 2.0f;
    public float attackDistance = 1.2f;

    [Header("Vision (Angel sees player)")]
    public float detectionRange = 15f;
    [Range(0f, 360f)] public float fieldOfView = 120f;
    [Tooltip("Within this distance the Angel notices the player even outside its FOV (still needs line of sight).")]
    public float proximitySenseRange = 2.5f;
    public float eyeHeight = 1.5f;
    public float playerTargetHeight = 1.0f;
    [Tooltip("Layers that can block vision / line of sight.")]
    public LayerMask visionBlockers = ~0;

    [Header("Weeping Angel (player looks at Angel)")]
    [Range(0f, 1f)] public float lookThreshold = 0.7f;
    public float lookMaxDistance = 100f;
    [Tooltip("Height above the Angel's pivot that the player's gaze is aimed at.")]
    public float lookTargetHeight = 1.0f;
    public bool freezeWhenWatchedInSearch = true;
    public bool freezeWhenWatchedInPatrol = false;

    [Header("Chase")]
    [Tooltip("How often the chase destination is refreshed (seconds). Prevents path-recalc spam.")]
    public float repathInterval = 0.2f;
    [Tooltip("Seconds the Angel keeps running at the last seen spot before switching to Search.")]
    public float loseSightGrace = 1.0f;

    [Header("Search")]
    public float searchTime = 5f;
    [Tooltip("Degrees per second the Angel turns while looking around at the search spot.")]
    public float searchScanSpeed = 60f;

    [Header("Idle / Patrol")]
    public float patrolRadius = 10f;
    public float patrolWaitTime = 2f;

    [Header("Floor Control")]
    public bool isActiveOnThisFloor = false;

    [Header("Angel Avoidance")]
    public float separationRadius = 2.0f;
    [Tooltip("How far a chasing Angel offsets its target away from nearby Angels.")]
    public float separationStrength = 2.0f;
    [Tooltip("Agent radius. Increase this if Angels still overlap.")]
    public float agentRadius = 0.45f;

    [Header("Attack")]
    public float attackCooldown = 1.5f;
    [Tooltip("Auto-found on the Player if left empty. The Angel removes one heart when it catches the player.")]
    public PlayerHealth playerHealth;
    [Tooltip("Extra hook for jumpscare sound / screen effects.")]
    public UnityEvent onPlayerCaught;

    [Header("Debug")]
    public bool debugLogs = false;

    // =========================================================
    // PRIVATE STATE
    // =========================================================

    private enum State { Patrol, Chase, Search }
    private State currentState = State.Patrol;

    private Vector3 lastSeenPosition;
    private float timeSinceSeen;
    private float nextRepathTime;
    private float searchTimer;
    private float patrolWaitTimer;
    private float nextAttackTime;
    private bool wasActive;
    private Vector3 startPosition;
    private Quaternion startRotation;

    // Shared registry of all Angels (replaces per-frame OverlapSphere + GetComponent)
    private static readonly List<FallenAngelAI> allAngels = new List<FallenAngelAI>();

    // Shared raycast buffer (no per-frame allocation)
    private static readonly RaycastHit[] hitBuffer = new RaycastHit[32];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        allAngels.Clear();
    }

    // =========================================================
    // UNITY CALLBACKS
    // =========================================================

    private void OnEnable()
    {
        if (!allAngels.Contains(this))
            allAngels.Add(this);
    }

    private void OnDisable()
    {
        allAngels.Remove(this);
    }

    private void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("FallenAngelAI needs a NavMeshAgent!", this);
            enabled = false;
            return;
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (player == null)
            Debug.LogWarning("FallenAngelAI: no Player assigned and nothing tagged 'Player' found.", this);

        if (!agent.isOnNavMesh)
            Debug.LogWarning("FallenAngelAI: agent is not on a NavMesh. Bake one and place the Angel on it.", this);

        agent.speed = patrolSpeed;
        agent.radius = agentRadius;
        agent.updateRotation = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(20, 80); // different yielding decisions per Angel
        agent.autoRepath = true;

        // Remember where this Angel started so it can be reset on player respawn.
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = player.GetComponentInChildren<PlayerHealth>();
        }

        if (playerHealth == null)
            Debug.LogWarning("FallenAngelAI: no PlayerHealth found. Catching the player won't remove hearts.", this);
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        // ---------------- Floor control ----------------
        if (!isActiveOnThisFloor || player == null)
        {
            SetFrozen(true);
            wasActive = false;
            return;
        }

        if (!wasActive)
        {
            wasActive = true;
            ResetToPatrol();
        }

        // ---------------- Vision ----------------
        bool watched = IsPlayerLookingAtAngel();
        bool canSee = CanSeePlayer();

        // ---------------- State machine ----------------
        switch (currentState)
        {
            case State.Patrol: UpdatePatrol(watched, canSee); break;
            case State.Chase: UpdateChase(watched, canSee); break;
            case State.Search: UpdateSearch(watched, canSee); break;
        }

        // ---------------- Attack ----------------
        TryAttack(watched);
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    /// <summary>Call this from your floor manager to turn the Angel on/off.</summary>
    public void SetActiveOnFloor(bool active)
    {
        isActiveOnThisFloor = active;
        if (!active) wasActive = false; // next activation restarts from Patrol
    }

    /// <summary>Sends the Angel back to where it started and restarts it from Patrol. Called on player respawn.</summary>
    public void ResetAngel()
    {
        if (agent == null) return;

        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.Warp(startPosition);
        }
        else
        {
            transform.position = startPosition;
        }

        transform.rotation = startRotation;
        nextAttackTime = Time.time + attackCooldown;
        wasActive = false; // Update() will restart it from Patrol if it's active
    }

    // =========================================================
    // STATES
    // =========================================================

    private void UpdatePatrol(bool watched, bool canSee)
    {
        if (canSee)
        {
            EnterChase();
            return;
        }

        if (freezeWhenWatchedInPatrol && watched)
        {
            SetFrozen(true);
            return;
        }

        SetFrozen(false);
        agent.speed = patrolSpeed;

        if (HasReachedDestination())
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f)
                PickPatrolPoint();
        }
    }

    private void UpdateChase(bool watched, bool canSee)
    {
        // Track sight even while frozen - the Angel can still see you, it just can't move.
        if (canSee)
        {
            lastSeenPosition = player.position;
            timeSinceSeen = 0f;
        }
        else
        {
            timeSinceSeen += Time.deltaTime;
        }

        // The Weeping Angel rule: never move while watched.
        if (watched)
        {
            SetFrozen(true);
            return;
        }

        SetFrozen(false);
        agent.speed = chaseSpeed;

        // Only give up after a short grace period (a pillar for one frame shouldn't end the chase).
        if (!canSee && timeSinceSeen >= loseSightGrace)
        {
            EnterSearch();
            return;
        }

        // Throttled destination refresh.
        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + repathInterval;
            Vector3 dest = canSee ? GetSeparatedChasePosition() : lastSeenPosition;
            agent.SetDestination(dest);
        }
    }

    private void UpdateSearch(bool watched, bool canSee)
    {
        if (canSee)
        {
            EnterChase();
            return;
        }

        if (watched && freezeWhenWatchedInSearch)
        {
            SetFrozen(true);
            return;
        }

        SetFrozen(false);
        agent.speed = searchSpeed;

        if (HasReachedDestination())
        {
            // Look around while waiting at the last seen position.
            transform.Rotate(0f, searchScanSpeed * Time.deltaTime, 0f);

            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f)
            {
                ChangeState(State.Patrol);
                PickPatrolPoint();
            }
        }
    }

    // =========================================================
    // STATE TRANSITIONS
    // =========================================================

    private void ChangeState(State newState)
    {
        if (currentState == newState) return;

        if (debugLogs)
            Debug.Log($"{name}: {currentState} -> {newState}", this);

        currentState = newState;
    }

    private void ResetToPatrol()
    {
        ChangeState(State.Patrol);
        SetFrozen(false);
        agent.speed = patrolSpeed;
        PickPatrolPoint();
    }

    private void EnterChase()
    {
        ChangeState(State.Chase);
        lastSeenPosition = player.position;
        timeSinceSeen = 0f;
        nextRepathTime = 0f; // repath immediately next update
        SetFrozen(false);
        agent.speed = chaseSpeed;
    }

    private void EnterSearch()
    {
        ChangeState(State.Search);
        searchTimer = searchTime;
        agent.speed = searchSpeed;
        agent.SetDestination(lastSeenPosition);
    }

    // =========================================================
    // MOVEMENT HELPERS
    // =========================================================

    /// <summary>Freezes / unfreezes the agent. Freezing also kills residual velocity so it stops instantly.</summary>
    private void SetFrozen(bool frozen)
    {
        if (agent.isStopped == frozen) return;

        agent.isStopped = frozen;

        if (frozen)
            agent.velocity = Vector3.zero;
    }

    private bool HasReachedDestination()
    {
        if (agent.pathPending) return false;
        if (agent.remainingDistance > agent.stoppingDistance + 0.1f) return false;
        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.05f;
    }

    private void PickPatrolPoint()
    {
        patrolWaitTimer = patrolWaitTime;

        for (int i = 0; i < 6; i++)
        {
            Vector2 circle = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = transform.position + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                if (agent.SetDestination(hit.position))
                    return;
            }
        }

        // No valid point found - try again soon.
        patrolWaitTimer = 1f;
    }

    /// <summary>
    /// Chase target = player position, nudged away from nearby Angels so they spread out
    /// instead of stacking. Very close to the player the nudge is dropped so Angels can still reach attack range.
    /// </summary>
    private Vector3 GetSeparatedChasePosition()
    {
        Vector3 target = player.position;

        float closeRange = attackDistance * 2.5f;
        if ((target - transform.position).sqrMagnitude <= closeRange * closeRange)
            return SnapToNavMesh(target);

        Vector3 separation = Vector3.zero;
        int count = 0;
        float radiusSqr = separationRadius * separationRadius;

        for (int i = 0; i < allAngels.Count; i++)
        {
            FallenAngelAI other = allAngels[i];
            if (other == this || !other.isActiveOnThisFloor) continue;

            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float sqr = away.sqrMagnitude;

            if (sqr < 0.0001f || sqr > radiusSqr) continue;

            float dist = Mathf.Sqrt(sqr);
            float strength = 1f - Mathf.Clamp01(dist / separationRadius);
            separation += (away / dist) * strength;
            count++;
        }

        if (count > 0)
            target += (separation / count) * separationStrength;

        return SnapToNavMesh(target);
    }

    private Vector3 SnapToNavMesh(Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;

        return position;
    }

    // =========================================================
    // VISION
    // =========================================================

    private bool CanSeePlayer()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * playerTargetHeight;
        Vector3 toPlayer = target - eye;
        float distance = toPlayer.magnitude;

        if (distance > detectionRange) return false;
        if (distance < 0.01f) return true;

        // Outside FOV only counts if the player is very close.
        bool inProximity = distance <= proximitySenseRange;
        if (!inProximity && Vector3.Angle(transform.forward, toPlayer) > fieldOfView * 0.5f)
            return false;

        // Closest thing along the ray must be the player (walls block sight).
        if (!TryGetFirstHit(eye, toPlayer / distance, distance + 0.5f, transform, out RaycastHit hit))
            return false;

        return IsPlayerTransform(hit.transform);
    }

    private bool IsPlayerLookingAtAngel()
    {
        if (playerCamera == null) return false;

        Vector3 camPos = playerCamera.transform.position;
        Vector3 target = transform.position + Vector3.up * lookTargetHeight;
        Vector3 toAngel = target - camPos;
        float distance = toAngel.magnitude;

        if (distance > lookMaxDistance) return false;
        if (distance < 0.01f) return true;

        Vector3 dir = toAngel / distance;

        if (Vector3.Dot(playerCamera.transform.forward, dir) < lookThreshold)
            return false;

        // Closest thing between camera and Angel must be the Angel itself.
        if (!TryGetFirstHit(camPos, dir, distance + 0.5f, player, out RaycastHit hit))
            return false;

        return hit.transform.IsChildOf(transform);
    }

    private bool IsPlayerTransform(Transform t)
    {
        return t == player
            || t.IsChildOf(player)
            || t.CompareTag("Player")
            || t.root.CompareTag("Player");
    }

    /// <summary>
    /// Finds the closest collider hit by a ray, ignoring triggers and anything under 'ignore'.
    /// Uses a shared buffer so it doesn't allocate.
    /// </summary>
    private bool TryGetFirstHit(Vector3 origin, Vector3 direction, float maxDistance,
                                Transform ignore, out RaycastHit best)
    {
        int count = Physics.RaycastNonAlloc(
            origin, direction, hitBuffer, maxDistance,
            visionBlockers, QueryTriggerInteraction.Ignore);

        best = default;
        float bestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hitBuffer[i];

            if (ignore != null && h.transform.IsChildOf(ignore))
                continue;

            if (h.distance < bestDistance)
            {
                bestDistance = h.distance;
                best = h;
                found = true;
            }
        }

        return found;
    }

    // =========================================================
    // ATTACK
    // =========================================================

    private void TryAttack(bool watched)
    {
        // A watched Angel is frozen and harmless.
        if (watched) return;
        if (Time.time < nextAttackTime) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackDistance) return;

        // Player is invulnerable (just hit) or already dead - don't attack yet.
        if (playerHealth != null && !playerHealth.CanBeHit) return;

        nextAttackTime = Time.time + attackCooldown;

        if (playerHealth != null)
            playerHealth.TakeHit(1);

        if (debugLogs)
            Debug.Log($"{name}: caught the player!", this);

        onPlayerCaught?.Invoke();
    }

    // =========================================================
    // DEBUG GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Proximity sense
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, proximitySenseRange);

        // Separation
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        // Attack
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // FOV edges
        Gizmos.color = Color.green;
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 left = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * transform.forward;
        Gizmos.DrawRay(origin, left * detectionRange);
        Gizmos.DrawRay(origin, right * detectionRange);
    }
}
