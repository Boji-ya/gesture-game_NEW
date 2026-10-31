
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [Min(1)] public int maxHearts = 5;
    public float invulnerableTime = 2f;

    [Header("Death Scene")]
    public string deathSceneName = "DeathScene";

    public UnityEvent onDamaged;
    public UnityEvent onDied;

    public event Action<int, int> HeartsChanged;
    public event Action Died;

    public int CurrentHearts { get; private set; }
    public bool IsDead { get; private set; }

    private float invulnerableUntil;

    public bool CanBeHit =>
        !IsDead && Time.time >= invulnerableUntil;

    private void Awake()
    {
        CurrentHearts = maxHearts;
    }

    public bool TakeHit(int amount = 1)
    {
        if (!CanBeHit || amount <= 0)
            return false;

        CurrentHearts = Mathf.Max(0, CurrentHearts - amount);
        invulnerableUntil = Time.time + invulnerableTime;

        HeartsChanged?.Invoke(CurrentHearts, maxHearts);

        if (CurrentHearts <= 0)
        {
            IsDead = true;

            // 觸發原本的死亡事件
            onDied?.Invoke();
            Died?.Invoke();

            // 切換至死亡畫面
            SceneManager.LoadScene(deathSceneName);
        }
        else
        {
            onDamaged?.Invoke();
        }

        return true;
    }

    public void Heal(int amount = 1)
    {
        if (IsDead || amount <= 0) return;

        CurrentHearts = Mathf.Min(maxHearts, CurrentHearts + amount);
        HeartsChanged?.Invoke(CurrentHearts, maxHearts);
    }

    public void ResetHearts()
    {
        CurrentHearts = maxHearts;
        IsDead = false;
        invulnerableUntil = Time.time + invulnerableTime;

        HeartsChanged?.Invoke(CurrentHearts, maxHearts);
    }
}
