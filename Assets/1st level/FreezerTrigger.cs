
using UnityEngine;

public class FreezerTrigger : MonoBehaviour
{
    [Header("冷凍庫門")]
    public RotatingDoor door;

    [Header("溫度系統")]
    public TemperatureManager temperatureManager;

    [Header("玩家背包")]
    public backpack playerBackpack;

    [Header("紫光手電筒")]
    public GameObject uvLight;

    [Header("白光手電筒")]
    public GameObject whiteLight;

    // 玩家是否已進入冷凍庫
    private bool playerInsideFreezer = false;

    // 是否已經完成離開
    private bool hasExited = false;

    // 避免同一個玩家的多個 Collider 重複觸發
    private bool playerInTrigger = false;

    private void OnTriggerEnter(Collider other)
    {
        FirstPersonMovement player =
            other.GetComponentInParent<FirstPersonMovement>();

        if (player == null || playerInTrigger || hasExited)
            return;

        playerInTrigger = true;

        // 第一次進入：開始進入冷凍庫
        if (!playerInsideFreezer)
        {
            playerInsideFreezer = true;

            Debug.Log("玩家開始進入冷凍庫！");

            // 更新任務提示
            ObjectiveManager objectiveManager =
                FindFirstObjectByType<ObjectiveManager>();

            if (objectiveManager != null)
            {
                objectiveManager.ObjectiveFindUVFlashlight();
            }

            if (temperatureManager != null)
                temperatureManager.EnterFreezer();

            return;
        }

        // 已在冷凍庫內，且解謎完成：開始離開
        if (door != null && door.IsPuzzleSolved())
        {
            Debug.Log("解謎完成，玩家正在離開冷凍庫！");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        FirstPersonMovement player =
            other.GetComponentInParent<FirstPersonMovement>();

        if (player == null)
            return;

        playerInTrigger = false;

        if (hasExited || !playerInsideFreezer)
            return;

        // 尚未解謎：視為玩家已進入冷凍庫，關門
        if (door != null && !door.IsPuzzleSolved())
        {
            Debug.Log("玩家已進入冷凍庫，關閉冷凍庫門！");
            door.PlayerEnterFreezer();
            return;
        }

        // 解謎成功：玩家離開冷凍庫
        hasExited = true;
        playerInsideFreezer = false;

        // 更新任務文字：尋找 2 個保險絲
        ObjectiveManager objectiveManager =
            FindFirstObjectByType<ObjectiveManager>();

        if (objectiveManager != null)
        {
            objectiveManager.ObjectiveFindFuses();
        }

        if (temperatureManager != null)
            temperatureManager.EscapeFreezer();

        if (playerBackpack != null)
            playerBackpack.RemoveItemByName("flashlight");

        if (uvLight != null)
            uvLight.SetActive(false);

        if (door != null)
            door.PlayerExitFreezer();

        Debug.Log("已離開冷凍庫，UV Flashlight 已移除！");
        // 恢復白光
        if (whiteLight != null)
        {
            whiteLight.SetActive(true);
        }

        Debug.Log("已離開冷凍庫，已切回白光手電筒！");
    }
}
