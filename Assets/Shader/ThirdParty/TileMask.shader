Shader "Unlit/TileMask"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _StencilRef("Stencil Ref", Range(0,255)) = 1 // Exposing the _StencilRef property
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Stencil
        {
            Ref[_StencilRef]  // Use the _StencilRef property
            Comp Always
            Pass Replace
            ReadMask 255
            WriteMask 255
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
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
        FallBack "Diffuse"
}
