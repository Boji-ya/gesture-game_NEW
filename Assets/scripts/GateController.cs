using UnityEngine;

public class GateController : MonoBehaviour
{
    [Header("大門")]
    public Transform gate;

    [Header("開門設定")]
    public float openHeight = 4f;
    public float openSpeed = 1.5f;

    private int fuseCount = 0;
    private bool leverPulled = false;
    private bool gateOpening = false;

    private Vector3 targetPosition;

    void Start()
    {
        targetPosition = gate.position + Vector3.up * openHeight;
    }

    void Update()
    {
        // 保險絲2根 + 拉桿拉下
        if (!gateOpening && fuseCount >= 2 && leverPulled)
        {
            gateOpening = true;
            Debug.Log("條件完成，大門開始上升！");
        }

        // 大門慢慢上升
        if (gateOpening)
        {
            gate.position = Vector3.MoveTowards(
                gate.position,
                targetPosition,
                openSpeed * Time.deltaTime
            );

            // 大門到達最高點
            if (gate.position == targetPosition)
            {
                gateOpening = false;
                Debug.Log("大門完全開啟！");
            }
        }
    }

    // 放入保險絲
    public void AddFuse()
    {
        fuseCount++;

        Debug.Log("目前保險絲：" + fuseCount + "/2");
    }

    // 拉下拉桿
    public void PullLever()
    {
        leverPulled = true;

        Debug.Log("電箱拉桿已拉下！");
    }
}