using System.Collections;
using UnityEngine;

public class TurkeyMole : MonoBehaviour
{
    [Header("機台總控管理員")]
    public WhackAMoleManager manager;

    [Header("位置設定 (Local Y)")]
    public float downY = 3.5f;   // 藏在桌面下的高度
    public float upY = 10f;      // 冒出洞口的高度
    public float moveSpeed = 5f;  // 升降速度

    public bool IsUp { get; private set; } = false;
    public bool IsHittable { get; private set; } = false;

    private Coroutine popCoroutine;

    void Awake()
    {
        // 初始位置藏在洞裡
        Vector3 pos = transform.localPosition;
        pos.y = downY;
        transform.localPosition = pos;
    }

    /// <summary>機台呼叫地鼠冒出</summary>
    public void PopUp(float stayDuration)
    {
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(PopRoutine(stayDuration));
    }
    /// <summary>被槌子/手勢擊中</summary>
    public bool Hit()
    {
        if (!IsHittable) return false;

        IsHittable = false;
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(HitRoutine());
        return true;
    }
    private IEnumerator PopRoutine(float stayDuration)
    {
        IsUp = true;
        IsHittable = true;

        // 1. 冒出
        yield return MoveToY(upY);

        // 2. 停留等待玩家瞄準敲擊
        yield return new WaitForSeconds(stayDuration);

        // 3. 超時沒被打中，自行縮回
        IsHittable = false;
        yield return MoveToY(downY);
        IsUp = false;
    }

    /// <summary>玩家射線對準火雞比讚時呼叫</summary>
    public void OnHitByPlayer()
    {
        if (!IsHittable) return;

        IsHittable = false;
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(HitRoutine());

        // 通知機台總控加分
        if (manager != null)
        {
            manager.AddScore();
        }
    }

    private IEnumerator HitRoutine()
    {
        // 被打中後快速縮回地底
        yield return MoveToY(downY);
        IsUp = false;
    }

    private IEnumerator MoveToY(float targetY)
    {
        while (Mathf.Abs(transform.localPosition.y - targetY) > 0.01f)
        {
            Vector3 pos = transform.localPosition;
            pos.y = Mathf.MoveTowards(pos.y, targetY, moveSpeed * Time.deltaTime);
            transform.localPosition = pos;
            yield return null;
        }
        Vector3 finalPos = transform.localPosition;
        finalPos.y = targetY;
        transform.localPosition = finalPos;
    }
}