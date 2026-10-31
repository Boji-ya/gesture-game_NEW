using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the player's hearts on screen.
///
/// SETUP: Canvas > create 5 Image objects in a row (a Horizontal Layout Group helps),
/// put this script on the Canvas (or a parent), and drag the 5 Images into "Hearts".
/// Optional: assign Full / Empty sprites. If you leave them empty, lost hearts are simply hidden.
/// </summary>
public class HeartsUI : MonoBehaviour
{
    public PlayerHealth health;
    public Image[] hearts;

    [Tooltip("Optional. If both are set, lost hearts swap to the empty sprite instead of hiding.")]
    public Sprite fullHeart;
    public Sprite emptyHeart;

    private void Start()
    {
        if (health == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                health = p.GetComponentInParent<PlayerHealth>();
                if (health == null)
                    health = p.GetComponentInChildren<PlayerHealth>();
            }
        }

        if (health == null)
        {
            Debug.LogError("HeartsUI: no PlayerHealth found.", this);
            enabled = false;
            return;
        }

        health.HeartsChanged += Refresh;
        Refresh(health.CurrentHearts, health.maxHearts);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.HeartsChanged -= Refresh;
    }

    private void Refresh(int current, int max)
    {
        bool useSprites = fullHeart != null && emptyHeart != null;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;

            bool filled = i < current;

            if (useSprites)
            {
                hearts[i].enabled = true;
                hearts[i].sprite = filled ? fullHeart : emptyHeart;
            }
            else
            {
                hearts[i].enabled = filled;
            }
        }
    }
}
