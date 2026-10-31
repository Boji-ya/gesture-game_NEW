using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class backpack : MonoBehaviour
{
    [Header("Backpack Slots")]
    public Image[] slotImages;

    [Header("Slot Icons")]
    public Image[] itemIcons;

    [Header("Stack Count Text")]
    public TMP_Text[] countTexts;

    [Header("Selection")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    private int selectedSlot = 1;

    // Stores what item is in each slot
    private string[] itemNames = new string[4];

    // Stores item amount in each slot
    private int[] itemCounts = new int[4];

    // Maximum stack for each slot
    private int[] maxStack = new int[4]
    {
        1,  // Slot 1：紫光
        5,  // Slot 2：電池
        2,  // Slot 3：保險絲
        1   // Slot 4：硬幣
    };


    void Start()
    {
        // Hide all item icons
        for (int i = 0; i < itemIcons.Length; i++)
        {
            if (itemIcons[i] != null)
            {
                itemIcons[i].gameObject.SetActive(false);
            }
        }

        // Hide all count texts
        for (int i = 0; i < countTexts.Length; i++)
        {
            if (countTexts[i] != null)
            {
                countTexts[i].gameObject.SetActive(false);
            }
        }

        UpdateSelection();
    }


    // =========================
    // 撿取物品
    // =========================
    public bool AddItem(string itemName, Sprite itemIcon)
    {
        int targetSlot = -1;

        // =========================
        // 決定物品要放哪個 Slot
        // =========================

        if (itemName == "flashlight")
        {
            // 紫光 → Slot 1
            targetSlot = 0;
        }
        else if (itemName == "battery")
        {
            // 電池 → Slot 2
            targetSlot = 1;
        }
        else if (itemName == "fuse")
        {
            // 保險絲 → Slot 3
            targetSlot = 2;
        }
        else if (itemName == "coin")
        {
            targetSlot = 3; // Slot 4
        }
        else
        {
            Debug.LogWarning("未定義的物品名稱：" + itemName);
            return false;
        }


        // =========================
        // 檢查這個 Slot 是否已經有物品
        // =========================

        if (!string.IsNullOrEmpty(itemNames[targetSlot]))
        {
            // 如果是同一種物品 → 嘗試堆疊
            if (itemNames[targetSlot] == itemName)
            {
                if (itemCounts[targetSlot] < maxStack[targetSlot])
                {
                    itemCounts[targetSlot]++;

                    UpdateSlotUI(targetSlot);

                    Debug.Log(
                        itemName +
                        " 增加，目前數量：" +
                        itemCounts[targetSlot]
                    );

                    return true;
                }
                else
                {
                    Debug.Log(
                        itemName +
                        " 已達最大堆疊數：" +
                        maxStack[targetSlot]
                    );

                    return false;
                }
            }

            // Slot 被其他物品佔用
            Debug.Log(
                "Slot " + (targetSlot + 1) +
                " 已經有其他物品！"
            );

            return false;
        }


        // =========================
        // Slot 是空的 → 放入物品
        // =========================

        itemNames[targetSlot] = itemName;
        itemCounts[targetSlot] = 1;

        if (targetSlot < itemIcons.Length &&
            itemIcons[targetSlot] != null)
        {
            itemIcons[targetSlot].sprite = itemIcon;
            itemIcons[targetSlot].gameObject.SetActive(true);
        }

        UpdateSlotUI(targetSlot);

        Debug.Log(
            "新增物品：" +
            itemName +
            " → Slot " +
            (targetSlot + 1)
        );

        return true;
    }


    // =========================
    // 更新某一個 Slot 的 UI
    // =========================
    void UpdateSlotUI(int slotIndex)
    {
        // 更新圖片
        if (slotIndex < itemIcons.Length &&
            itemIcons[slotIndex] != null)
        {
            itemIcons[slotIndex].sprite =
                GetItemIcon(slotIndex);

            itemIcons[slotIndex].gameObject.SetActive(true);
        }


        // 更新數量文字
        if (slotIndex < countTexts.Length &&
            countTexts[slotIndex] != null)
        {
            if (itemCounts[slotIndex] > 1)
            {
                countTexts[slotIndex].text =
                    "×" + itemCounts[slotIndex];

                countTexts[slotIndex].gameObject.SetActive(true);
            }
            else
            {
                // 數量只有 1 時不顯示
                countTexts[slotIndex].text = "";
                countTexts[slotIndex].gameObject.SetActive(false);
            }
        }
    }


    // 取得物品 Icon
    Sprite GetItemIcon(int slotIndex)
    {
        if (slotIndex < itemIcons.Length &&
            itemIcons[slotIndex] != null)
        {
            return itemIcons[slotIndex].sprite;
        }

        return null;
    }


    // =========================
    // 選擇 Slot
    // =========================
    public void SelectSlot(int slot)
    {
        if (slot < 1 || slot > 4)
            return;

        selectedSlot = slot;

        UpdateSelection();
    }


    void UpdateSelection()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] == null)
                continue;

            if (i + 1 == selectedSlot)
                slotImages[i].color = selectedColor;
            else
                slotImages[i].color = normalColor;
        }
    }


    // =========================
    // 取得目前選中的物品
    // =========================
    public string GetSelectedItem()
    {
        if (selectedSlot < 1 ||
            selectedSlot > itemNames.Length)
        {
            return "";
        }

        return itemNames[selectedSlot - 1];
    }


    // =========================
    // 取得目前選中的物品數量
    // =========================
    public int GetSelectedItemCount()
    {
        if (selectedSlot < 1 ||
            selectedSlot > itemCounts.Length)
        {
            return 0;
        }

        return itemCounts[selectedSlot - 1];
    }


    // =========================
    // 消耗目前選中的物品 1 個
    // =========================
    public bool UseSelectedItem()
    {
        int index = selectedSlot - 1;

        if (index < 0 || index >= itemCounts.Length)
            return false;

        if (itemCounts[index] <= 0)
            return false;

        itemCounts[index]--;

        Debug.Log(
            "使用物品：" +
            itemNames[index] +
            "，剩餘：" +
            itemCounts[index]
        );

        // 如果用完了
        if (itemCounts[index] <= 0)
        {
            itemNames[index] = "";

            if (itemIcons[index] != null)
            {
                itemIcons[index].sprite = null;
                itemIcons[index].gameObject.SetActive(false);
            }

            if (countTexts[index] != null)
            {
                countTexts[index].text = "";
                countTexts[index].gameObject.SetActive(false);
            }
        }
        else
        {
            UpdateSlotUI(index);
        }

        return true;
    }


    // =========================
    // 移除指定物品
    // =========================
    public void RemoveItemByName(string itemName)
    {
        for (int i = 0; i < itemNames.Length; i++)
        {
            if (itemNames[i] == itemName)
            {
                itemNames[i] = "";
                itemCounts[i] = 0;

                if (i < itemIcons.Length &&
                    itemIcons[i] != null)
                {
                    itemIcons[i].sprite = null;
                    itemIcons[i].gameObject.SetActive(false);
                }

                if (i < countTexts.Length &&
                    countTexts[i] != null)
                {
                    countTexts[i].text = "";
                    countTexts[i].gameObject.SetActive(false);
                }

                // 如果刪掉的是目前選中的 Slot
                if (selectedSlot == i + 1)
                {
                    selectedSlot = 1;
                    UpdateSelection();
                }

                Debug.Log(
                    "Removed item from backpack: " +
                    itemName
                );

                return;
            }
        }

        Debug.Log(
            "Item not found in backpack: " +
            itemName
        );
    }
    // =========================
    // 取得指定物品的數量
    // =========================
    public int GetItemCount(string itemName)
    {
        for (int i = 0; i < itemNames.Length; i++)
        {
            if (itemNames[i] == itemName)
            {
                return itemCounts[i];
            }
        }

        return 0;
    }

    // =========================
    // 消耗指定物品 1 個，不改變目前選中的 Slot
    // =========================
    public bool UseItemByName(string itemName)
    {
        for (int i = 0; i < itemNames.Length; i++)
        {
            if (itemNames[i] == itemName && itemCounts[i] > 0)
            {
                itemCounts[i]--;

                Debug.Log(
                    "使用物品：" + itemName +
                    "，剩餘：" + itemCounts[i]
                );

                // 物品用完了
                if (itemCounts[i] <= 0)
                {
                    itemNames[i] = "";

                    if (i < itemIcons.Length && itemIcons[i] != null)
                    {
                        itemIcons[i].sprite = null;
                        itemIcons[i].gameObject.SetActive(false);
                    }

                    if (i < countTexts.Length && countTexts[i] != null)
                    {
                        countTexts[i].text = "";
                        countTexts[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    UpdateSlotUI(i);
                }

                return true;
            }
        }

        Debug.Log("背包沒有可使用的物品：" + itemName);
        return false;
    }


    public bool HasItem(string itemName)
    {
        return GetItemCount(itemName) > 0;
    }

}