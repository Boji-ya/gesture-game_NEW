/*using UnityEngine;

public class FreezerDoorTrigger : MonoBehaviour
{
    public RotatingDoor door;

    private void OnTriggerEnter(Collider other)
    {
        FirstPersonMovement player =
            other.GetComponentInParent<FirstPersonMovement>();

        if (player == null)
            return;

        Debug.Log("玩家進入冷凍庫 Trigger！");

        door.PlayerEnterFreezer();
    }

    private void OnTriggerExit(Collider other)
    {
        FirstPersonMovement player =
            other.GetComponentInParent<FirstPersonMovement>();

        if (player == null)
            return;

        Debug.Log("玩家離開冷凍庫 Trigger！");

        door.PlayerExitFreezer();
    }
}*/

using UnityEngine;

public class FreezerDoorTriger : MonoBehaviour
{
    void Start()
    {
        Debug.Log(" FreezerDoorTrigger 腳本正在運作！");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger 被觸發！碰到：" + other.gameObject.name);
    }
}