using UnityEngine;
using System.Collections.Generic;

public class UVFlashlight : MonoBehaviour
{
    [Header("References")]
    public Light flashlightLight;
    public Transform flashlightOrigin;
    public backpack playerBackpack;

    [Header("Detection Settings")]
    public float range = 10f;
    public float angle = 20f;
    public LayerMask symbolLayer;

    private HashSet<UVGlowSymbol> previouslyLit = new HashSet<UVGlowSymbol>();
    void Start()
    {
        /*// 遊戲開始時，紫光一定關閉
        if (flashlightLight != null)
        {
            flashlightLight.enabled = false;
        }*/
    }

    void Update()
    {
        bool flashlightOn =
            playerBackpack != null &&
            playerBackpack.GetSelectedItem() == "flashlight" &&
            flashlightLight != null &&
            flashlightLight.enabled;

        HashSet<UVGlowSymbol> nowLit = new HashSet<UVGlowSymbol>();

        if (flashlightOn)
        {
            Collider[] hits = Physics.OverlapSphere(
                flashlightOrigin.position,
                range,
                symbolLayer
            );

            foreach (var hit in hits)
            {
                UVGlowSymbol symbol = hit.GetComponent<UVGlowSymbol>();
                if (symbol == null) continue;

                Vector3 dirToSymbol =
                    (hit.transform.position - flashlightOrigin.position).normalized;

                float angleToSymbol =
                    Vector3.Angle(flashlightOrigin.forward, dirToSymbol);

                if (angleToSymbol <= angle)
                {
                    float angleFactor = 1f - (angleToSymbol / angle);

                    float distToSymbol =
                        Vector3.Distance(
                            flashlightOrigin.position,
                            hit.transform.position
                        );

                    float distFactor =
                        1f - Mathf.Clamp01(distToSymbol / range);

                    float finalFactor = angleFactor * distFactor;

                    if (finalFactor > 0.05f)
                    {
                        nowLit.Add(symbol);
                        symbol.SetLitAmount(finalFactor);
                    }
                }
            }
        }

        // 原本亮著、現在沒被偵測到的 symbol → 淡出
        foreach (var symbol in previouslyLit)
        {
            if (!nowLit.Contains(symbol))
                symbol.SetLitAmount(0f);
        }

        previouslyLit = nowLit;

    }

}