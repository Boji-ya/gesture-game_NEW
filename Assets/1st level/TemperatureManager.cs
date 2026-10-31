using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TemperatureManager : MonoBehaviour
{
    public float currentTemp = 360f;
    public float dropSpeed = 1f;

    public Image frostUI;
    public AudioSource breathSound;

    // 是否正在冷凍庫中
    private bool freezerActive = false;

    // 是否已經逃離冷凍庫
    private bool escaped = false;

    void Start()
    {
        freezerActive = false;
        escaped = false;

        // 一開始不要顯示結霜效果
        if (frostUI != null)
        {
            Color tempColor = frostUI.color;
            tempColor.a = 0f;
            frostUI.color = tempColor;
        }

        // 一開始不要播放呼吸聲
        if (breathSound != null && breathSound.isPlaying)
        {
            breathSound.Stop();
        }
    }

    void Update()
    {
        // 還沒進冷凍庫，或已經逃出去 → 不做任何事情
        if (!freezerActive || escaped)
            return;

        // 溫度開始下降
        currentTemp -= Time.deltaTime * dropSpeed;

        update_freeze_effect();
        update_breahe_audio();

        // 溫度歸零 → 死亡 / 重開
        if (currentTemp <= 0)
        {
            die();
        }
    }

    // 玩家進入冷凍庫時呼叫
    public void EnterFreezer()
    {
        // 已經逃出去就不能再次啟動
        if (escaped)
            return;

        freezerActive = true;

        Debug.Log("玩家進入冷凍庫，溫度系統開始！");

        update_freeze_effect();
        update_breahe_audio();
    }

    private void update_freeze_effect()
    {
        if (frostUI == null)
            return;

        if (currentTemp <= 150)
        {
            float alpha = (150 - currentTemp) / 180f;

            Color tempColor = frostUI.color;
            tempColor.a = Mathf.Clamp01(alpha);
            frostUI.color = tempColor;
        }
    }

    void update_breahe_audio()
    {
        if (breathSound == null)
            return;

        if (currentTemp <= 30)
        {
            if (!breathSound.isPlaying)
                breathSound.Play();
        }
        else
        {
            if (breathSound.isPlaying)
                breathSound.Stop();
        }
    }

    // 玩家離開冷凍庫時呼叫
    public void EscapeFreezer()
    {
        escaped = true;
        freezerActive = false;

        // 停止呼吸聲
        if (breathSound != null && breathSound.isPlaying)
        {
            breathSound.Stop();
        }

        // 清除結霜效果
        if (frostUI != null)
        {
            Color tempColor = frostUI.color;
            tempColor.a = 0f;
            frostUI.color = tempColor;
        }

        Debug.Log("玩家已離開冷凍庫，溫度系統永久停止。");
    }

    private void die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}