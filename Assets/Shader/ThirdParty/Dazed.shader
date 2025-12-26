Shader "Unlit/Dazed"
{
    Properties
    {
        _BaseMap("Base Map (Sprite Texture)", 2D) = "white" {}
        _MinEdgeSoftness("Min Edge Softness", Range(0.0, 10.0)) = 0.0  // Minimum edge softness
        _MaxEdgeSoftness("Max Edge Softness", Range(0.0, 10.0)) = 8.0  // Maximum edge softness
        _PulseRate("Pulse Rate", Range(0.0, 10.0)) = 5.0  // Rate of pulsing (increased)
        _SampleArea("Sampling Area", Range(1, 10)) = 5.0  // Size of the sampling area
    }

        SubShader
        {
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
            LOD 200

            Pass
            {
                Tags { "LightMode" = "UniversalForward" }
                Blend SrcAlpha OneMinusSrcAlpha  // Enable transparency blending
                ZWrite Off  // Disable depth writing for transparency
                Cull Off  // Render both sides of the sprite

                HLSLPROGRAM
                #pragma target 4.5
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                Texture2D _BaseMap;
                SamplerState sampler_BaseMap;
                float _MinEdgeSoftness;
                float _MaxEdgeSoftness;
                float _PulseRate;
                float _SampleArea;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                half4 frag(v2f i) : SV_Target
                {
                    // Sample the sprite texture
                    half4 spriteColor = _BaseMap.Sample(sampler_BaseMap, i.uv);

                    // Apply edge softness by averaging neighboring pixels
                    float softnessOffset = (_MinEdgeSoftness + (_MaxEdgeSoftness - _MinEdgeSoftness) * (sin(_Time.y * _PulseRate) * 0.5 + 0.5)) / _ScreenParams.x; // Adjust softness based on screen size

                    half4 softenedColor = spriteColor;

                    // Sample surrounding pixels for softness
                    int softnessRange = (int)_SampleArea; // Get the sampling area size
                    for (int x = -softnessRange; x <= softnessRange; x++)
                    {
                        for (int y = -softnessRange; y <= softnessRange; y++)
                        {
                            if (x == 0 && y == 0) continue; // Skip center pixel
                            softenedColor += _BaseMap.Sample(sampler_BaseMap, i.uv + float2(x, y) * softnessOffset);
                        }
                    }

                    softenedColor /= (1 + 4 * softnessRange * softnessRange); // Normalize the softened color

                    // Maintain the original alpha value
                    softenedColor.a = spriteColor.a;

                    // Return the softened color
                    return softenedColor;
                }
                ENDHLSL
            }
        }
            FallBack Off
}
