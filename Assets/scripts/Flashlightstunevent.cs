using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 手電筒震懾事件（手勢操控版）：
/// 火雞王追擊期間，每隔一段時間觸發「手勢挑戰」(3~5 個手勢依序做出)。
/// 成功 → 手電筒照射動畫 → 火雞王暈眩
/// 失敗 / 超時 → 火雞王攻擊
///
/// 接法：你的手勢辨識系統每次偵測到手勢就呼叫
///     SetCurrentGesture("Fist")   // 手勢名稱
///     SetCurrentGesture("")       // 沒有手勢 / 手放下
/// 每幀呼叫或「手勢改變時」呼叫都可以。
/// 由 Finalboss 呼叫 BeginLoop() / EndLoop() 控制。
/// </summary>
public class FlashlightStunEvent : MonoBehaviour
{
    [Header("參考")]
    [SerializeField] TurkeyKingBoss boss;

    [Header("挑戰頻率")]
    [SerializeField] float firstDelay = 4f;
    [SerializeField] Vector2 intervalRange = new Vector2(5f, 8f);

    [Header("手勢設定")]
    [Tooltip("挑戰會從這些手勢中隨機抽取，名稱要和辨識系統回報的一致")]
    [SerializeField] string[] gesturePool = { "Fist", "OpenPalm", "Peace", "ThumbsUp", "Point" };
    [SerializeField, Range(3, 5)] int minGestures = 3;
    [SerializeField, Range(3, 5)] int maxGestures = 5;
    [SerializeField] float timePerGesture = 2.5f;   // 手勢辨識比按鍵慢，給多一點時間
    [SerializeField] float holdTime = 0.25f;        // 手勢需穩定維持多久才算數（防誤判）
    [SerializeField] bool wrongGestureFails = false; // 做錯手勢是否直接失敗（建議先關，容錯高）

    [Header("成功：手電筒動畫")]
    [SerializeField] Light flashlight;
    [SerializeField] Transform flashlightTransform;
    [SerializeField] Animator flashlightAnimator;
    [SerializeField] string flashlightTrigger = "Shine";
    [SerializeField] float shineDuration = 1.2f;
    [SerializeField] float shineIntensity = 8f;
    [SerializeField] float stunDuration = 4f;

    [Header("音效（可留空）")]
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip promptClip;
    [SerializeField] AudioClip successClip;
    [SerializeField] AudioClip failClip;

    [Header("事件（接 UI 等）")]
    public UnityEvent onChallengeStart;
    public UnityEvent onChallengeEnd;
    public UnityEvent onSuccess;
    public UnityEvent onFail;
    public UnityEvent<string> onPromptText;   // 整串序列（已完成的會變灰）
    public UnityEvent<string> onTargetGesture;// 目前要做的那一個手勢名稱
    public UnityEvent<int> onProgress;        // 已完成幾個

    public bool IsChallengeActive { get; private set; }

    Coroutine loopRoutine;
    float baseIntensity;
    bool baseEnabled;

    // 目前手勢狀態
    string currentGesture = "";
    float currentSince;

    void Awake()
    {
        if (flashlight != null)
        {
            baseIntensity = flashlight.intensity;
            baseEnabled = flashlight.enabled;
        }
    }

    // ───────────── 手勢輸入入口（給辨識系統呼叫） ─────────────

    public void SetCurrentGesture(string gestureName)
    {
        gestureName ??= "";
        if (gestureName == currentGesture) return;   // 沒變化就不重設計時
        currentGesture = gestureName;
        currentSince = Time.time;
    }

    bool IsHeld(string name) =>
        currentGesture == name && Time.time - currentSince >= holdTime;

    // ───────────── 外部控制 ─────────────

    public void BeginLoop()
    {
        if (loopRoutine != null) return;
        loopRoutine = StartCoroutine(Loop());
    }

    public void EndLoop()
    {
        if (loopRoutine != null) StopCoroutine(loopRoutine);
        loopRoutine = null;
        IsChallengeActive = false;
        RestoreFlashlight();
        onChallengeEnd.Invoke();
    }

    // ───────────── 主循環 ─────────────

    IEnumerator Loop()
    {
        yield return new WaitForSeconds(firstDelay);

        while (true)
        {
            yield return RunChallenge();
            yield return new WaitForSeconds(Random.Range(intervalRange.x, intervalRange.y));
        }
    }

    IEnumerator RunChallenge()
    {
        if (gesturePool == null || gesturePool.Length < 2) yield break;

        IsChallengeActive = true;
        onChallengeStart.Invoke();

        int count = Random.Range(minGestures, maxGestures + 1);
        string[] sequence = BuildSequence(count);

        onPromptText.Invoke(SequenceToText(sequence, 0));
        onProgress.Invoke(0);

        bool success = true;

        for (int i = 0; i < count; i++)
        {
            string target = sequence[i];
            onTargetGesture.Invoke(target);
            if (sfx != null && promptClip != null) sfx.PlayOneShot(promptClip);

            // 步驟開始時玩家可能還維持著上一個手勢，這個不算「做錯」
            string ignoreWrong = currentGesture;

            float t = 0f;
            bool gotCorrect = false;

            while (t < timePerGesture)
            {
                t += Time.deltaTime;

                if (IsHeld(target)) { gotCorrect = true; break; }

                if (wrongGestureFails
                    && currentGesture != ""
                    && currentGesture != ignoreWrong
                    && currentGesture != target
                    && Time.time - currentSince >= holdTime)
                {
                    break;   // 穩定做了錯的手勢
                }

                // 玩家換了手勢，之後就不再忽略
                if (currentGesture != ignoreWrong) ignoreWrong = "\0";

                yield return null;
            }

            if (!gotCorrect) { success = false; break; }

            onProgress.Invoke(i + 1);
            onPromptText.Invoke(SequenceToText(sequence, i + 1));

            // 等玩家換手勢或放下，避免同一個手勢連續算兩次
            // （序列保證相鄰不重複，所以這裡只要給一個短暫緩衝）
            yield return new WaitForSeconds(0.15f);
        }

        if (success) yield return SuccessRoutine();
        else FailRoutine();

        IsChallengeActive = false;
        onChallengeEnd.Invoke();
    }

    // 相鄰不重複；第一個也不等於玩家目前正在比的手勢
    string[] BuildSequence(int count)
    {
        string[] seq = new string[count];
        string prev = currentGesture;
        for (int i = 0; i < count; i++)
        {
            string pick;
            int guard = 0;
            do { pick = gesturePool[Random.Range(0, gesturePool.Length)]; }
            while (pick == prev && ++guard < 20);
            seq[i] = pick;
            prev = pick;
        }
        return seq;
    }

    // ───────────── 成功 / 失敗 ─────────────

    IEnumerator SuccessRoutine()
    {
        onSuccess.Invoke();
        if (sfx != null && successClip != null) sfx.PlayOneShot(successClip);

        if (flashlightAnimator != null && !string.IsNullOrEmpty(flashlightTrigger))
            flashlightAnimator.SetTrigger(flashlightTrigger);

        if (flashlight != null) flashlight.enabled = true;

        Quaternion startRot = flashlightTransform != null ? flashlightTransform.rotation : Quaternion.identity;
        Quaternion targetRot = startRot;
        if (flashlightTransform != null && boss != null)
        {
            Vector3 dir = boss.transform.position - flashlightTransform.position;
            if (dir.sqrMagnitude > 0.001f) targetRot = Quaternion.LookRotation(dir);
        }

        if (boss != null) boss.OnFlashed(stunDuration);

        float t = 0f;
        while (t < shineDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / shineDuration);

            if (flashlightTransform != null)
                flashlightTransform.rotation = Quaternion.Slerp(startRot, targetRot, Mathf.Min(1f, p * 3f));

            if (flashlight != null)
            {
                float flicker = Mathf.PerlinNoise(Time.time * 30f, 0f);
                flashlight.intensity = Mathf.Lerp(baseIntensity, shineIntensity, flicker);
            }
            yield return null;
        }

        if (flashlightTransform != null) flashlightTransform.rotation = startRot;
        RestoreFlashlight();
    }

    void FailRoutine()
    {
        onFail.Invoke();
        if (sfx != null && failClip != null) sfx.PlayOneShot(failClip);
        if (boss != null) boss.AttackPlayer();
    }

    void RestoreFlashlight()
    {
        if (flashlight == null) return;
        flashlight.intensity = baseIntensity;
        flashlight.enabled = baseEnabled;
    }

    // ───────────── UI 文字 ─────────────

    static string SequenceToText(string[] seq, int done)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < seq.Length; i++)
        {
            sb.Append(i < done ? $"<color=#888888>{seq[i]}</color>" : seq[i]);
            if (i < seq.Length - 1) sb.Append("  >  ");
        }
        return sb.ToString();
    }
}