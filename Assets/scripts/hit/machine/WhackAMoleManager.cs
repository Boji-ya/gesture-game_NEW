
using System.Collections;
using UnityEngine;
using TMPro;

public class WhackAMoleManager : MonoBehaviour
{
    [Header("5 隻火雞地鼠陣列")]
    public TurkeyMole[] turkeyMoles;

    [Header("機台看板 UI (World Space Text)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;

    [Header("音效設定")]
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip winSound;
    public AudioClip failSound;

    [Header("通關獎勵設定")]
    public backpack playerBackpack;
    public Sprite fuseIcon;
    public string fuseItemName = "fuse";

    [Header("通關提示 UI")]
    public TextMeshProUGUI resultText;

    [Header("提示文字設定")]
    public float resultTextDuration = 3f;

    private Coroutine resultTextCoroutine;

    [Header("遊戲參數")]
    public float gameDuration = 25f;
    public int targetScore = 6;
    public float minSpawnDelay = 0.6f;
    public float maxSpawnDelay = 1.4f;
    public float moleStayDuration = 1.5f;

    [Header("遊戲狀態")]
    public bool isGameActive = false;
    public int currentScore = 0;

    private float timeRemaining = 0f;

    // 防止同一局重複發放獎勵
    private bool rewardGiven = false;

    [ContextMenu("測試開始遊戲")]
    void Start()
    {
        isGameActive = false;

        if (resultText != null)
            resultText.text = "";
    }

    public void StartGame()
    {
        if (isGameActive) return;

        currentScore = 0;
        timeRemaining = gameDuration;
        isGameActive = true;
        rewardGiven = false;

        if (resultText != null)
            resultText.text = "";

        UpdateUI();

        StartCoroutine(GameTimerRoutine());
        StartCoroutine(SpawnLoopRoutine());
    }

    private IEnumerator GameTimerRoutine()
    {
        while (timeRemaining > 0 && isGameActive)
        {
            timeRemaining -= Time.deltaTime;

            UpdateUI();

            yield return null;
        }

        if (isGameActive)
        {
            // 時間到且未達標，遊戲失敗
            GameOver(false);
        }
    }

    private IEnumerator SpawnLoopRoutine()
    {
        while (isGameActive)
        {
            yield return new WaitForSeconds(
                Random.Range(minSpawnDelay, maxSpawnDelay)
            );

            if (!isGameActive)
                break;

            if (turkeyMoles == null || turkeyMoles.Length == 0)
                continue;

            // 隨機挑一隻目前沒有冒出的火雞
            int randomIndex = Random.Range(0, turkeyMoles.Length);

            if (turkeyMoles[randomIndex] != null &&
                !turkeyMoles[randomIndex].IsUp)
            {
                turkeyMoles[randomIndex].PopUp(moleStayDuration);
            }
        }
    }

    /// <summary>
    /// 火雞被打中時由 TurkeyMole 呼叫
    /// </summary>
    public void AddScore()
    {
        if (!isGameActive) return;

        currentScore++;

        UpdateUI();

        if (currentScore >= targetScore)
        {
            GameOver(true);
        }
    }

    public void HammerHit(int holeNumber)
    {
        if (!isGameActive) return;

        // 將洞口編號 1~N 轉為陣列索引 0~N-1
        int index = holeNumber - 1;

        if (turkeyMoles == null ||
            index < 0 ||
            index >= turkeyMoles.Length)
        {
            return;
        }

        if (turkeyMoles[index] == null)
            return;

        // 判定是否擊中火雞
        if (turkeyMoles[index].Hit())
        {
            // 播放擊中音效
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }

            AddScore();
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString("D3");
        }

        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(
                Mathf.Max(0, timeRemaining)
            ).ToString();
        }
    }
    private IEnumerator ClearResultTextAfterDelay()
    {
        yield return new WaitForSeconds(resultTextDuration);

        if (resultText != null)
        {
            resultText.text = "";
        }

        resultTextCoroutine = null;
    }
    private void ShowResultText(string message)
    {
        if (resultText == null) return;

        // 避免重複啟動倒數
        if (resultTextCoroutine != null)
        {
            StopCoroutine(resultTextCoroutine);
            resultTextCoroutine = null;
        }

        resultText.text = message;
        resultTextCoroutine = StartCoroutine(
            ClearResultTextAfterDelay()
        );
    }
    private void GameOver(bool isSuccess)
    {
        isGameActive = false;

        // 停止計時與火雞生成
        StopAllCoroutines();

        if (isSuccess)
        {
            // 播放勝利音效
            if (audioSource != null && winSound != null)
            {
                audioSource.PlayOneShot(winSound);
            }

            // 通關後直接發放保險絲
            GiveFuseReward();

            ObjectiveManager objectiveManager =
            FindFirstObjectByType<ObjectiveManager>();

            if (objectiveManager != null)
            {
                objectiveManager.ObjectiveOpenGate();
            }
        }
        else
        {
            // 播放失敗音效
            if (audioSource != null && failSound != null)
            {
                audioSource.PlayOneShot(failSound);
            }

            ShowResultText("GAME OVER!\nTry Again!");
        }
    }

    /// <summary>
    /// 通關後將保險絲直接加入玩家背包
    /// </summary>
    private void GiveFuseReward()
    {
        if (rewardGiven) return;

        rewardGiven = true;

        // 檢查是否有指定玩家背包
        if (playerBackpack == null)
        {
            Debug.LogError(
                "WhackAMoleManager：尚未指定玩家背包！"
            );

            if (resultText != null)
            {
                resultText.text =
                    "YOU WIN!\nBackpack not assigned!";
            }

            return;
        }

        // 使用背包原本的 AddItem 方法新增保險絲
        bool added = playerBackpack.AddItem(
            fuseItemName,
            fuseIcon
        );

        ShowResultText(
            added
                ? "YOU WIN!\nYou got a fuse!"
                : "YOU WIN!\nBackpack full!"
        );
        if (added)
        {
            Debug.Log(
                "打地鼠通關成央I保險絲已加入玩家背包。"
            );
        }
        else
        {
            Debug.LogWarning(
                "打地鼠通關成央A但無法將保險絲加入背包。"
            );
        }
    }
}
