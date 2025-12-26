// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:0,bdst:0,dpts:2,wrdp:True,dith:0,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:True,aust:True,igpj:False,qofs:0,qpre:2,rntp:3,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:9361,x:33316,y:32669,varname:node_9361,prsc:2|emission-9674-OUT,clip-1137-OUT;n:type:ShaderForge.SFN_Tex2d,id:2754,x:32804,y:32466,ptovrint:False,ptlb:TEX,ptin:_TEX,varname:node_6069,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-8148-OUT;n:type:ShaderForge.SFN_Tex2d,id:6086,x:32705,y:33027,ptovrint:False,ptlb:Mask,ptin:_Mask,varname:node_9683,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-8587-OUT;n:type:ShaderForge.SFN_Color,id:9326,x:32765,y:32695,ptovrint:False,ptlb:Color,ptin:_Color,varname:node_7299,prsc:2,glob:False,taghide:False,taghdr:True,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5,c2:0.5,c3:0.5,c4:1;n:type:ShaderForge.SFN_Multiply,id:905,x:33037,y:32569,varname:node_905,prsc:2|A-2754-RGB,B-9326-RGB,C-9281-RGB,D-9281-A,E-2754-A;n:type:ShaderForge.SFN_Divide,id:1137,x:33012,y:33062,varname:node_1137,prsc:2|A-6086-R,B-5208-OUT;n:type:ShaderForge.SFN_Slider,id:5208,x:32711,y:33279,ptovrint:False,ptlb:Mask_Range,ptin:_Mask_Range,varname:node_1992,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:3;n:type:ShaderForge.SFN_VertexColor,id:9281,x:32822,y:32854,varname:node_9281,prsc:2;n:type:ShaderForge.SFN_TexCoord,id:1449,x:32350,y:32516,varname:node_1449,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Add,id:8148,x:32526,y:32614,varname:node_8148,prsc:2|A-1449-UVOUT,B-2497-OUT;n:type:ShaderForge.SFN_Multiply,id:2497,x:32398,y:32767,varname:node_2497,prsc:2|A-1405-T,B-4719-OUT;n:type:ShaderForge.SFN_Append,id:4719,x:32144,y:32785,varname:node_4719,prsc:2|A-9087-OUT,B-588-OUT;n:type:ShaderForge.SFN_Slider,id:9087,x:31723,y:32670,ptovrint:False,ptlb:U,ptin:_U,varname:node_9976,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:-5,cur:0,max:5;n:type:ShaderForge.SFN_Slider,id:588,x:31690,y:32809,ptovrint:False,ptlb:V,ptin:_V,varname:node_872,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:-5,cur:0,max:5;n:type:ShaderForge.SFN_Time,id:1405,x:32052,y:32549,varname:node_1405,prsc:2;n:type:ShaderForge.SFN_TexCoord,id:7542,x:32216,y:33069,varname:node_7542,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Add,id:8587,x:32476,y:33144,varname:node_8587,prsc:2|A-7542-UVOUT,B-1086-OUT;n:type:ShaderForge.SFN_Multiply,id:1086,x:32348,y:33297,varname:node_1086,prsc:2|A-2166-T,B-459-OUT;n:type:ShaderForge.SFN_Append,id:459,x:32094,y:33315,varname:node_459,prsc:2|A-5738-OUT,B-4003-OUT;n:type:ShaderForge.SFN_Slider,id:5738,x:31673,y:33200,ptovrint:False,ptlb:U_Mask,ptin:_U_Mask,varname:_U_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:-5,cur:0,max:5;n:type:ShaderForge.SFN_Slider,id:4003,x:31640,y:33339,ptovrint:False,ptlb:V_Mask,ptin:_V_Mask,varname:_V_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:-5,cur:0,max:5;n:type:ShaderForge.SFN_Time,id:2166,x:32002,y:33079,varname:node_2166,prsc:2;n:type:ShaderForge.SFN_Multiply,id:9674,x:33132,y:32701,varname:node_9674,prsc:2|A-905-OUT,B-9326-A;proporder:2754-9326-9087-588-6086-5208-5738-4003;pass:END;sub:END;*/

Shader "Shader Forge/Add_DisUV" {
    Properties {
        _TEX ("TEX", 2D) = "white" {}
        [HDR]_Color ("Color", Color) = (0.5,0.5,0.5,1)
        _U ("U", Range(-5, 5)) = 0
        _V ("V", Range(-5, 5)) = 0
        _Mask ("Mask", 2D) = "white" {}
        _Mask_Range ("Mask_Range", Range(0, 3)) = 0
        _U_Mask ("U_Mask", Range(-5, 5)) = 0
        _V_Mask ("V_Mask", Range(-5, 5)) = 0
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend One One
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles 
            #pragma target 3.0
            uniform sampler2D _TEX; uniform float4 _TEX_ST;
            uniform sampler2D _Mask; uniform float4 _Mask_ST;
            uniform float4 _Color;
            uniform float _Mask_Range;
            uniform float _U;
            uniform float _V;
            uniform float _U_Mask;
            uniform float _V_Mask;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
                UNITY_FOG_COORDS(1)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.pos = UnityObjectToClipPos( v.vertex );
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float4 node_2166 = _Time;
                float2 node_8587 = (i.uv0+(node_2166.g*float2(_U_Mask,_V_Mask)));
                float4 _Mask_var = tex2D(_Mask,TRANSFORM_TEX(node_8587, _Mask));
                clip((_Mask_var.r/_Mask_Range) - 0.5);
////// Lighting:
////// Emissive:
                float4 node_1405 = _Time;
                float2 node_8148 = (i.uv0+(node_1405.g*float2(_U,_V)));
                float4 _TEX_var = tex2D(_TEX,TRANSFORM_TEX(node_8148, _TEX));
                float3 emissive = ((_TEX_var.rgb*_Color.rgb*i.vertexColor.rgb*i.vertexColor.a*_TEX_var.a)*_Color.a);
                float3 finalColor = emissive;
                fixed4 finalRGBA = fixed4(finalColor,1);
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode"="ShadowCaster"
            }
            Offset 1, 1
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCASTER
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles 
            #pragma target 3.0
            uniform sampler2D _Mask; uniform float4 _Mask_ST;
            uniform float _Mask_Range;
            uniform float _U_Mask;
            uniform float _V_Mask;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float2 uv0 : TEXCOORD1;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos( v.vertex );
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                float4 node_2166 = _Time;
                float2 node_8587 = (i.uv0+(node_2166.g*float2(_U_Mask,_V_Mask)));
                float4 _Mask_var = tex2D(_Mask,TRANSFORM_TEX(node_8587, _Mask));
                clip((_Mask_var.r/_Mask_Range) - 0.5);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
