Shader "Unlit/SpriteOutline"
{
    Properties
    {
        _BaseMap("Base Map (Sprite Texture)", 2D) = "white" {}
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)  // Default outline color set to white
        _OutlineThickness("Outline Thickness", Range(0.0, 100.0)) = 5.0  // Maximum outline thickness
        _FadeDistance("Fade Distance", Range(0.0, 100.0)) = 3.0  // Maximum fade distance
        _BlendAmount("Blend Amount", Range(0.0, 1.0)) = 0.5  // Amount of alpha blending for the outline
        _GlowIntensity("Glow Intensity", Range(0.0, 100.0)) = 1.0  // Intensity of glow effect
        _SampleArea("Sampling Area", Range(1, 5)) = 2.0  // Size of the sampling area
        _EdgeSoftness("Edge Softness", Range(0.0, 10.0)) = 1.0  // Amount of edge softness
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
                float4 _OutlineColor;
                float _OutlineThickness;
                float _FadeDistance;
                float _BlendAmount;
                float _GlowIntensity;
                float _SampleArea;
                float _EdgeSoftness;  // New property for edge softness

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
                    float softnessOffset = _EdgeSoftness / _ScreenParams.x; // Adjust softness based on screen size
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

                    // Calculate offsets based on thickness and screen aspect ratio
                    float2 offset = _OutlineThickness / _ScreenParams.xy;

                    // Initialize alpha variables for outline detection
                    float alphaSum = 0.0;

                    // Sample surrounding pixels for outline detection
                    for (int x = -softnessRange; x <= softnessRange; x++)
                    {
                        for (int y = -softnessRange; y <= softnessRange; y++)
                        {
                            if (x == 0 && y == 0) continue; // Skip center pixel
                            float sampleAlpha = _BaseMap.Sample(sampler_BaseMap, i.uv + float2(x, y) * offset).a; // Use Sample method
                            alphaSum += sampleAlpha;
                        }
                    }

                    // Determine if the pixel is an edge or fully transparent
                    float outlineAlpha = 0.0;
                    if (softenedColor.a <= 0.0 && alphaSum > 0.0)
                    {
                        outlineAlpha = smoothstep(0.0, _FadeDistance, alphaSum);
                        outlineAlpha = outlineAlpha * _BlendAmount; // Apply blend amount
                        return half4(_OutlineColor.rgb * _GlowIntensity, outlineAlpha);
                    }

                    // Return the softened sprite color if not transparent
                    return softenedColor;
                }
                ENDHLSL
            }
        }
            FallBack Off
}