Shader "Custom/Symbol_UVGlow"
{
    Properties
    {
        _MainTex ("Symbol Texture (use Alpha)", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0.7, 0, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0,3)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend One One
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _GlowColor;
            float _GlowIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                // 用貼圖Alpha當遮罩，強度=0時完全不疊加顏色（相加模式下等於看不到）
                fixed3 finalColor = _GlowColor.rgb * _GlowIntensity * tex.a;
                return fixed4(finalColor, 1);
            }
            ENDCG
        }
    }
}