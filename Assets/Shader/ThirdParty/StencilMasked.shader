Shader "Ryan/StencilMasked"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {} // Define the texture property
    }

        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off
        ZWrite On
        ZTest LEqual

        // Stencil settings
        Stencil
        {
            Ref 1         // Reference value (same as the mask)
            Comp Equal    // Only render where the stencil value is equal to the reference
            Pass Keep     // Keep the stencil value
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
                // Sample the texture using the UV coordinates
                fixed4 color = tex2D(_MainTex, i.uv);

            // Return the color sampled from the texture
            return color;
        }
        ENDCG
    }
    }
}
