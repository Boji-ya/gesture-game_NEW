using UnityEngine;

public class RotatingDoor : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float openAngle = 80f;

    [Tooltip("Rotation speed. 1 = 100 degrees per second")]
    public float openSpeed = 1f;

    [Header("Sound Settings")]
    public AudioSource audioSource;
    public AudioClip openSound;

    [Header("Door Collider")]
    public Collider doorCollider;

    [Header("Cutscene")]
    public Freezeranimation doorCutscene;   // the parent object with the Animator + Freezeranimation

    // Local rotation, so the door works correctly under DoorHinge
    private Quaternion closedRotation;
    private Quaternion targetOpenRotation;

    private bool isOpen = false;
    private bool playerInside = false;
    private bool puzzleSolved = false;

    void Start()
    {
        closedRotation = transform.localRotation;

        targetOpenRotation =
            closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        Quaternion target = isOpen ? targetOpenRotation : closedRotation;

        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            target,
            openSpeed * 100f * Time.deltaTime
        );
    }

    // Player gestures outside -> open door
    public void OpenFromGesture()
    {
        // Once inside the freezer, gestures can't open the door
        if (playerInside)
        {
            Debug.Log("Player is inside the freezer, gesture can't open the door.");
            return;
        }

        // Puzzle already solved, don't open by gesture
        if (puzzleSolved)
            return;

        OpenDoor();
    }

    // The real "open door"
    // Called by PuzzleComplete (no cutscene) or by the cutscene's Animation Event
    public void OpenDoor()
    {
        if (isOpen)
            return;

        isOpen = true;

        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }

        if (audioSource != null && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }

        Debug.Log("Freezer door opening.");
    }

    // Player enters the freezer
    public void PlayerEnterFreezer()
    {
        if (playerInside)
            return;

        playerInside = true;

        Debug.Log("Player entered freezer, door closes and locks.");

        CloseDoor();
    }

    // Close door (smoothly rotates back in Update)
    public void CloseDoor()
    {
        isOpen = false;

        if (doorCollider != null)
        {
            doorCollider.enabled = true;
        }

        Debug.Log("Freezer door closing.");
    }

    // Puzzle solved
    public void PuzzleComplete()
    {
        if (puzzleSolved) return;

        puzzleSolved = true;

        if (doorCutscene != null)
        {
            doorCutscene.FreezerRoomDone();
        }
        else
        {
            // Fallback if no cutscene is assigned
            OpenDoor();
        }
    }

    // Player leaves the freezer
    public void PlayerExitFreezer()
    {
        playerInside = false;
    }

    // Lets other scripts check puzzle state
    public bool IsPuzzleSolved()
    {
        return puzzleSolved;
    }
}