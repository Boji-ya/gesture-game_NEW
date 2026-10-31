using UnityEngine;

public class GateLeverController : MonoBehaviour
{
    [Header("拉桿設定")]
    public float pullAngle = 60f;
    public float speed = 5f;

    [Header("大門控制")]
    public GateController gateController;

    private bool isPulled = false;

    private Quaternion startRotation;
    private Quaternion targetRotation;

    void Start()
    {
        startRotation = transform.localRotation;

        targetRotation =
            Quaternion.Euler(0, 0, pullAngle) * startRotation;
    }

    void Update()
    {
        if (isPulled)
        {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * speed
            );
        }
        else
        {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                startRotation,
                Time.deltaTime * speed
            );
        }
    }

    public void ToggleLever()
    {
        if (isPulled)
            return;

        isPulled = true;

        Debug.Log("大門拉桿已拉下！");

        if (gateController != null)
        {
            gateController.PullLever();
        }
        else
        {
            Debug.LogWarning("GateController 沒有指定！");
        }
    }
}