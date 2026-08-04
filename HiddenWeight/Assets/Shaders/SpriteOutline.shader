Shader "Hidden Weight/SpriteOutline"
{
    // 스프라이트 픽셀은 그대로 두고, 알파가 비어 있는 자리 중 이웃(8방향)에 불투명 픽셀이
    // 있으면 그 자리를 외곽선 색으로 칠한다. 원본 PNG를 다시 뽑거나 손대지 않고 실루엣만
    // 밝혀서, 잔재 지역의 "어둡고 무거운 앰버" 톤은 그대로 두면서 가독성만 올린다.
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0.86, 0.78, 0.62, 1)
        _OutlineWidth ("Outline Width (texel)", Range(0, 6)) = 2.2
        // 안쪽 픽셀을 얼마나 남길지. 1이면 원본 그대로(기존 적 실루엣 용도)이고, 0에 가까우면
        // 외곽선만 남아 "무엇의 형태인지만 알려주는" 실루엣이 된다(예지 고스트 용도).
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
            "IgnoreProjector"="True"
        }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float4 _Color;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _FillAlpha;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                // 이미 불투명한 픽셀은 원본 그대로 — 외곽선은 "빈 자리" 쪽에만 그린다.
                if (c.a < 0.95)
                {
                    float2 texel = _MainTex_TexelSize.xy * _OutlineWidth;
                    float neighborAlpha = 0;
                    neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( texel.x,  0)).a;
                    neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-texel.x,  0)).a;
                    neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0,  texel.y)).a;
                    neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0, -texel.y)).a;
                    neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( texel.x,  texel.y)).a;
                    neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-texel.x,  texel.y)).a;
                    neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( texel.x, -texel.y)).a;
                    neighborAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-texel.x, -texel.y)).a;

                    if (neighborAlpha > 0.01)
                    {
                        float4 outline = _OutlineColor;
                        outline.a *= saturate(neighborAlpha) * IN.color.a;
                        return outline;
                    }
                }

                c.a *= _FillAlpha;
                return c;
            }
            ENDHLSL
        }
    }
}
