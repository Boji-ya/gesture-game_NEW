using UnityEngine;
public class HeadBob : MonoBehaviour
{
    [Header("Bob Settings")]
    public float walkBobSpeed = 10f;
    public float walkBobAmount = 0.05f;
    public float runBobSpeed = 16f;
    public float runBobAmount = 0.1f;
    [Header("References")]
    public FirstPersonMovement movement;
    private float defaultYPos;
    private float timer;
    void Start()
    {
        defaultYPos = transform.localPosition.y;
    }
    void Update()
    {
        bool isMoving = movement.gestureWalking || movement.gestureRunning;
        if (isMoving)
        {
            float speed = movement.gestureRunning ? runBobSpeed : walkBobSpeed;
            float amount = movement.gestureRunning ? runBobAmount : walkBobAmount;
            timer += Time.deltaTime * speed;
            float newY = defaultYPos + Mathf.Sin(timer) * amount;
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                newY,
                transform.localPosition.z
            );
        }
        else
        {
            // Smoothly return camera to default position when stopping
            timer = 0;
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                Mathf.Lerp(transform.localPosition.y, defaultYPos, Time.deltaTime * 6f),
                transform.localPosition.z
            );
        }
    }
}