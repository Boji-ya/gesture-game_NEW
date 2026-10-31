using UnityEngine;

public class UVGlowSymbol : MonoBehaviour
{
    [Header("Glow Settings")]
    public float maxGlowIntensity = 2f;
    public float fadeSpeed = 3f;

    private Renderer rend;
    private MaterialPropertyBlock mpb;
    private float targetIntensity = 0f;
    private float currentIntensity = 0f;

    void Start()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        // 平滑淡入淡出
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * fadeSpeed);

        rend.GetPropertyBlock(mpb);
        mpb.SetFloat("_GlowIntensity", currentIntensity);
        rend.SetPropertyBlock(mpb);
    }

    // 被照到時
    public void SetLitAmount(float amount)
    {
        targetIntensity = amount * maxGlowIntensity;
    }
}