using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Remembers the last checkpoint (puzzle 1, 2 or 3) and respawns the player there when they die.
/// Put ONE of these in the scene (e.g. on an empty "GameManager" object).
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("References")]
    public Transform player;
    public PlayerHealth playerHealth;

    [Tooltip("Where the player respawns if they die before reaching puzzle 1. " +
             "Leave empty to use the player's starting position.")]
    public Transform startSpawn;

    [Tooltip("Optional: movement / mouse-look scripts to disable while dead so the player can't move during the respawn delay.")]
    public Behaviour[] disableWhileDead;

    [Tooltip("Angels to send back to their starting spots when the player respawns.")]
    public FallenAngelAI[] angelsToReset;

    [Header("Respawn")]
    public float respawnDelay = 1.5f;

    [Header("Events")]
    public UnityEvent onPlayerDied;      // e.g. start a fade to black
    public UnityEvent onPlayerRespawned; // e.g. fade back in

    [Header("Debug")]
    public bool debugLogs = false;

    /// <summary>0 = start of the game, 1/2/3 = puzzle checkpoints.</summary>
    public int CurrentPuzzle { get; private set; }

    private Vector3 respawnPosition;
    private Quaternion respawnRotation;
    private bool respawning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (playerHealth == null && player != null)
        {
            playerHealth = player.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = player.GetComponentInChildren<PlayerHealth>();
        }

        if (player == null || playerHealth == null)
        {
            Debug.LogError("CheckpointManager needs a Player and a PlayerHealth.", this);
            enabled = false;
            return;
        }

        // Default respawn = start of the game.
        if (startSpawn != null)
        {
            respawnPosition = startSpawn.position;
            respawnRotation = startSpawn.rotation;
        }
        else
        {
            respawnPosition = player.position;
            respawnRotation = player.rotation;
        }

        playerHealth.Died += OnPlayerDied;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.Died -= OnPlayerDied;

        if (Instance == this)
            Instance = null;
    }

    // =========================================================
    // CHECKPOINTS
    // =========================================================

    /// <summary>
    /// Saves a checkpoint for a puzzle. Only moves forward: reaching puzzle 1 again
    /// after puzzle 2 won't overwrite the puzzle 2 checkpoint.
    /// </summary>
    public void SetCheckpoint(int puzzleNumber, Transform point)
    {
        if (puzzleNumber <= CurrentPuzzle) return;

        CurrentPuzzle = puzzleNumber;
        respawnPosition = point.position;
        respawnRotation = point.rotation;

        if (debugLogs)
            Debug.Log($"Checkpoint saved: puzzle {puzzleNumber}", this);
    }

    // =========================================================
    // DEATH / RESPAWN
    // =========================================================

    private void OnPlayerDied()
    {
        if (respawning) return;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        respawning = true;

        if (debugLogs)
            Debug.Log($"Player died. Respawning at puzzle {CurrentPuzzle} checkpoint.", this);

        SetControlsEnabled(false);
        onPlayerDied?.Invoke();

        yield return new WaitForSeconds(respawnDelay);

        TeleportPlayer();

        playerHealth.ResetHearts();

        for (int i = 0; i < angelsToReset.Length; i++)
        {
            if (angelsToReset[i] != null)
                angelsToReset[i].ResetAngel();
        }

        SetControlsEnabled(true);
        onPlayerRespawned?.Invoke();

        respawning = false;
    }

    private void TeleportPlayer()
    {
        // A CharacterController overrides transform changes, so switch it off while moving.
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.SetPositionAndRotation(respawnPosition, respawnRotation);
        Physics.SyncTransforms();

        if (cc != null) cc.enabled = true;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void SetControlsEnabled(bool value)
    {
        for (int i = 0; i < disableWhileDead.Length; i++)
        {
            if (disableWhileDead[i] != null)
                disableWhileDead[i].enabled = value;
        }
    }
}
