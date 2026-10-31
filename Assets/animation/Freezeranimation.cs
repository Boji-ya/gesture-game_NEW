using UnityEngine;
using UnityEngine.Playables;

public class Freezeranimation : MonoBehaviour
{
    public Camera playerCam;
    public Camera cutsceneCam;
    public FirstPersonMovement playerMovement;
    public MonoBehaviour playerLook;
    public RotatingDoor door;
    public PlayableDirector freezerTimeline;

    private bool playing = false;

    void Awake()
    {
        if (cutsceneCam != null)
            cutsceneCam.enabled = false;

        if (freezerTimeline != null)
            freezerTimeline.playOnAwake = false;
    }

    public void OpenDoorEvent()
    {
        if (door != null)
            door.OpenDoor();
    }

    public void FreezerRoomDone()
    {
        if (playing) return;

        if (freezerTimeline == null)
        {
            Debug.LogError("Freezer Timeline is not assigned!");
            return;
        }

        playing = true;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerLook != null)
            playerLook.enabled = false;

        if (playerCam != null)
            playerCam.enabled = false;

        if (cutsceneCam != null)
            cutsceneCam.enabled = true;

        freezerTimeline.time = 0;
        freezerTimeline.Play();
    }

    public void EndCutscene()
    {
        if (freezerTimeline != null)
            freezerTimeline.Stop();

        if (cutsceneCam != null)
            cutsceneCam.enabled = false;

        if (playerCam != null)
            playerCam.enabled = true;

        if (playerMovement != null)
            playerMovement.enabled = true;

        if (playerLook != null)
            playerLook.enabled = true;

        playing = false;
    }
}