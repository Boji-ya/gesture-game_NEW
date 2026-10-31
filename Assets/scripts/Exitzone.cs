using UnityEngine;

/// <summary>
/// 出口區域：放在一個 isTrigger 的 Collider 上。
/// 玩家進入且鐵捲門已完全升起時，通知 FinalBossSequence 逃脫成功。
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExitZone : MonoBehaviour
{
    [SerializeField] Finalboss sequence;   // 留空會自動找

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        if (sequence == null)
            sequence = FindFirstObjectByType<Finalboss>();
    }

    // 用 Stay 而不是 Enter，避免玩家在鐵捲門升完前就站在區域內而漏掉
    void OnTriggerStay(Collider other)
    {
        if (sequence == null) return;
        if (other.GetComponentInParent<FirstPersonMovement>() != null)
            sequence.PlayerEscaped();
    }
}