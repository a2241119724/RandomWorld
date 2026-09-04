// 放置预览呼吸（建造红/绿格子预览专用，IsAvailableMap 的 TilemapRenderer 挂载）。
// 零逻辑改动：逐格绿(可放)/红(不可放)判定色仍由 Tilemap 顶点色承载（ShowRect 的 SetColor），
// 本 shader 只叠加视觉 —— 整体呼吸脉动（alpha）+ 上升微光带（世界 y 扫描，跨 chunk 连续）。
// unlit：预览是覆盖层性质，不受光照/夜色影响，夜间建造也清晰。
Shader "Custom/PlacingPreview"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _PulseSpeed("Pulse Speed (rad/s)", Float) = 3.0
        _MinAlpha("Breath Min Alpha", Range(0.1, 1)) = 0.45
        _ScanBoost("Scan Brightness Boost", Range(0, 1)) = 0.35
        _ScanSpeed("Scan Speed (world units/s)", Float) = 2.0
        _ScanWaveLen("Scan Wavelength (world units)", Float) = 4.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "Unlit"

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            CBUFFER_START(UnityPerMaterial)
                float _PulseSpeed;
                float _MinAlpha;
                float _ScanBoost;
                float _ScanSpeed;
                float _ScanWaveLen;
            CBUFFER_END

            // TilemapRenderer 逐 chunk 把 sprite atlas 绑到 _MainTex（纹理引用不进 UnityPerMaterial）
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;       // Tilemap 逐格判定色：绿=可放 / 红=不可放
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;

                // 呼吸脉动：alpha 在 [_MinAlpha, 1] 正弦摆动，预览整体"活着"
                float breath = _MinAlpha + (1.0 - _MinAlpha) * (0.5 + 0.5 * sin(_Time.y * _PulseSpeed));
                col.a *= breath;

                // 上升微光带：世界 y 缓慢上扫，能量流动感（波长 4 格，保守亮度）
                float scan = 0.5 + 0.5 * sin((i.positionWS.y - _Time.y * _ScanSpeed) * 6.2831853 / _ScanWaveLen);
                col.rgb *= 1.0 + _ScanBoost * scan;

                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
