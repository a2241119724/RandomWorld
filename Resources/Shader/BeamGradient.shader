// URP unlit 程序化光柱（装备掉落光束专用）。
// 无贴图：水平高斯衰减 × 底亮顶淡 × 上升流光条纹全部在 fragment 程序化生成，
// 替代原 CPU 逐像素 GenerateBeamTexture（每稀有度一张 128×256）。
// 呼吸脉冲在顶点着色器对 positionOS.x 缩放（与原 EquipmentBeam.Update 的 localScale.x 脉冲数学等价），
// 相位按 pivot 世界位置 hash 打散（同 Custom/Sprite-Lit-Sway 的做法），消除逐实例 MonoBehaviour。
// 材质由 EquipmentBeamManager 按稀有度缓存（≤6 份），同稀有度光柱共享 → SRP Batcher 合批。
Shader "Custom/BeamGradient"
{
    Properties
    {
        _BeamColor("Beam Color (a=base alpha)", Color) = (1,1,1,0.5)
        _Falloff("Horizontal Gaussian Falloff", Float) = 12.0
        _VFadePow("Vertical Fade Power", Float) = 0.6
        _PulseSpeed("Pulse Speed (rad/s)", Float) = 2.0
        _PulseAmp("Pulse Amplitude", Float) = 0.08
        _StripeFreq("Rising Stripe Count", Float) = 2.5
        _StripeSpeed("Rising Stripe Speed (cycles/s)", Float) = 0.35
        _StripeBoost("Stripe Brightness Boost", Range(0, 1)) = 0.35
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

            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            CBUFFER_START(UnityPerMaterial)
                half4 _BeamColor;
                float _Falloff;
                float _VFadePow;
                float _PulseSpeed;
                float _PulseAmp;
                float _StripeFreq;
                float _StripeSpeed;
                float _StripeBoost;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                // 呼吸相位 = 光柱 pivot 世界位置 hash，逐柱打散（照抄 SpriteLitSway.ApplySwayOffset 的 hash）
                float2 pivot = TransformObjectToWorld(float3(0, 0, 0)).xy;
                float phase = frac(sin(dot(pivot, float2(12.9898, 78.2333))) * 43758.5453) * 6.2831853;
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed + phase) * _PulseAmp;
                // mesh x∈[-0.5,0.5]、pivot 底部中心：positionOS.x*pulse ≡ 原 localScale.x*pulse
                o.positionCS = TransformObjectToHClip(float3(v.positionOS.x * pulse, v.positionOS.y, v.positionOS.z));
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 水平高斯衰减（CPU 版 hFade=exp(-falloff*dx*dx)，dx=(x-halfW)/halfW ≡ uv.x*2-1）
                float dx = i.uv.x * 2.0 - 1.0;
                float hFade = exp(-_Falloff * dx * dx);
                // 底亮顶淡（CPU 版 vFade=pow(1-y/h, 0.6)）
                float vFade = pow(saturate(1.0 - i.uv.y), _VFadePow);
                // 上升流光条纹：随时间上移的亮带（y*freq - t*speed → 条纹向 y+ 漂移），横向微弯，顶端渐隐
                float stripe = 0.5 + 0.5 * sin((i.uv.y * _StripeFreq - _Time.y * _StripeSpeed
                             + sin(i.uv.x * 6.2831853) * 0.3) * 6.2831853);

                half4 col = _BeamColor;
                col.rgb *= 1.0 + _StripeBoost * stripe * (1.0 - i.uv.y);
                col.a *= hFade * vFade;
                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
