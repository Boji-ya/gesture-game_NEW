using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial")]
    public Image tutorialImage;
    public Sprite[] tutorialImages;
    //每5秒切換一次
    public float imageDuration = 5f;

    [Header("VHS noise")]
    public GameObject vhsNoise;
    public VideoPlayer vhsVideo;
    public AudioSource glitchAudio;

    [Header("Black Screen")]
    public GameObject blackScreen;

    [Header("Next Scene")]
    public string nextScene = "SampleScene";

    public float glitchDuration = 3f;
    public float blackScreenDuration = 1f;

    void Start()
    {
        StartCoroutine(PlayTutorial());
    }

    IEnumerator PlayTutorial()
    {
        // 播放 5 張手勢教學圖片
        for (int i = 0; i < tutorialImages.Length; i++)
        {
            tutorialImage.sprite = tutorialImages[i];

            yield return new WaitForSeconds(imageDuration);
        }

        // 最後一張稍微停一下
        yield return new WaitForSeconds(0.5f);

        // 開始 VHS 雜訊
        Startnoise();

        // 等待雜訊播放
        yield return new WaitForSeconds(glitchDuration);

        // 黑屏
        blackScreen.SetActive(true);

        yield return new WaitForSeconds(blackScreenDuration);

        // 進入遊戲
        SceneManager.LoadScene(nextScene);
    }

    void Startnoise()
    {
        vhsNoise.SetActive(true);

        vhsVideo.Play();

        glitchAudio.Play();
    }
}