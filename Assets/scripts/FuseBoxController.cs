using UnityEngine;
using System.Collections;

public class FuseBoxController : MonoBehaviour
{
    [Header("Player Backpack")]
    public backpack playerBackpack;

    [Header("Alarm")]
    public AudioSource alarmAudio;
    public float alarmDuration = 3f;

    [Header("Chase Music")]
    public AudioSource chaseMusic;
    public bool stopChaseMusicAfterDoorOpens = true;

    [Header("Door")]
    public Transform door;
    public float doorOpenHeight = 3f;
    public float doorOpenDuration = 2f;

    [Header("Delay")]
    public float doorOpenDelay = 30f;

    [Header("Final Battle")]
    public Finalboss finalBoss;
    public TurkeyKingBoss turkeyKingBoss;
    public TurkeyKingQTE turkeyKingQTE;

    [Header("Turkey King Model")]
    public GameObject turkeyKingModel;

    private bool activated = false;

    private void Start()
    {
        // Hide the Turkey King model at the start.
        if (turkeyKingModel != null)
        {
            turkeyKingModel.SetActive(false);
        }

        // Disable the boss AI at the start.
        if (turkeyKingBoss != null)
        {
            turkeyKingBoss.enabled = false;
        }

        // Disable the QTE at the start.
        if (turkeyKingQTE != null)
        {
            turkeyKingQTE.enabled = false;
        }
    }

    public void ActivateFuseBox()
    {
        if (activated)
        {
            Debug.Log("Fusebox already activated!");
            return;
        }

        if (playerBackpack == null)
        {
            Debug.LogWarning("FuseBox: Player Backpack is missing!");
            return;
        }

        // Check whether the player has two fuses.
        int fuseCount = playerBackpack.GetItemCount("fuse");

        Debug.Log("Current backpack Fuse count: " + fuseCount);

        if (fuseCount < 2)
        {
            Debug.Log("Not enough fuses! Need 2 fuses.");
            return;
        }

        // Consume both fuses.
        bool firstFuseUsed =
            playerBackpack.UseItemByName("fuse");

        bool secondFuseUsed =
            playerBackpack.UseItemByName("fuse");

        if (!firstFuseUsed || !secondFuseUsed)
        {
            Debug.LogWarning(
                "Fuse consumption failed! Cannot activate fusebox."
            );
            return;
        }

        activated = true;

        Debug.Log("Both fuses inserted! Starting final battle.");

        // Show the Turkey King model.
        if (turkeyKingModel != null)
        {
            turkeyKingModel.SetActive(true);
            Debug.Log("Turkey King model is visible!");
        }
        else
        {
            Debug.LogError("Turkey King Model reference is missing!");
        }

        // Enable the Turkey King AI.
        if (turkeyKingBoss != null)
        {
            turkeyKingBoss.enabled = true;
            Debug.Log("Turkey King enabled!");
        }
        else
        {
            Debug.LogError("TurkeyKingBoss reference is missing!");
        }

        // Enable the Turkey King QTE and start the escape timer.
        if (turkeyKingQTE != null)
        {
            turkeyKingQTE.enabled = true;
            turkeyKingQTE.StartEscapeTimer();
            Debug.Log("Turkey King QTE enabled and escape timer started!");
        }
        else
        {
            Debug.LogError("TurkeyKingQTE reference is missing!");
        }

        // Start the final battle.
        if (finalBoss != null)
        {
            finalBoss.StartFinalBattle();
            Debug.Log("StartFinalBattle called!");
        }
        else
        {
            Debug.LogError("Finalboss reference is missing!");
        }

        // Continue the alarm, music, and door sequence.
        StartCoroutine(FuseBoxSequence());
    }

    private IEnumerator FuseBoxSequence()
    {
        // Step 1: Play the alarm.
        if (alarmAudio != null)
        {
            alarmAudio.Play();
            Debug.Log("Alarm started!");

            yield return new WaitForSeconds(alarmDuration);

            alarmAudio.Stop();
            Debug.Log("Alarm finished!");
        }
        else
        {
            yield return new WaitForSeconds(alarmDuration);
        }

        // Step 2: Start the chase music.
        if (chaseMusic != null)
        {
            chaseMusic.loop = true;
            chaseMusic.Play();
            Debug.Log("Chase music started!");
        }

        // Step 3: Wait before opening the door.
        Debug.Log(doorOpenDelay + " seconds door delay started!");

        yield return new WaitForSeconds(doorOpenDelay);

        // Step 4: Open the door.
        if (door != null)
        {
            Debug.Log("Delay finished! Opening door!");
            yield return StartCoroutine(OpenDoor());
        }
        else
        {
            Debug.LogWarning("FuseBox has no Door assigned!");
        }

        // Step 5: Stop the chase music after the door opens.
        if (stopChaseMusicAfterDoorOpens && chaseMusic != null)
        {
            chaseMusic.Stop();
            Debug.Log("Chase music stopped!");
        }
    }

    private IEnumerator OpenDoor()
    {
        Vector3 startPosition = door.position;

        Vector3 targetPosition =
            startPosition + Vector3.up * doorOpenHeight;

        float elapsed = 0f;

        while (elapsed < doorOpenDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / doorOpenDuration
            );

            door.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            yield return null;
        }

        door.position = targetPosition;

        Debug.Log("Door fully opened!");
    }
}