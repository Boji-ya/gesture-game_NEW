using UnityEngine;
using System.Collections.Generic;

public class FreezePuzzleManager : MonoBehaviour
{
    public lever_controller[] levers;
    public int[] correctOrder = { 3, 2, 1, 4 };

    private List<int> playerInput = new List<int>();

    //  音效元件
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;

    // 開門
    public RotatingDoor targetDoor; // 綁定要打開的門
    // 當玩家「拉下」拉桿時呼叫
    public void AddInput(int leverID)
    {
        playerInput.Add(leverID);
        Debug.Log($"拉下第 {leverID+1} 根。目前輸入數量：{playerInput.Count}");

        // 如果 4 根都拉下來了，就來對答案！
        if (playerInput.Count == correctOrder.Length)
        {
            CheckResult();
        }
    }

    // 當玩家發現不對勁，「推回」拉桿時呼叫
    public void RemoveInput(int leverID)
    {
        if (playerInput.Contains(leverID))
        {
            playerInput.Remove(leverID);
            Debug.Log($"推回第 {leverID} 根。輸入已取消。");
        }
    }

    private void CheckResult()
    {
        bool isCorrect = true;

        for (int i = 0; i < correctOrder.Length; i++)
        {
            if (playerInput[i] != correctOrder[i])
            {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect)
        {
            Debug.Log("順序完全正確！通關！");
            if (audioSource != null && correctSound != null)
            {
                audioSource.PlayOneShot(correctSound);
            }
            if (targetDoor != null)
            {
                targetDoor.PuzzleComplete();
            }
        }
        else
        {
            Debug.Log(" 順序錯誤！門沒有動靜... (等待玩家自己推回去重試)");
            if (audioSource != null && wrongSound != null)
            {
                audioSource.PlayOneShot(wrongSound);
            }
        }
    }
}