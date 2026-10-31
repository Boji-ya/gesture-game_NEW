using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class TurkeyKingQTE : MonoBehaviour
{
    [System.Serializable]
    public class GestureOption
    {
        public string command;
        public string displayName;
    }

    [Header("Boss and Player")]
    public Transform player;
    public Transform boss;
    public MonoBehaviour bossChaseScript;
    public TurkeyKingBoss turkeyKingBoss;
    public MonoBehaviour[] playerControlsToPause;

    [Header("QTE Settings")]
    public float triggerDistance = 3f;
    [Min(1)] public int requiredGestures = 5;
    public float qteTimeLimit = 7f;
    public float qteCooldown = 1f;
    public float gestureDebounce = 0.35f;
    public float sameGestureCooldown = 0.5f;

    [Header("Available Gestures")]
    public GestureOption[] gestures =
    {
        new GestureOption { command = "SLOT 1", displayName = "SLOT 1" },
        new GestureOption { command = "SLOT 2", displayName = "SLOT 2" },
        new GestureOption { command = "SLOT 3", displayName = "SLOT 3" },
        new GestureOption { command = "SLOT 4", displayName = "SLOT 4" },
        new GestureOption { command = "TURN STOP", displayName = "FIST" },
        new GestureOption { command = "UV", displayName = "OK GESTURE" },
        new GestureOption { command = "GRAB", displayName = "THUMBS UP" },
    };

    [Header("Boss Fight")]
    public float escapeDuration = 30f;
    public float stunDuration = 3f;
    public float failureCooldown = 1f;

    [Header("Flashlight")]
    public Transform playerBody;
    public Light flashlight;
    public float flashlightDuration = 0.7f;

    [Header("UI")]
    public TMP_Text qteText;
    public TMP_Text progressText;
    public TMP_Text timerText;

    [Header("Events")]
    public UnityEvent onBossAttack;
    public UnityEvent onEscapeComplete;

    public bool IsQTEActive => qteActive;
    private readonly List<GestureOption> sequence =
        new List<GestureOption>();

    private int gestureCount;
    private float qteTimer;
    private float escapeTimer;
    private float nextGestureTime;
    private float nextQTETime;
    private float lastAcceptedGestureTime = -1f;

    private bool fightStarted;
    private bool escapeTimeUp;
    private bool qteActive;
    private bool resolving;
    private bool fightEnded;

    private bool previousFlashlightState;
    private bool flashlightStateSaved;
    private Quaternion originalBodyRotation;

    private Coroutine actionRoutine;
    private string lastQTEGesture = "";

    [Header("Player Health")]
    public PlayerHealth playerHealth;

    void Start()
    {
        escapeTimer = escapeDuration;

        if (qteText != null) qteText.gameObject.SetActive(false);
        if (progressText != null) progressText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);

        // Validate the configured commands.
        HashSet<string> uniqueCommands = new HashSet<string>();

        foreach (GestureOption option in gestures)
        {
            if (option == null || string.IsNullOrWhiteSpace(option.command))
            {
                Debug.LogError("TurkeyKingQTE: A gesture command is empty.");
                enabled = false;
                return;
            }

            string command = option.command.Trim().ToUpperInvariant();

            if (!uniqueCommands.Add(command))
            {
                Debug.LogError(
                    "TurkeyKingQTE: Duplicate gesture command: " + command);
                enabled = false;
                return;
            }
        }

        if (gestures.Length < requiredGestures)
        {
            Debug.LogError(
                "TurkeyKingQTE: You need at least " +
                requiredGestures + " unique gestures.");
            enabled = false;
        }
    }

    // Called by FuseBoxController when both fuses are inserted.
    public void StartEscapeTimer()
    {
        escapeTimer = escapeDuration;
        escapeTimeUp = false;
        fightStarted = true;
    }

    void Update()
    {
        if (fightEnded)
            return;

        // Wait until the fusebox starts the fight (no more distance trigger).
        if (!fightStarted)
            return;

        if (!escapeTimeUp)
        {
            escapeTimer -= Time.deltaTime;

            if (escapeTimer <= 0f)
                CompleteEscape();
        }

        if (timerText != null && !qteActive)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = escapeTimeUp
                ? "ESCAPE NOW!!"
                : "ESCAPE: " + Mathf.CeilToInt(Mathf.Max(0f, escapeTimer));
        }

        if (qteActive)
        {
            qteTimer -= Time.deltaTime;
            UpdateUI();

            if (qteTimer <= 0f)
                FailQTE();

            return;
        }

        if (resolving || player == null || boss == null)
            return;

        if (Time.time < nextQTETime)
            return;

        if (Vector3.Distance(player.position, boss.position)
            <= triggerDistance)
        {
            Debug.Log("Turkey King is close! Starting QTE.");
            StartQTE();
        }
    }

    void StartQTE()
    {
        if (gestures.Length < requiredGestures)
            return;

        sequence.Clear();

        List<GestureOption> pool = new List<GestureOption>(gestures);

        // Pick unique gestures in random order.
        for (int i = 0; i < requiredGestures; i++)
        {
            int index = Random.Range(i, pool.Count);

            GestureOption temp = pool[i];
            pool[i] = pool[index];
            pool[index] = temp;

            sequence.Add(pool[i]);
        }

        qteActive = true;

        if (turkeyKingBoss != null)
        {
            turkeyKingBoss.PauseBossMovement(true);
        }

        gestureCount = 0;
        qteTimer = qteTimeLimit;
        nextGestureTime = 0f;
        lastAcceptedGestureTime = -1f;
        lastQTEGesture = "";

        PausePlayer(true);

        if (qteText != null)
            qteText.gameObject.SetActive(true);

        if (progressText != null)
            progressText.gameObject.SetActive(true);

        if (timerText != null)
            timerText.gameObject.SetActive(true);

        UpdateUI();
    }

    // Called by UDPGestureReceiver for each incoming message.
    public void ReceiveGesture(string gesture)
    {
        Debug.Log("QTE received gesture: " + gesture);

        if (!qteActive || resolving || fightEnded)
        {
            Debug.Log("Gesture ignored: QTE is not active.");
            return;
        }

        if (string.IsNullOrWhiteSpace(gesture))
            return;

        string normalized = gesture.Trim().ToUpperInvariant();

        // Only challenge gestures count. Ignore movement and unrelated data.
        int matchedIndex = -1;

        for (int i = 0; i < sequence.Count; i++)
        {
            if (sequence[i].command.Trim().ToUpperInvariant() == normalized)
            {
                matchedIndex = i;
                break;
            }
        }

        if (matchedIndex == -1)
        {
            Debug.Log("Gesture not in QTE sequence: " + normalized);
            return;
        }

        Debug.Log("Gesture matched sequence index: " + matchedIndex);
        Debug.Log("Next expected index: " + gestureCount);

        // Ignore repeated packets of the same pose for a short period.
        if (normalized == lastQTEGesture &&
            Time.time - lastAcceptedGestureTime < sameGestureCooldown)
            return;

        if (Time.time < nextGestureTime)
            return;

        lastQTEGesture = normalized;
        lastAcceptedGestureTime = Time.time;
        nextGestureTime = Time.time + gestureDebounce;

        if (matchedIndex < gestureCount)
        {
            return; // Ignore gestures already completed
        }

        if (matchedIndex != gestureCount)
        {
            Debug.Log("Wrong gesture order. Expected: " +
                sequence[gestureCount].command);

            return; // Ignore wrong order instead of failing
        }

        gestureCount++;
        UpdateUI();

        if (gestureCount >= requiredGestures)
            SucceedQTE();
    }

    void UpdateUI()
    {
        if (progressText != null)
            progressText.text = gestureCount + " / " + requiredGestures;

        if (qteText != null)
        {
            string instructions = "PERFORM IN ORDER:\n";

            for (int i = 0; i < sequence.Count; i++)
            {
                string name = string.IsNullOrWhiteSpace(
                    sequence[i].displayName)
                    ? sequence[i].command
                    : sequence[i].displayName;

                instructions += (i + 1) + ". " + name;

                if (i < gestureCount)
                    instructions += "  ✓";

                instructions += "\n";
            }

            qteText.text = instructions;
        }

        if (timerText != null && qteActive)
            timerText.text = "TIME LEFT: " +
                Mathf.Max(0f, qteTimer).ToString("F1") + "s";
    }

    void SucceedQTE()
    {
        if (!qteActive || resolving)
            return;

        qteActive = false;
        resolving = true;
        HideQTEUI();

        actionRoutine = StartCoroutine(SuccessRoutine());
    }

    IEnumerator SuccessRoutine()
    {
        if (turkeyKingBoss != null)
        {
            turkeyKingBoss.OnFlashed(3f);
        }
        if (playerBody != null)
        {
            originalBodyRotation = playerBody.rotation;

            if (boss != null)
            {
                Vector3 direction = boss.position - playerBody.position;
                direction.y = 0f;

                if (direction.sqrMagnitude > 0.001f)
                    playerBody.rotation = Quaternion.LookRotation(direction);
            }
        }

        if (flashlight != null)
        {
            previousFlashlightState = flashlight.enabled;
            flashlightStateSaved = true;
            flashlight.enabled = true;
        }

        yield return new WaitForSeconds(flashlightDuration);

        if (flashlight != null && flashlightStateSaved)
            flashlight.enabled = previousFlashlightState;

        if (playerBody != null)
            playerBody.rotation = originalBodyRotation;

        yield return new WaitForSeconds(stunDuration);

        if (bossChaseScript != null)
            bossChaseScript.enabled = true;

        PausePlayer(false);

        nextQTETime = Time.time + qteCooldown;
        if (turkeyKingBoss != null && !turkeyKingBoss.IsStunned)
        {
            turkeyKingBoss.PauseBossMovement(false);
        }
        resolving = false;
        actionRoutine = null;
    }

    void FailQTE()
    {
        if (!qteActive || resolving)
            return;

        qteActive = false;
        resolving = true;
        HideQTEUI();

        if (actionRoutine != null)
            StopCoroutine(actionRoutine);

        actionRoutine = StartCoroutine(FailureRoutine());
    }

    IEnumerator FailureRoutine()
    {
        // Lose 1 heart when QTE fails
        if (playerHealth != null)
        {
            bool lostHeart = playerHealth.TakeHit(1);

            if (lostHeart)
                Debug.Log("QTE failed! Player lost 1 heart.");
            else
                Debug.Log("No heart lost: player is invulnerable or already dead.");
        }
        else
        {
            Debug.LogWarning("TurkeyKingQTE: PlayerHealth is not assigned!");
        }

        // Let the player move again
        PausePlayer(false);

        // Stop the Turkey King
        if (turkeyKingBoss != null)
            turkeyKingBoss.PauseBossMovement(true);

        Debug.Log("Turkey King stopped for 2 seconds.");

        // Wait 2 seconds
        yield return new WaitForSeconds(2f);

        // Resume chasing
        if (bossChaseScript != null)
            bossChaseScript.enabled = true;

        if (turkeyKingBoss != null && !turkeyKingBoss.IsStunned)
            turkeyKingBoss.PauseBossMovement(false);

        Debug.Log("Turkey King resumed chasing.");

        // Wait for the next QTE cooldown
        yield return new WaitForSeconds(failureCooldown);

        nextQTETime = Time.time + qteCooldown;
        resolving = false;
        actionRoutine = null;
    }

    void CompleteEscape()
    {
        // Timer is over, but the boss keeps chasing and QTEs keep working
        // until the player actually gets out.
        escapeTimeUp = true;

        if (timerText != null && !qteActive)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = "ESCAPE NOW!!";
        }

        onEscapeComplete?.Invoke();
    }

    void HideQTEUI()
    {
        if (qteText != null) qteText.gameObject.SetActive(false);
        if (progressText != null) progressText.gameObject.SetActive(false);
    }

    void PausePlayer(bool pause)
    {
        if (playerControlsToPause == null)
            return;

        foreach (MonoBehaviour control in playerControlsToPause)
        {
            if (control != null)
                control.enabled = !pause;
        }
    }
}