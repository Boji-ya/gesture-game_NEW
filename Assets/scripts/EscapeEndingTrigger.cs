
using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeEndingTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // 確認碰到出口的是玩家
        if (!other.CompareTag("Player") &&
            !other.transform.root.CompareTag("Player"))
        {
            return;
        }

        hasTriggered = true;

        // 切換到通關畫面
        SceneManager.LoadScene("end");
    }
}
