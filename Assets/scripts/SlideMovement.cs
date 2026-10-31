using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideMovement : MonoBehaviour
{
    [Header("Fuse Requirement")]
    public bool requiresFuse = false;
    public string requiredItemName = "Fuse";
    public backpack playerBackpack;

    [Header("Slide Points")]
    public Transform slideStart;
    public Transform slideEnd;

    [Header("Settings")]
    public float slideDuration = 3f;
    public float triggerRange = 2f;

    [Header("Player Scripts To Disable While Sliding")]
    public MonoBehaviour[] movementScriptsToDisable;

    private static readonly List<SlideMovement> allSlides =
        new List<SlideMovement>();

    private static bool playerIsSliding = false;

    private void OnEnable()
    {
        if (!allSlides.Contains(this))
            allSlides.Add(this);
    }

    private void OnDisable()
    {
        allSlides.Remove(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform p = other.CompareTag("Player") ? other.transform : other.transform.root;

        if (p.CompareTag("Player"))
            TryStartSlide(p);
    }
    // Call this when the grab gesture is detected.
    public static void TryStartNearest(Transform player)
    {
        if (player == null || playerIsSliding)
            return;

        SlideMovement nearestSlide = null;
        float nearestDistance = float.MaxValue;

        foreach (SlideMovement slide in allSlides)
        {
            if (slide.requiresFuse)
            {
                if (slide.playerBackpack == null)
                    continue;

                if (!slide.playerBackpack.HasItem(slide.requiredItemName))
                    continue;
            }

            if (slide == null || slide.slideStart == null ||
                slide.slideEnd == null)
                continue;

            float distance = Vector3.Distance(
                player.position,
                slide.slideStart.position
            );

            if (distance <= slide.triggerRange &&
                distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSlide = slide;
            }
        }

        if (nearestSlide == null)
        {
            Debug.Log("No slide entrance nearby.");
            return;
        }

        nearestSlide.StartCoroutine(
            nearestSlide.SlidePlayer(player)
        );
    }
    public void TryStartSlide(Transform player)
    {
        if (player == null || playerIsSliding)
            return;

        if (slideStart == null || slideEnd == null)
        {
            Debug.LogWarning(
                "Slide start or end is not assigned: " + gameObject.name
            );
            return;
        }

        // Check whether this slide requires a fuse.
        if (requiresFuse)
        {
            if (playerBackpack == null)
            {
                Debug.LogWarning("Slide backpack is not assigned!");
                return;
            }

            if (!playerBackpack.HasItem(requiredItemName))
            {
                Debug.Log("You need the fuse to use this slide!");
                return;
            }
        }

        // Make sure the player is near this slide's entrance.
        float distance = Vector3.Distance(
            player.position,
            slideStart.position
        );

        if (distance > triggerRange)
        {
            Debug.Log("Too far from the slide entrance.");
            return;
        }

        StartCoroutine(SlidePlayer(player));
    }

    private IEnumerator SlidePlayer(Transform player)
    {
        playerIsSliding = true;

        // Disable normal movement while sliding.
        foreach (MonoBehaviour script in movementScriptsToDisable)
        {
            if (script != null)
                script.enabled = false;
        }

        // Start at the slide entrance.
        player.position = slideStart.position;

        Vector3 startPosition = slideStart.position;
        Vector3 endPosition = slideEnd.position;

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            if (player == null)
                break;

            float t = elapsed / slideDuration;

            player.position = Vector3.Lerp(
                startPosition,
                endPosition,
                t
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (player != null)
        {
            player.position = endPosition;

            // Restore normal movement.
            foreach (MonoBehaviour script in movementScriptsToDisable)
            {
                if (script != null)
                    script.enabled = true;
            }
        }

        playerIsSliding = false;

        Debug.Log("Player finished sliding!");
    }
}