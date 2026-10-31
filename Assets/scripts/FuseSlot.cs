using UnityEngine;

public class FuseSlot : MonoBehaviour
{
    [Header("保險絲插入後顯示的模型")]
    public GameObject fuseModel;

    [Header("背包")]
    public backpack playerBackpack;

    private bool hasFuse = false;

    // 玩家嘗試放入保險絲
    public void InsertFuse()
    {
        // 已經有保險絲
        if (hasFuse)
        {
            Debug.Log("這個插槽已經有保險絲！");
            return;
        }

        // 沒有連接背包
        if (playerBackpack == null)
        {
            Debug.LogError("沒有指定 Backpack！");
            return;
        }

        // 檢查玩家目前選擇的物品
        string selectedItem = playerBackpack.GetSelectedItem();

        Debug.Log("目前選擇物品：" + selectedItem);

        // 不是保險絲
        if (selectedItem != "fuse")
        {
            Debug.Log("目前沒有選擇保險絲！");
            return;
        }

        // 扣掉背包裡的一根保險絲
        bool used = playerBackpack.UseSelectedItem();

        if (!used)
        {
            Debug.Log("無法使用保險絲！");
            return;
        }

        // 成功放入
        hasFuse = true;

        // 顯示電箱裡的保險絲
        if (fuseModel != null)
        {
            fuseModel.SetActive(true);
        }

    }
}