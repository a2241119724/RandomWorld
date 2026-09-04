// 水面波光（海洋水格 overlay 层专用，TileMap 水波 overlay TilemapRenderer 挂载）。
// 主 chunk tilemap 的水格照常渲染（RuleTile 邻居评估完整），本层重复放同一水 tile
// 只为取其贴图 alpha 作为波光 mask，叠加 additive 微光 —— 不换素材、不动碰撞。
// 波光 = 两层反向滚动正弦干涉（世界坐标，跨 chunk 连续）+ hash 微闪烁（星光感）。
// 参数保守：静水微光优先，不抢戏。Blend One One 纯加法，与主层水色叠加无顺序依赖。
Shader "Custom/WaterShimmer"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _ShimmerColor("Shimmer Color", Color) = (0.55, 0.78, 0.9, 1)
        _Intensity("Shimmer Intensity", Range(0, 1)) = 0.18
        _WaveDir1("Wave 1 Direction (xy)", Vector) = (0.35, 0.93, 0, 0)
        _WaveFreq1("Wave 1 Frequency (rad per world unit)", Float) = 0.6
        _WaveSpeed1("Wave 1 Speed (rad/s)", Float) = 0.45
        _WaveDir2("Wave 2 Direction (xy)", Vector) = (-0.41, 0.91, 0, 0)
        _WaveFreq2("Wave 2 Frequency (rad per world unit)", Float) = 0.9
        _WaveSpeed2("Wave 2 Speed (rad/s)", Float) = 0.6
        _SparkleAmount("Sparkle Amount", Range(0, 1)) = 0.35
        _SparkleScale("Sparkle Cell Size (world units)", Float) = 0.8
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
            Name "Additive"

            Blend One One
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            CBUFFER_START(UnityPerMaterial)
                half4 _ShimmerColor;
                float _Intensity;
                float4 _WaveDir1;
                float _WaveFreq1;
                float _WaveSpeed1;
                float4 _WaveDir2;
                float _WaveFreq2;
                float _WaveSpeed2;
                float _SparkleAmount;
                float _SparkleScale;
            CBUFFER_END

            // TilemapRenderer 逐 chunk 把 sprite atlas 绑到 _MainTex（纹理引用不进 UnityPerMaterial）
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;       // Tilemap 顶点色（保持链路，正常为白）
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

            float Hash21(float2 p)
            {
                p = frac(p * float2(233.34, 851.73));
                p += dot(p, p + 23.45);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 水贴图 alpha 作 mask：波光只出现在水面纹理处，海岸线自然收边
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a * i.color.a;

                float2 p = i.positionWS.xy;

                // 两层反向滚动正弦干涉：缓变亮暗带交织，静水涌动感（跨 chunk 无缝）
                float w1 = sin(dot(p, _WaveDir1.xy) * _WaveFreq1 + _Time.y * _WaveSpeed1);
                float w2 = sin(dot(p, _WaveDir2.xy) * _WaveFreq2 - _Time.y * _WaveSpeed2);
                float interf = 0.5 + 0.5 * w1 * w2;

                // hash 微闪烁：少量格子随机亮起（0.5s 换一批），星光粼粼
                float sparkle = pow(Hash21(floor(p / _SparkleScale) + floor(_Time.y * 2.0) * 13.7), 12.0);

                float shimmer = saturate(interf * 0.75 + sparkle * _SparkleAmount);
                return half4(_ShimmerColor.rgb * shimmer * _Intensity * mask, 0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
