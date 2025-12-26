Shader "Ryan/Invert-Color"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
    }
        SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION; // Use float4 to match clip space transformations
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Declare the texture and sampler for URP
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS); // Expecting a float4
                OUT.uv = IN.uv; // No need for _MainTex_ST, use the UVs directly
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the texture
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Invert the RGB channels
                color.rgb = 1.0 - color.rgb;

                // Return the inverted color with original alpha
                return color;
            }
            ENDHLSL
        }
    }
        FallBack "Universal Render Pipeline/Unlit"
}
