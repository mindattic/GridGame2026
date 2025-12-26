Shader "Ryan/StencilMask"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {} // The texture to use for masking
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque" }

        // Render settings to only affect the stencil buffer
        ColorMask 0
        ZWrite Off

        // Stencil settings
        Stencil
        {
            Ref 1           // Reference value
            Comp Always     // Always pass
            Pass Replace    // Replace the stencil buffer value with the reference value
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex; // The texture sampler

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the texture and use the alpha channel to determine the stencil effect
                fixed4 texColor = tex2D(_MainTex, i.uv);

            // Discard fragments with low alpha, so they don't write to the stencil buffer
            if (texColor.a < 0.1)
                discard;

            return texColor; // This won't render anything but allows stencil setting
        }
        ENDCG
    }
    }
}
