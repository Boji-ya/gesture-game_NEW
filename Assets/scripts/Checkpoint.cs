using UnityEngine;

/// <summary>
/// Checkpoint for one puzzle. Two ways to use it:
///  1. Walk-in: put it on an object with a trigger Collider; the checkpoint saves when the player enters.
///  2. Puzzle solved: call Activate() from your puzzle script (or a UnityEvent) instead.
///
/// Make one for Puzzle 1, one for Puzzle 2 (the Fallen Angel puzzle) and one for Puzzle 3.
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Tooltip("1, 2 or 3. Higher numbers always replace lower ones.")]
    public int puzzleNumber = 1;

    [Tooltip("Where the player reappears. Leave empty to use this object's position and rotation. " +
             "Place it on solid floor, slightly above the ground, facing the right way.")]
    public Transform respawnPoint;

    [Tooltip("Turn off if you only want to save via Activate() from a puzzle script.")]
    public bool activateOnTrigger = true;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!activateOnTrigger) return;

        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            Activate();
    }

    /// <summary>Saves this checkpoint. Safe to call from UnityEvents.</summary>
    public void Activate()
    {
        if (CheckpointManager.Instance == null)
        {
            Debug.LogWarning("Checkpoint: no CheckpointManager in the scene.", this);
            return;
        }

        Transform point = respawnPoint != null ? respawnPoint : transform;
        CheckpointManager.Instance.SetCheckpoint(puzzleNumber, point);
    }

    private void OnDrawGizmos()
    {
        Transform point = respawnPoint != null ? respawnPoint : transform;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(point.position, 0.4f);
        Gizmos.DrawRay(point.position, point.forward);
    }
}
