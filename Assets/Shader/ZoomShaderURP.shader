Shader "Ryan/ZoomShaderURP"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _Progress ("Progress 0..1", Range(0,1)) = 0
        _Zoom     ("Zoom Strength", Float) = 1
        _Spin     ("Spin Radians", Float) = 6.28318
        _Smudge   ("Smudge Strength", Float) = 0.5
        _CenterUV ("Center in UV", Vector) = (0.5, 0.5, 0, 0)

        // Burn effect (Photoshop-like darkening)
        _BurnStrength   ("Burn Strength (0..1)", Range(0,1)) = 0
        _BurnRadius     ("Burn Radius", Range(0,1)) = 0.45
        _BurnFeather    ("Burn Feather", Range(0,1)) = 0.35
        _BurnMultiplier ("Burn Multiplier", Range(0,4)) = 2
        _BurnGammaScale ("Burn Gamma Scale", Range(0,8)) = 2

        // Inversion
        _InvertStrength ("Invert Strength (0..1)", Range(0,1)) = 1
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
            Name "ZoomShader"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Progress;
                float _Zoom;
                float _Spin;
                float _Smudge;
                float4 _CenterUV;
                float4 _MainTex_ST;

                // Burn params
                float _BurnStrength;
                float _BurnRadius;
                float _BurnFeather;
                float _BurnMultiplier;
                float _BurnGammaScale;

                // Invert
                float _InvertStrength;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            float2 rotate2D(float2 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = float4(v.positionOS.xy, 0, 1);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                // If ScreenGrabber already flips Y, remove this flip to avoid double-flip.
                uv.y = 1.0 - uv.y;

                // Progress-driven zoom and spin
                float angle = _Spin * _Progress;
                float zoom  = 1.0 + max(0.0, _Zoom) * _Progress;

                float2 center = _CenterUV.xy;
                float2 delta  = uv - center;

                // Inverse mapping for sampling
                float2 rotated = rotate2D(delta, -angle);
                float2 baseUV  = center + (rotated / zoom);

                // Radial smudge
                const int Samples = 8;
                float2 dir = normalize(rotated + 1e-5);
                float smudgeLen = _Smudge * _Progress * 0.05;

                half4 acc = 0;
                float wsum = 0;
                [unroll]
                for (int k = 0; k < Samples; k++)
                {
                    float t = (Samples <= 1) ? 0.0 : (float)k / (Samples - 1);
                    float w = 1.0 - t;
                    float2 tapUV = baseUV + dir * (t * smudgeLen);
                    acc += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, tapUV) * w;
                    wsum += w;
                }
                half4 col = (wsum > 0) ? (acc / wsum) : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, baseUV);

                // Progressive inversion (before burn-to-black so final blackout stays black)
                float inv = saturate(_InvertStrength * _Progress);
                col.rgb = lerp(col.rgb, 1.0 - col.rgb, inv);

                // Burn effect: midtone-focused darken + guaranteed fade to black
                if (_BurnStrength > 0.0)
                {
                    float r = length(uv - center);
                    float feather = max(_BurnFeather, 1e-5);
                    float mask = 1.0 - smoothstep(_BurnRadius, _BurnRadius + feather, r);

                    float burnFactor = saturate(_BurnStrength * _Progress * _BurnMultiplier);

                    float radialBurn = burnFactor * mask;
                    float expo = 1.0 + (radialBurn * _BurnGammaScale);
                    col.rgb = pow(max(col.rgb, 1e-4), expo);

                    // Global fade to black for a clean transition
                    col.rgb = lerp(col.rgb, 0.0, burnFactor);
                }

                col.a = 1;
                return col;
            }
            ENDHLSL
        }
    }
}