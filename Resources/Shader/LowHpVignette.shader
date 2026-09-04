// 低血量红晕（玩家专用）— UGUI 全屏 overlay。
// 屏幕四边随血量降低泛红 + 呼吸脉冲，_Intensity 由 LowHpVignetteUI 按血量驱动（0=隐藏）。
// 零贴图：边缘形状由 frag 内按 uv 到屏幕边缘的方形距离 smoothstep 程序化生成。
Shader "Custom/LowHpVignette"
{
    Properties
    {
        _Color("Edge Color", Color) = (0.68, 0.04, 0.04, 1)
        _Intensity("Intensity (0=隐藏)", Range(0, 1)) = 0
        _InnerEdge("Inner Edge (开始泛红的归一化距离)", Range(0.3, 0.95)) = 0.55
        _PulseSpeed("Pulse Speed (rad/s)", Float) = 2.4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Intensity;
            float _InnerEdge;
            float _PulseSpeed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                // 方形边缘距离：0=屏幕中心，1=屏幕边缘/四角（四边均匀泛红）
                float2 d = abs(IN.texcoord - 0.5) * 2.0;
                float dist = max(d.x, d.y);
                float vig = smoothstep(_InnerEdge, 1.0, dist);

                // 呼吸脉冲：强度随时间 ±25%
                float pulse = 0.75 + 0.25 * sin(_Time.y * _PulseSpeed);

                color.a *= vig * _Intensity * pulse;
                return color;
            }
        ENDCG
        }
    }
}
