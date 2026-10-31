using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// 火雞王：NavMesh 追擊、狂暴模式、閃光暈眩。
/// 需要場景有烘焙好的 NavMesh。
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class TurkeyKingBoss : MonoBehaviour
{
    [Header("目標")]
    [SerializeField] Transform player;   // 留空會自動找 FirstPersonMovement

    [Header("一般狀態")]
    [SerializeField] float normalSpeed = 3.5f;
    [SerializeField] float normalChaseRange = 12f;   // 進入這個距離開始追
    [SerializeField] float normalLoseRange = 18f;    // 超過這個距離放棄追擊
    [SerializeField] float normalStunDuration = 3f;  // 閃光暈眩 3 秒

    [Header("狂暴狀態")]
    [SerializeField] float rageSpeed = 7f;           // 玩家跑步 9、走路 3，所以 7 是「跑才逃得掉」
    [SerializeField] float rageAcceleration = 30f;
    [SerializeField] float rageChaseRange = 200f;    // 追擊範圍大幅增加（等於整張地圖）
    [SerializeField] float rageStunDuration = 1f;    // 閃光只暈 1 秒
    [SerializeField] float speedRampRate = 6f;       // 速度提升的平滑程度

    [Header("抓到玩家")]
    [SerializeField] float catchDistance = 1.6f;

    [Header("不推玩家")]
    [SerializeField] bool ignorePlayerCollision = true;   // 火雞王與玩家的碰撞體互相穿過，不會推人

    [Header("音效")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip rageRoar;

    [Header("事件（可接動畫/特效）")]
    public UnityEvent onRageStart;
    public UnityEvent onStunStart;
    public UnityEvent onStunEnd;
    [Header("手電筒挑戰失敗：撲擊")]
    [SerializeField] float lungeSpeed = 10f;
    [SerializeField] float lungeDuration = 1.5f;
    [SerializeField] float lungeCatchDistance = 2.5f;   // 撲擊時抓人範圍變大

    public UnityEvent onAttackStart;                    // 接攻擊動畫 / 吼叫

    float lungeTimer;

    /// <summary>火雞王抓到玩家時觸發</summary>
    public event Action OnCaughtPlayer;

    public bool IsRaged => isRaged;
    public bool IsStunned => isStunned;

    NavMeshAgent agent;
    FirstPersonMovement playerMovement;

    bool isActive = true;
    bool isRaged;
    bool isStunned;
    bool isChasing;
    float stunTimer;
    float repathTimer;
    float targetSpeed;

    const float RepathInterval = 0.2f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        playerMovement = FindFirstObjectByType<FirstPersonMovement>();
        if (player == null && playerMovement != null)
            player = playerMovement.transform;

        agent.stoppingDistance = 0f;
        agent.autoBraking = false;
        agent.speed = normalSpeed;
        targetSpeed = normalSpeed;

        // 不讓 NavMeshAgent 的避障把玩家擠開
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        // 如果火雞王有 Rigidbody，改成 kinematic，避免物理推力
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        if (ignorePlayerCollision)
            IgnorePlayerCollision();
    }

    void OnDisable()
    {
        // 避免玩家的 activeChaserCount 卡住不歸零
        if (isChasing && playerMovement != null)
            playerMovement.RegisterChaseEnd();
        isChasing = false;
    }

    void Update()
    {
        if (!isActive || player == null) return;

        // 撲擊狀態（手勢挑戰失敗時由 AttackPlayer() 啟動）
        bool lunging = lungeTimer > 0f;
        if (lunging) lungeTimer -= Time.deltaTime;

        // 速度：撲擊時直接衝刺，否則平滑過渡到目標速度
        if (lunging) agent.speed = lungeSpeed;
        else agent.speed = Mathf.MoveTowards(agent.speed, targetSpeed, speedRampRate * Time.deltaTime);

        // 暈眩中：倒數，不移動、不抓人
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) EndStun();
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        if (dist <= (lunging ? lungeCatchDistance : catchDistance))
        {
            Caught();
            return;
        }

        float chaseRange = isRaged ? rageChaseRange : normalChaseRange;
        float loseRange = isRaged ? float.PositiveInfinity : normalLoseRange;

        if (!isChasing && dist <= chaseRange) SetChasing(true);
        else if (isChasing && dist > loseRange) SetChasing(false);

        if (isChasing && agent.isOnNavMesh)
        {
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                repathTimer = RepathInterval;
                agent.SetDestination(player.position);
            }
        }
    }

    // ───────────── 公開方法 ─────────────

    /// <summary>進入狂暴：速度提升、追擊範圍增加、暈眩縮短、持續追擊</summary>
    public void EnterRage()
    {
        if (isRaged || !isActive) return;

        if (agent == null)
        {
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        }

        if (agent == null)
        {
            Debug.LogError(
                "TurkeyKingBoss: NavMeshAgent is missing!",
                this
            );
            return;
        }

        isRaged = true;

        targetSpeed = rageSpeed;
        agent.acceleration = rageAcceleration;

        if (audioSource != null && rageRoar != null)
        {
            audioSource.PlayOneShot(rageRoar);
        }

        if (onRageStart != null)
        {
            onRageStart.Invoke();
        }

        SetChasing(true);
    }

    public void PauseBossMovement(bool pause)
    {
        if (agent == null) return;

        if (pause)
        {
            agent.isStopped = true;

            // 立刻停下，不要因為慣性滑向玩家
            if (agent.isOnNavMesh)
                agent.velocity = Vector3.zero;
        }
        else if (!isStunned)
        {
            agent.isStopped = false;
        }
    }

    public void OnFlashed(float overrideDuration = -1f)
    {
        if (!isActive || isStunned) return;

        isStunned = true;
        lungeTimer = 0f;   // 被照到就取消撲擊
        stunTimer = overrideDuration > 0f
            ? overrideDuration
            : (isRaged ? rageStunDuration : normalStunDuration);

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        onStunStart.Invoke();
    }

    /// <summary>手勢挑戰失敗時呼叫：火雞王撲擊（短時間加速 + 抓人範圍變大）</summary>
    public void AttackPlayer()
    {
        if (!isActive || isStunned) return;
        lungeTimer = lungeDuration;
        SetChasing(true);
        onAttackStart.Invoke();
    }

    /// <summary>勝利/失敗後停止火雞王</summary>
    public void StopBoss()
    {
        isActive = false;
        isStunned = false;
        if (isChasing) SetChasing(false);
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    // ───────────── 內部 ─────────────

    /// <summary>讓火雞王和玩家的所有碰撞體互相忽略，這樣就不會推到玩家</summary>
    void IgnorePlayerCollision()
    {
        if (player == null) return;

        Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
        Collider[] bossColliders = GetComponentsInChildren<Collider>();

        foreach (Collider p in playerColliders)
        {
            foreach (Collider b in bossColliders)
            {
                if (p != null && b != null)
                    Physics.IgnoreCollision(p, b, true);
            }
        }
    }

    void EndStun()
    {
        isStunned = false;
        if (agent.isOnNavMesh) agent.isStopped = false;
        onStunEnd.Invoke();
    }

    void SetChasing(bool value)
    {
        if (isChasing == value) return;
        isChasing = value;

        if (playerMovement != null)
        {
            if (value) playerMovement.RegisterChaseStart();
            else playerMovement.RegisterChaseEnd();
        }

        if (!value && agent.isOnNavMesh) agent.ResetPath();
    }

    void Caught()
    {
        StopBoss();
        OnCaughtPlayer?.Invoke();
    }
}