Shader "Ryan/SpriteGrassMask"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Surface color source for blades
        _SurfaceTex ("Surface", 2D) = "white" {}

        // Grass look
        _GrassColor ("Grass Color (tint)", Color) = (1,1,1,1)
        _Taper ("Blade Taper 0..1", Range(0.0, 1.0)) = 1.0

        // Pixel-sized blades
        _BladeHeightPx ("Blade Height (pixels)", Range(1.0, 256.0)) = 10.0
        _BladeWidthPx  ("Blade Width (pixels)",  Range(0.1, 32.0))  = 0.8

        // Placement
        _CellsX ("Cells X (density)", Range(32, 4096)) = 1024
        _CellsY ("Cells Y (rows)",   Range(1, 4096))   = 1024
        _Coverage ("Blade chance per cell", Range(0.0, 1.0)) = 0.55
        _Seed ("Seed", Float) = 1337.0

        // Mask control
        _MaskBlackThresh ("Mask Alpha Threshold", Range(0.0, 0.2)) = 0.06
        _BaseBlend ("Show Base Sprite 0..1", Range(0.0, 1.0)) = 0.0
        _RowJitter ("Row Jitter (0..1 of cell)", Range(0.0, 1.0)) = 0.5

        // Wind
        _WindStrength ("Wind Strength", Range(0.0, 0.2)) = 0.05
        _WindFreq     ("Wind Frequency", Range(0.1, 10.0)) = 2.0
        _WindDetail   ("Wind Detail", Range(0.0, 5.0)) = 1.2

        // Orientation (UV-space, not screen-space)
        _NormalGrowBlend   ("Grow Toward Camera 0..1", Range(0.0, 1.0)) = 0.35
        _BillboardGrow     ("Billboard Growth 0..1", Range(0.0, 1.0)) = 1.0
        _NormalProjectDist ("Project Dist (wu)", Range(0.05, 2.0)) = 0.6
        _GrowthAngle       ("UV Growth Angle (deg)", Range(-180, 180)) = 90

        // Outline
        _OutlineEnabled ("Outline Enabled (0/1)", Float) = 0
        _OutlineWidthPx ("Outline Width (pixels)", Range(0.1, 8.0)) = 1.0
        _OutlineColor   ("Outline Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_SurfaceTex);
            SAMPLER(sampler_SurfaceTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;

                float4 _SurfaceTex_ST;

                float4 _GrassColor;
                float  _Taper;

                float  _BladeHeightPx;
                float  _BladeWidthPx;

                float  _CellsX;
                float  _CellsY;
                float  _Coverage;
                float  _Seed;

                float  _MaskBlackThresh;
                float  _BaseBlend;
                float  _RowJitter;

                float  _WindStrength;
                float  _WindFreq;
                float  _WindDetail;

                float  _NormalGrowBlend;
                float  _BillboardGrow;
                float  _NormalProjectDist;
                float  _GrowthAngle; // UV-space angle

                float  _OutlineEnabled;
                float  _OutlineWidthPx;
                float4 _OutlineColor;
            CBUFFER_END

            float4 _MainTex_TexelSize; // x=1/width, y=1/height

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                float4 color : COLOR;

                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
            };

            float hash11(float n)
            {
                n = frac(n * 0.1031);
                n *= n + 33.33;
                n *= n + n;
                return frac(n);
            }

            float2 hash21(float2 p)
            {
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.worldNormal = normalize(TransformObjectToWorldNormal(float3(0,0,1)));
                return o;
            }

            float PixelsToUV_UVSpace(float pixels, float2 unitDir)
            {
                // unitDir is a normalized direction in UV space.
                // One pixel in UV space along U is _MainTex_TexelSize.x; along V is _MainTex_TexelSize.y.
                // The UV length that corresponds to 1 pixel along this direction is:
                //    1 / length(unitDir / texelSize)
                float2 texel = _MainTex_TexelSize.xy;
                float perPixelUV = 1.0 / max(1e-6, length(unitDir / texel));
                return pixels * perPixelUV;
            }

            float bladeAt(
                float2 uv,
                float2 uvHeightDir,
                float2 uvPerpDir,
                int2 cell,
                float2 baseUVMask,
                float2 baseUVSurf,
                float time,
                float heightUVBase,
                float halfWidthUVBase,
                float outlineWidthUV,
                float2 gradXMask,
                float2 gradYMask,
                float2 gradXSurf,
                float2 gradYSurf,
                out float shade,
                out float3 albedo,
                out float outline)
            {
                float4 baseSamp = SAMPLE_TEXTURE2D_GRAD(_MainTex, sampler_MainTex, baseUVMask, gradXMask, gradYMask);

                float maskHeight01 = baseSamp.a;
                if (maskHeight01 <= _MaskBlackThresh)
                {
                    shade = 0.0;
                    albedo = 0.0.xxx;
                    outline = 0.0;
                    return 0.0;
                }

                float3 surfCol = SAMPLE_TEXTURE2D_GRAD(_SurfaceTex, sampler_SurfaceTex, baseUVSurf, gradXSurf, gradYSurf).rgb;

                float2 rnd = hash21(float2(cell) + _Seed);
                float var = lerp(0.85, 1.15, rnd.x);
                float height = heightUVBase * maskHeight01 * var;
                float halfWidth = halfWidthUVBase * lerp(0.9, 1.1, rnd.y);

                float2 d = uv - baseUVMask;
                float s = dot(d, uvHeightDir);
                if (s < 0.0 || s > height)
                {
                    shade = 0.0;
                    albedo = 0.0.xxx;
                    outline = 0.0;
                    return 0.0;
                }

                float t = time + rnd.x * 6.28318;
                float sway = sin(t * _WindFreq) + 0.5 * sin(t * (_WindFreq * 2.13 + 0.7 * _WindDetail));
                float tilt = sway * _WindStrength * (s / max(height, 1e-4));

                float lateral = dot(d, uvPerpDir) - tilt;

                float taper = lerp(1.0, 1.0 - _Taper, saturate(s / max(height, 1e-4)));
                float halfW = halfWidth * taper;

                float edge = smoothstep(halfW, halfW * 0.7, abs(lateral));
                if (edge <= 0.0)
                {
                    shade = 0.0;
                    albedo = 0.0.xxx;
                    outline = 0.0;
                    return 0.0;
                }

                // Outline mask near the blade edge (inside the blade)
                float innerStart = max(halfW - outlineWidthUV, 0.0);
                outline = smoothstep(innerStart, halfW, abs(lateral)); // 0 center -> 1 at edge

                shade = lerp(0.85, 1.0, saturate(s / max(height, 1e-4)));
                albedo = surfCol;
                return edge;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 baseCol = baseSample * i.color;
                float time = _Time.y;

                float2 dUVdx = ddx(i.uv);
                float2 dUVdy = ddy(i.uv);

                // UV-space growth direction (stable relative to the sprite UVs)
                float ang = radians(_GrowthAngle);
                float2 uvHeightDir = normalize(float2(cos(ang), sin(ang)));
                float2 uvPerpDir   = normalize(float2(-uvHeightDir.y, uvHeightDir.x));

                float heightUV    = PixelsToUV_UVSpace(_BladeHeightPx, uvHeightDir);
                float halfWidthUV = PixelsToUV_UVSpace(_BladeWidthPx * 0.5, uvPerpDir);
                float outlineWidthUV = (_OutlineEnabled > 0.5) ? PixelsToUV_UVSpace(_OutlineWidthPx, uvPerpDir) : 0.0;

                float2 stScaleMain  = _MainTex_ST.xy;
                float2 stOffsetMain = _MainTex_ST.zw;
                float2 uvLocal = (i.uv - stOffsetMain) / max(1e-6, stScaleMain);

                float2 stScaleSurf  = _SurfaceTex_ST.xy;
                float2 stOffsetSurf = _SurfaceTex_ST.zw;

                float2 gradScale = stScaleSurf / max(1e-6, stScaleMain);
                float2 dUVdxSurf = dUVdx * gradScale;
                float2 dUVdySurf = dUVdy * gradScale;

                float2 grid = float2(_CellsX, _CellsY);
                float2 cellSizeLocal = 1.0 / grid;
                int2 cell = int2(floor(uvLocal * grid));

                float alpha = 0.0;
                float shade = 0.0;
                float3 bladeAlbedo = 0.0.xxx;
                float outlineMask = 0.0;

                [unroll]
                for (int dy = -1; dy <= 1; ++dy)
                {
                    for (int dx = -1; dx <= 1; ++dx)
                    {
                        int2 c = int2(cell.x + dx, cell.y + dy);
                        if (c.x < 0 || c.x >= (int)_CellsX || c.y < 0 || c.y >= (int)_CellsY) continue;

                        float chance = hash11(dot(float2(c), float2(17.23, 91.07)) + _Seed * 0.137);
                        if (chance > _Coverage) continue;

                        float2 rnd = hash21(float2(c) + _Seed);

                        float jitterY = (rnd.y - 0.5) * _RowJitter;
                        float2 baseLocal = (float2(c) + float2(rnd.x, jitterY)) * cellSizeLocal;

                        float2 baseMaskUV = baseLocal * stScaleMain + stOffsetMain;
                        float2 baseSurfUV = baseLocal * stScaleSurf + stOffsetSurf;

                        float localShade; float3 localAlbedo; float localOutline;
                        float cov = bladeAt(i.uv, uvHeightDir, uvPerpDir, c, baseMaskUV, baseSurfUV, time, heightUV, halfWidthUV, outlineWidthUV, dUVdx, dUVdy, dUVdxSurf, dUVdySurf, localShade, localAlbedo, localOutline);

                        if (cov > alpha)
                        {
                            alpha = cov;
                            shade = localShade;
                            bladeAlbedo = localAlbedo;
                            outlineMask = localOutline;
                        }
                    }
                }

                if (_BaseBlend > 0.001)
                {
                    float baseVisible = step(_MaskBlackThresh, baseSample.a);
                    baseCol.a *= baseVisible * _BaseBlend;
                }
                else
                {
                    baseCol = float4(0,0,0,0);
                }

                if (alpha > 0.0)
                {
                    float3 grassRGB = bladeAlbedo * _GrassColor.rgb * i.color.rgb;
                    grassRGB *= shade;

                    // Apply outline as a color mix near edges
                    if (_OutlineEnabled > 0.5 && outlineMask > 0.0)
                    {
                        float outlineIntensity = saturate(outlineMask * _OutlineColor.a);
                        grassRGB = lerp(grassRGB, _OutlineColor.rgb, outlineIntensity);
                    }

                    float outA = saturate(baseCol.a + alpha * (1.0 - baseCol.a));
                    float3 outRGB = lerp(baseCol.rgb, grassRGB, alpha);
                    return float4(outRGB, outA);
                }

                return baseCol;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
