using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StairClimb : MonoBehaviour
{
    [Header("Ray Origins")]
    [SerializeField] Transform stepRayLower;
    [SerializeField] Transform stepRayUpper;

    [Header("Step Settings")]
    [SerializeField] float stepHeight = 0.4f;
    [SerializeField] float climbSpeed = 5f;
    [SerializeField] float climbForwardDistance = 0.35f;
    [SerializeField] float rayDistance = 0.8f;

    [Header("Ground")]
    [SerializeField] LayerMask groundMask = ~0;

    Rigidbody rb;
    FirstPersonMovement movement;

    bool climbing;

    Vector3 climbStart;
    Vector3 climbDirection;

    float startY;
    float targetY;

    float climbProgress;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<FirstPersonMovement>();

        // Put the upper ray at step height
        if (stepRayUpper != null)
        {
            Vector3 p = stepRayUpper.localPosition;

            stepRayUpper.localPosition = new Vector3(
                p.x,
                stepHeight,
                p.z
            );
        }
    }

    void FixedUpdate()
    {
        if (climbing)
        {
            ContinueClimb();
            return;
        }

        CheckForStep();
    }

    // =========================================================
    // CHECK PLAYER INPUT
    // =========================================================

    void CheckForStep()
    {
        if (movement == null)
            return;

        float forward =
            movement.forwardInput +
            ((movement.gestureWalking ||
              movement.gestureRunning) ? 1f : 0f);

        float right = movement.rightInput;

        Vector3 input = new Vector3(
            right,
            0f,
            forward
        );

        if (input.sqrMagnitude < 0.01f)
            return;

        Vector3 direction =
            transform.TransformDirection(input).normalized;

        direction.y = 0f;
        direction.Normalize();

        // Try directly forward
        if (CheckStep(direction))
            return;

        // Try slightly left
        if (CheckStep(
            Quaternion.Euler(0f, 30f, 0f) * direction))
            return;

        // Try slightly right
        CheckStep(
            Quaternion.Euler(0f, -30f, 0f) * direction
        );
    }

    // =========================================================
    // DETECT STEP
    // =========================================================

    bool CheckStep(Vector3 direction)
    {
        if (stepRayLower == null ||
            stepRayUpper == null)
            return false;

        Vector3 lower =
            stepRayLower.position;

        Vector3 upper =
            stepRayUpper.position;

        // Debug rays
        Debug.DrawRay(
            lower,
            direction * rayDistance,
            Color.red
        );

        Debug.DrawRay(
            upper,
            direction * rayDistance,
            Color.green
        );

        // =====================================================
        // 1. LOWER RAY
        // =====================================================

        if (!Physics.Raycast(
            lower,
            direction,
            out RaycastHit lowerHit,
            rayDistance,
            groundMask,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        // Ignore slopes
        if (lowerHit.normal.y > 0.3f)
            return false;

        // =====================================================
        // 2. UPPER RAY
        // Must be clear
        // =====================================================

        if (Physics.Raycast(
            upper,
            direction,
            rayDistance,
            groundMask,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        // =====================================================
        // 3. FIND TOP OF STEP
        // =====================================================

        Vector3 topRayStart =
            lowerHit.point +
            direction * 0.05f;

        // Start above the maximum possible step
        topRayStart.y =
            lower.y +
            stepHeight +
            0.15f;

        Debug.DrawRay(
            topRayStart,
            Vector3.down * (stepHeight + 0.3f),
            Color.blue
        );

        if (!Physics.Raycast(
            topRayStart,
            Vector3.down,
            out RaycastHit topHit,
            stepHeight + 0.3f,
            groundMask,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        // =====================================================
        // 4. CALCULATE STEP HEIGHT
        // =====================================================

        float stepTopY = topHit.point.y;

        // The lower ray represents approximately the
        // player's foot level.
        float currentFootY = lower.y;

        float heightDifference =
            stepTopY - currentFootY;

        // Not a step
        if (heightDifference < 0.02f)
            return false;

        // Too high
        if (heightDifference > stepHeight)
            return false;

        // =====================================================
        // 5. START CLIMB
        // =====================================================

        climbing = true;

        climbProgress = 0f;

        climbStart = rb.position;

        climbDirection = direction;

        startY = rb.position.y;

        targetY =
            rb.position.y +
            heightDifference +
            0.02f;

        // Stop current physics velocity
        rb.velocity = Vector3.zero;

        // Temporarily stop normal movement
        if (movement != null)
            movement.enabled = false;

        return true;
    }

    // =========================================================
    // CLIMB
    // =========================================================

    void ContinueClimb()
    {
        climbProgress +=
            climbSpeed *
            Time.fixedDeltaTime;

        float t =
            Mathf.Clamp01(climbProgress);

        // Smooth movement
        float smoothT =
            Mathf.SmoothStep(
                0f,
                1f,
                t
            );

        // =====================================================
        // VERTICAL MOVEMENT
        // =====================================================

        float newY =
            Mathf.Lerp(
                startY,
                targetY,
                smoothT
            );

        // =====================================================
        // FORWARD MOVEMENT
        // =====================================================

        float forwardAmount =
            Mathf.Lerp(
                0f,
                climbForwardDistance,
                smoothT
            );

        Vector3 newPosition =
            climbStart +
            climbDirection * forwardAmount;

        newPosition.y = newY;

        // =====================================================
        // MOVE PLAYER
        // =====================================================

        rb.MovePosition(newPosition);

        // Prevent gravity from pulling us down
        rb.velocity = Vector3.zero;

        // =====================================================
        // FINISHED
        // =====================================================

        if (t >= 1f)
        {
            climbing = false;

            // Final position
            Vector3 finalPosition =
                rb.position;

            finalPosition.y = targetY;

            rb.position = finalPosition;

            // Stop any leftover physics
            rb.velocity = Vector3.zero;

            // Enable normal movement again
            if (movement != null)
                movement.enabled = true;
        }
    }
}
