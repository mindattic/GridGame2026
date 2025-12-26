Shader "SpritePan" {
    Properties{
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
            // _MainTex_ST is automatically provided for scaling and default offset.
            // Here we add an additional offset property.
            _MainTexOffset("Texture Offset", Vector) = (0,0,0,0)
    }
        SubShader{
            Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
            LOD 100

            Cull Off
            Lighting Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t {
                    float4 vertex : POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                sampler2D _MainTex;
                fixed4 _Color;
                float4 _MainTex_ST;    // contains scale (xy) and default offset (zw)
                float4 _MainTexOffset; // new offset property; use XY components

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    // Calculate UVs: first apply scale/offset from _MainTex_ST then add our custom offset.
                    o.texcoord = v.texcoord * _MainTex_ST.xy + _MainTex_ST.zw + _MainTexOffset.xy;
                    o.color = v.color * _Color;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 texColor = tex2D(_MainTex, i.texcoord);
                    return texColor * i.color;
                }
                ENDCG
            }
        }
            Fallback "Sprites/Default"
}
