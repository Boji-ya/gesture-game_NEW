using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lever_controller : MonoBehaviour
{
    public float pullAngle = 60f;     // 拉下去的角度
    public float speed = 5f;          // 拉動的滑順速度
    private bool isPulled = false;
    private Quaternion startRotation;
    private Quaternion targetRotation;

    public int myLeverID;               // 拉桿的編號 (0, 1, 2, 3)
    public FreezePuzzleManager manager; //拉桿管理器
    // Start is called before the first frame update
    void Start()
    {
        startRotation = transform.localRotation;
        // 計算拉下後的目標角度（沿 z軸轉動）
        targetRotation = Quaternion.Euler(0, 0, pullAngle) * startRotation;

        // 自動去場景裡抓！
        if (manager == null)
        {
            manager = FindObjectOfType<FreezePuzzleManager>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 透過 Lerp 讓拉桿平滑地轉動
        if (isPulled)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * speed);
        }
        else
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, startRotation, Time.deltaTime * speed);
        }
    }

    public void Pull()
    {
        isPulled = true;
    }

    // 當解謎順序錯誤、需要重置時呼叫此方法
    public void ResetLever()
    {
        isPulled = false;
    }
    //測試用的
    void OnMouseDown()
    {
        // 切換狀態（拉下 彈回）
        isPulled = !isPulled;
        Debug.Log("拉桿狀態切換：" + isPulled);
    }
    public void ToggleLever()
    {
        if (!isPulled)
        {
            isPulled = true;
            manager.AddInput(myLeverID); // 告訴manager拉下來了
        }
        else
        {
            isPulled = false;
            manager.RemoveInput(myLeverID); // 告訴manager推回去了
        }
    }
}
