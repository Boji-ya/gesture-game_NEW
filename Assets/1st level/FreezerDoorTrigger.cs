using UnityEngine;

public class FreezerDoorTrigger : MonoBehaviour
{
    public RotatingDoor door;
    public TemperatureManager temperatureManager;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("玩家進入冷凍庫！碰到：" + other.gameObject.name);

        FirstPersonMovement player =
            other.GetComponentInParent<FirstPersonMovement>();

        if (player != null)
        {
            Debug.Log("確認是玩家，關閉冷凍庫門！");

            if (door != null)
                door.PlayerEnterFreezer();

            if (temperatureManager != null)
                temperatureManager.EnterFreezer();
        }
    }
}