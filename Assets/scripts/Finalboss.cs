using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// 最終戰鬥流程：
/// 修好電閘 → StartFinalBattle() → 鐵捲門緩慢上升(30秒) + 火雞王狂暴
/// → 鐵捲門升完 → 逃脫階段 → 進入 ExitZone 勝利 / 被抓到 Game Over
/// </summary>
public class Finalboss : MonoBehaviour
{
    public enum State { Idle, ShutterRising, Escape, Won, Lost }

    [Header("參考")]
    [SerializeField] TurkeyKingBoss boss;

    [Header("鐵捲門")]
    [SerializeField] Transform shutter;
    [SerializeField] Vector3 raiseOffset = new Vector3(0f, 3f, 0f);  // 往上升多少
    [SerializeField] float raiseDuration = 30f;
    [SerializeField] float shakeAmount = 0.015f;                     // 老舊故障的抖動

    [SerializeField] FlashlightStunEvent flashlightStun;

    [Header("鐵捲門音效")]
    [SerializeField] AudioSource shutterAudio;       // 3D 音源，建議放在鐵捲門上
    [SerializeField] AudioClip shutterGrindLoop;     // 巨大金屬摩擦聲（循環）
    [SerializeField] AudioClip shutterFinishClip;    // 升到頂的「哐」一聲（可留空）

    [Header("結束後換場景（可留空，改用下面的事件）")]
    [SerializeField] string winSceneName;
    [SerializeField] string gameOverSceneName;
    [SerializeField] float endDelay = 2f;

    [Header("事件（可接 UI、燈光、BGM）")]
    public UnityEvent onBattleStart;
    public UnityEvent onShutterOpened;
    public UnityEvent onWin;
    public UnityEvent onGameOver;
    public State CurrentState { get; private set; } = State.Idle;
    // DELETE AFTER TESTING!!!!!!!!!!!
    [Header("Testing")]
    [SerializeField] private bool startBattleOnPlay = false;

    void Start()
    {
        if (startBattleOnPlay)
        {
            StartFinalBattle();
        }
    }
    // DELETE AFTER TESTING!!!!!!!!!!!




    void OnEnable()
    {
        if (boss != null) boss.OnCaughtPlayer += HandleCaught;
    }

    void OnDisable()
    {
        if (boss != null) boss.OnCaughtPlayer -= HandleCaught;
    }

    // ───────────── 觸發入口 ─────────────

    /// <summary>玩家修好電閘時呼叫這個</summary>
    public void StartFinalBattle()
    {
        if (CurrentState != State.Idle) return;
        CurrentState = State.ShutterRising;

        if (onBattleStart != null)
        {
            onBattleStart.Invoke();
        }

        // 巨大聲響驚動火雞王 → 狂暴
        if (boss != null) boss.EnterRage();
        if (flashlightStun != null) flashlightStun.BeginLoop();
        StartCoroutine(RaiseShutter());
    }

    /// <summary>ExitZone 偵測到玩家進入時呼叫</summary>
    public void PlayerEscaped()
    {
        if (CurrentState != State.Escape) return;   // 鐵捲門沒升完不算成功
        CurrentState = State.Won;

        if (boss != null) boss.StopBoss();
        onWin.Invoke();
        StartCoroutine(LoadSceneAfterDelay(winSceneName));
        if (flashlightStun != null) flashlightStun.EndLoop();
    }

    // ───────────── 流程 ─────────────

    IEnumerator RaiseShutter()
    {
        Vector3 start = shutter.position;
        Vector3 end = start + raiseOffset;

        if (shutterAudio != null && shutterGrindLoop != null)
        {
            shutterAudio.clip = shutterGrindLoop;
            shutterAudio.loop = true;
            shutterAudio.Play();
        }

        float t = 0f;
        while (t < raiseDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / raiseDuration);

            Vector3 pos = Vector3.Lerp(start, end, progress);
            pos.x += Random.Range(-1f, 1f) * shakeAmount;   // 老舊故障的抖動
            pos.z += Random.Range(-1f, 1f) * shakeAmount;
            shutter.position = pos;

            yield return null;
        }

        shutter.position = end;

        if (shutterAudio != null)
        {
            shutterAudio.Stop();
            if (shutterFinishClip != null) shutterAudio.PlayOneShot(shutterFinishClip);
        }

        // 進入逃脫階段：火雞王持續追擊（狂暴狀態不會解除）
        CurrentState = State.Escape;
        onShutterOpened.Invoke();
    }

    void HandleCaught()
    {
        if (CurrentState == State.Won || CurrentState == State.Lost) return;
        CurrentState = State.Lost;

        StopAllCoroutines();
        if (shutterAudio != null) shutterAudio.Stop();

        onGameOver.Invoke();
        StartCoroutine(LoadSceneAfterDelay(gameOverSceneName));
        if (flashlightStun != null) flashlightStun.EndLoop();
    }

    IEnumerator LoadSceneAfterDelay(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) yield break;
        yield return new WaitForSecondsRealtime(endDelay);
        SceneManager.LoadScene(sceneName);
    }
}