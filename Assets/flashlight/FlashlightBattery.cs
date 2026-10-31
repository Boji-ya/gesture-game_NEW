using UnityEngine;
using UnityEngine.UI;

public class FlashlightBattery : MonoBehaviour
{
    [Header("電量設定")]
    public int maxBattery = 3;
    public int currentBattery = 3;

    // 每隔幾秒消耗一格
    public float drainInterval = 60f;

    private float timer = 0f;

    [Header("手電筒")]
    public Light flashlightLight;

    [Header("電量 UI")]
    public Image[] batteryIcons;


    void Start()
    {
        // 遊戲開始時滿電 3 格
        currentBattery = maxBattery;

        // 更新 UI
        UpdateBatteryUI();

        // 一開始開啟白光手電筒
        if (flashlightLight != null)
        {
            flashlightLight.enabled = true;
        }
    }


    void Update()
    {
        // 已經沒電
        if (currentBattery <= 0)
        {
            if (flashlightLight != null)
            {
                flashlightLight.enabled = false;
            }

            return;
        }

        // 累積時間
        timer += Time.deltaTime;

        // 到達消耗時間
        if (timer >= drainInterval)
        {
            timer = 0f;

            // 消耗一格
            currentBattery--;

            Debug.Log("手電筒電量剩下：" + currentBattery);

            // 更新 UI
            UpdateBatteryUI();

            // 沒電了
            if (currentBattery <= 0)
            {
                currentBattery = 0;

                if (flashlightLight != null)
                {
                    flashlightLight.enabled = false;
                }

                Debug.Log("手電筒沒電了！");
            }
        }
    }


    public void AddBattery()
    {
        TryAddBattery();
    }

    public bool TryAddBattery()
    {
        if (currentBattery >= maxBattery)
        {
            Debug.Log("手電筒電量已滿！");
            return false;
        }

        currentBattery = Mathf.Min(maxBattery, currentBattery + 1);

        // 補電後重新計算下一次消耗時間
        timer = 0f;

        UpdateBatteryUI();

        if (currentBattery > 0 && flashlightLight != null)
            flashlightLight.enabled = true;

        Debug.Log("補充電池，目前電量：" + currentBattery);
        return true;
    }


    // 更新左下角電量 UI
    void UpdateBatteryUI()
    {
        for (int i = 0; i < batteryIcons.Length; i++)
        {
            if (batteryIcons[i] == null)
                continue;

            // 目前有幾格，就顯示幾格
            if (i < currentBattery)
            {
                batteryIcons[i].gameObject.SetActive(true);
            }
            else
            {
                batteryIcons[i].gameObject.SetActive(false);
            }
        }
    }
}