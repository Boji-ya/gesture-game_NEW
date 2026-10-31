using UnityEngine;

public class FreezerExit : MonoBehaviour
{
    public backpack playerBackpack;
    public GameObject uvLight;
    public TemperatureManager temperatureManager;

    private bool hasExited = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasExited)
            return;

        // 確認進入 Trigger 的是玩家
        if (!other.GetComponentInParent<FirstPersonMovement>())
            return;

        hasExited = true;

        if (temperatureManager != null)
        {
            temperatureManager.EscapeFreezer();
        }

        // 移除背包裡的 UV 手電筒
        if (playerBackpack != null)
        {
            playerBackpack.RemoveItemByName("flashlight");
        }

        // 關閉紫光
        if (uvLight != null)
        {
            uvLight.SetActive(false);
        }

        Debug.Log("已離開冷凍庫，UV Flashlight 已移除。");
    }
}