using System.Collections.Generic;
using UnityEngine;

public class FirstPersonMovement : MonoBehaviour
{
    #region

    public bool gestureRunning;
    public bool gestureWalking;
    public bool gestureTurnLeft;
    public bool gestureTurnRight;
    public float gestureTurnFactor = 0f;

    public float speed = 3;
    public float runSpeed = 9;
    public float turnSpeed = 60f;
    public float acceleration = 8f;
    public float turnAcceleration = 12f;

    [Header("Running")]
    public bool canRun = true;
    public bool IsRunning { get; private set; }

    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    #endregion


    #region 虛擬搖桿輸入

    [Header("Virtual Joystick")]
    public float forwardInput = 0f;

    public float rightInput = 0f;

    #endregion


    #region 轉視角倍率

    [Header("Combat Mode (Turning)")]
    [Tooltip("目前有幾隻鬼在追玩家。>0 時自動套用 combatTurnMultiplier。由鬼的 AI 腳本呼叫 RegisterChaseStart/RegisterChaseEnd 更新。")]
    public int activeChaserCount = 0;

    [Tooltip("應戰模式下的轉視角倍率，1 = 跟平常一樣，2 = 轉速兩倍。")]
    public float combatTurnMultiplier = 2.0f;

    public bool IsInCombat => activeChaserCount > 0;

    public void RegisterChaseStart()
    {
        activeChaserCount++;
    }

    public void RegisterChaseEnd()
    {
        activeChaserCount = Mathf.Max(0, activeChaserCount - 1);
    }

    #endregion


    #region 碰撞阻擋

    private Vector3 lastCollisionNormal = Vector3.zero;

    private bool isCollidingWithWall = false;

    void OnCollisionStay(Collision collision)
    {
        if (collision.contactCount > 0)
        {
            lastCollisionNormal = collision.GetContact(0).normal;
            isCollidingWithWall = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        isCollidingWithWall = false;
    }

    #endregion


    #region 私有狀態

    private float currentSpeed = 0f;
    private float currentTurnSpeed = 0f;
    private float currentForward = 0f;
    private float currentRight = 0f;
    private Rigidbody rb;

    #endregion


    void Awake()
    {

        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        UpdateForwardMovement();
        UpdateTurning();
        UpdateJoystickMovement();
        ApplyFinalVelocity();
    }


    #region FixedUpdate 拆成的小函式

    void UpdateForwardMovement()
    {
        IsRunning = canRun && gestureRunning;
        float targetMovingSpeed = (gestureWalking || gestureRunning)
            ? (IsRunning ? runSpeed : speed)
            : 0f;

        if (speedOverrides.Count > 0)
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();

        currentSpeed = Mathf.Lerp(currentSpeed, targetMovingSpeed, acceleration * Time.fixedDeltaTime);
    }

    void UpdateTurning()
    {
        float multiplier = IsInCombat ? combatTurnMultiplier : 1f;

        float targetTurn = 0f;

        if (gestureTurnLeft) targetTurn = -turnSpeed * multiplier;
        if (gestureTurnRight) targetTurn = turnSpeed * multiplier;

        currentTurnSpeed = Mathf.Lerp(currentTurnSpeed, targetTurn, turnAcceleration * Time.fixedDeltaTime);

        // 清除物理引擎自己累積的角速度，避免撞擊後殘留旋轉慣性
        rb.angularVelocity = Vector3.zero;

        Quaternion deltaRotation = Quaternion.Euler(0f, currentTurnSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    //搖桿操作:分成F(Forward)/R(Right)向量
    void UpdateJoystickMovement()
    {
        currentForward = Mathf.Lerp(currentForward, forwardInput, acceleration * Time.fixedDeltaTime);
        currentRight = Mathf.Lerp(currentRight, rightInput, acceleration * Time.fixedDeltaTime);
    }

    void ApplyFinalVelocity()
    {
        Vector3 localMove = new Vector3(
            currentRight * speed,
            0f,
            currentSpeed + currentForward * speed
        );

        Vector3 worldMove = rb.rotation * localMove;

        if (isCollidingWithWall && worldMove.sqrMagnitude > 0.0001f)
        {
            Vector3 moveDir = worldMove.normalized;
            float intoWall = Vector3.Dot(moveDir, -lastCollisionNormal);

            if (intoWall > 0f)
            {
                // intoWall 越接近 1（越正面撞），衰減越多；越接近 0（越側面擦），衰減越少
                // 用平方讓正面撞擊的衰減更明顯，側擦時手感更輕微
                float dampFactor = 1f - Mathf.Pow(Mathf.Clamp01(intoWall), 2f);
                worldMove *= dampFactor;
            }
        }

        // 用 Lerp 讓速度變化更平滑，避免瞬間從高速砍到低速的頓挫感
        Vector3 targetVelocity = new Vector3(worldMove.x, rb.velocity.y, worldMove.z);
        Vector3 smoothedVelocity = Vector3.Lerp(
            rb.velocity,
            targetVelocity,
            1f - Mathf.Exp(-15f * Time.fixedDeltaTime) // 數值越大，跟上目標速度越快；越小越平滑但反應變慢
        );
        rb.velocity = new Vector3(smoothedVelocity.x, rb.velocity.y, smoothedVelocity.z);

    }

    #endregion
}