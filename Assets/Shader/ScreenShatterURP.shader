Shader "Ryan/ScreenShatterURP"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Progress ("Progress 0..1", Range(0,1)) = 0
        _Explode  ("Explode Distance", Float) = 1
        _Spin     ("Spin Radians", Float) = 6.28318
        _Jitter   ("Jitter Distance", Float) = 0.5
        _CenterUV ("Center in UV", Vector) = (0.5, 0.5, 0, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ScreenShatter"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Progress;
                float _Explode;
                float _Spin;
                float _Jitter;
                float4 _CenterUV;
                float4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float2 uv2        : TEXCOORD1; // shard id
                float2 uv3        : TEXCOORD2; // shard center uv
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  alpha      : TEXCOORD1;
            };

            float hash11(float x){ return frac(sin(x * 12.9898) * 43758.5453); }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float id = v.uv2.x;
                float r0 = hash11(id);
                float r1 = hash11(id + 17.0);
                float r2 = hash11(id + 41.0);

                float explodeAmt = _Explode * _Progress;
                float2 dirUV = normalize((v.uv3 - _CenterUV.xy) + 1e-4);
                float angle = _Spin * _Progress * (r0 * 2.0 - 1.0);
                float s = sin(angle);
                float c = cos(angle);

                float2 p = v.positionOS.xy; // [-1,1]
                float2 pivot = float2(v.uv3.x * 2.0 - 1.0, v.uv3.y * 2.0 - 1.0);

                p -= pivot;
                float2 pr = float2(c * p.x - s * p.y, s * p.x + c * p.y);
                p = pr + pivot;

                float2 explodeXY = dirUV * explodeAmt;
                float2 jitterXY = float2(r1 - 0.5, r2 - 0.5) * _Jitter * _Progress;

                o.positionCS = float4(p + explodeXY + jitterXY, 0, 1);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.alpha = 1.0;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                col.a *= i.alpha;
                return col;
            }
            ENDHLSL
        }
    }
}
