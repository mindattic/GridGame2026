Shader "Ryan/NorthDepthBlurURP"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _StartY ("Start Y (no blur below)", Range(0,1)) = 0.6
        _MaxRadius ("Max Blur Radius (pixels)", Range(0,10)) = 3
        _Exponent ("Falloff Exponent", Range(0.1, 5)) = 1.5
        _Noise ("Dither (0..1)", Range(0,1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend One Zero // overwrite

        Pass
        {
            Name "NorthDepthBlur"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _StartY;
                float  _MaxRadius;
                float  _Exponent;
                float  _Noise;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize; // x=1/width, y=1/height

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = float4(v.positionOS.xy, 0, 1);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float rand2(float2 p)
            {
                // Cheap hash-based noise for dithering
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                // If your grab is already flipped, remove this
                uv.y = 1.0 - uv.y;

                // Blur factor grows toward the top of the screen (north)
                float t = saturate((uv.y - _StartY) / max(1e-5, (1.0 - _StartY)));
                t = pow(t, _Exponent);

                // Convert radius in pixels -> UV using texel size
                float rPx = _MaxRadius * t;

                // Small stochastic jitter to reduce banding
                float jitter = (rand2(uv * _ScreenParams.xy) - 0.5) * _Noise;

                // Optimized 9-tap Gaussian samples, applied on X and Y and lightly on diagonals in one pass
                const float w0 = 0.227027f;   // center
                const float w1 = 0.316216f;   // near
                const float w2 = 0.070270f;   // far
                const float o1 = 1.384615f;   // near offset
                const float o2 = 3.230769f;   // far offset

                float2 texel = _MainTex_TexelSize.xy * (1.0 + jitter);

                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * w0;

                // Horizontal
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texel.x * o1 * rPx, 0)) * w1;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(texel.x * o1 * rPx, 0)) * w1;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texel.x * o2 * rPx, 0)) * w2;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(texel.x * o2 * rPx, 0)) * w2;

                // Vertical
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, texel.y * o1 * rPx)) * w1;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, texel.y * o1 * rPx)) * w1;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, texel.y * o2 * rPx)) * w2;
                c += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, texel.y * o2 * rPx)) * w2;

                // Light diagonals to fake a more isotropic kernel
                float diag = 0.7071 * rPx;
                half4 cd = 0;
                cd += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texel * float2( o1*diag,  o1*diag)) * w1;
                cd += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texel * float2(-o1*diag,  o1*diag)) * w1;
                cd += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texel * float2( o1*diag, -o1*diag)) * w1;
                cd += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texel * float2(-o1*diag, -o1*diag)) * w1;
                cd += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texel * float2( o2*diag,  o2*diag)) * w2;
                cd += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texel * float2(-o2*diag,  o2*diag)) * w2;
                cd += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texel * float2( o2*diag, -o2*diag)) * w2;
                cd += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texel * float2(-o2*diag, -o2*diag)) * w2;

                // Mix a small portion of diagonals to avoid excessive softness
                c = lerp(c, cd, 0.25);

                c.a = 1;
                return c;
            }
            ENDHLSL
        }
    }
}
