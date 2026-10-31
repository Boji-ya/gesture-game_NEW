
using TMPro;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject objectivePanel;
    [SerializeField] private TMP_Text objectiveText;

    [Header("Initial Objective")]
    [TextArea(2, 4)]
    [SerializeField]
    private string initialObjective =
        "EXPLORE 1st floor\nAnd find props";

    private void Start()
    {
        ShowObjective(initialObjective);
    }

    public void ShowObjective(string message)
    {
        if (objectiveText == null)
        {
            Debug.LogWarning(
                "ObjectiveManager：尚未指定 ObjectiveText。"
            );
            return;
        }

        objectiveText.text = message;

        if (objectivePanel != null)
        {
            objectivePanel.SetActive(true);
        }

        Debug.Log("任務更新：" + message);
    }

    // 玩家進入冷凍庫時呼叫
    public void ObjectiveFindUVFlashlight()
    {
        ShowObjective(
            "Find UV flashlight\nThen escape"
        );
    }
    public void ObjectiveFindFuses()
    {
        ShowObjective("Find 2 fuses");
    }

    public void ObjectiveOpenGate()
    {
        ShowObjective("Open the gate");
    }
    public void HideObjective()
    {
        if (objectivePanel != null)
        {
            objectivePanel.SetActive(false);
        }
    }
}
