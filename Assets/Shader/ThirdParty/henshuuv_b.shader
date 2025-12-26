// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:0,bsrc:3,bdst:7,dpts:2,wrdp:False,dith:0,atcv:False,rfrpo:True,rfrpn:Refraction,coma:15,ufog:True,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:False,fnsp:False,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:9361,x:33159,y:32825,varname:node_9361,prsc:2|emission-6754-OUT,custl-3931-OUT,alpha-482-OUT;n:type:ShaderForge.SFN_Tex2d,id:575,x:32278,y:32641,ptovrint:False,ptlb:tex,ptin:_tex,varname:_MainTex,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-8476-OUT;n:type:ShaderForge.SFN_Multiply,id:6754,x:32539,y:32836,varname:node_6754,prsc:2|A-2131-OUT,B-4934-A,C-2121-RGB,D-8098-RGB,E-2121-A;n:type:ShaderForge.SFN_VertexColor,id:4934,x:32187,y:32830,varname:node_4934,prsc:2;n:type:ShaderForge.SFN_Color,id:2121,x:32228,y:32986,ptovrint:True,ptlb:Color,ptin:_TintColor,varname:_TintColor,prsc:2,glob:False,taghide:False,taghdr:True,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5,c2:0.5,c3:0.5,c4:1;n:type:ShaderForge.SFN_TexCoord,id:9108,x:31309,y:32632,varname:node_9108,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Tex2d,id:8098,x:32105,y:33297,ptovrint:False,ptlb:mask,ptin:_mask,varname:node_6158,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-2364-OUT;n:type:ShaderForge.SFN_Slider,id:1642,x:31067,y:32984,ptovrint:False,ptlb:U,ptin:_U,varname:node_4640,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:10;n:type:ShaderForge.SFN_Slider,id:1142,x:31067,y:33159,ptovrint:False,ptlb:v,ptin:_v,varname:node_1091,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:10;n:type:ShaderForge.SFN_Add,id:8476,x:31921,y:32673,varname:node_8476,prsc:2|A-8069-UVOUT,B-4159-OUT;n:type:ShaderForge.SFN_Multiply,id:4159,x:31766,y:32880,varname:node_4159,prsc:2|A-6741-T,B-9701-OUT;n:type:ShaderForge.SFN_Append,id:9701,x:31479,y:33085,varname:node_9701,prsc:2|A-1642-OUT,B-1142-OUT;n:type:ShaderForge.SFN_Time,id:6741,x:31507,y:32840,varname:node_6741,prsc:2;n:type:ShaderForge.SFN_Slider,id:4958,x:32321,y:33374,ptovrint:False,ptlb:liangdu,ptin:_liangdu,varname:node_4187,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:10;n:type:ShaderForge.SFN_Power,id:2131,x:32579,y:32632,varname:node_2131,prsc:2|VAL-575-RGB,EXP-6202-OUT;n:type:ShaderForge.SFN_Slider,id:6202,x:32215,y:32541,ptovrint:False,ptlb:duibi,ptin:_duibi,varname:node_5505,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:1,max:10;n:type:ShaderForge.SFN_Multiply,id:3931,x:32573,y:33046,varname:node_3931,prsc:2|A-6754-OUT,B-4958-OUT,C-4934-A,D-9955-RGB;n:type:ShaderForge.SFN_Color,id:9955,x:32635,y:33278,ptovrint:False,ptlb:glowcolor,ptin:_glowcolor,varname:node_6324,prsc:2,glob:False,taghide:False,taghdr:True,tagprd:False,tagnsco:False,tagnrm:False,c1:0.5,c2:0.5,c3:0.5,c4:1;n:type:ShaderForge.SFN_Rotator,id:8069,x:31590,y:32636,varname:node_8069,prsc:2|UVIN-9108-UVOUT,ANG-5433-OUT,SPD-4993-OUT;n:type:ShaderForge.SFN_Slider,id:4993,x:31150,y:32851,ptovrint:False,ptlb:UVrotator,ptin:_UVrotator,varname:node_9062,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:-5,cur:5,max:5;n:type:ShaderForge.SFN_Slider,id:5433,x:30905,y:32671,ptovrint:False,ptlb:jiao'dujiao'du,ptin:_jiaodujiaodu,varname:node_54,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:45,max:360;n:type:ShaderForge.SFN_TexCoord,id:7242,x:31241,y:33295,varname:node_7242,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Slider,id:354,x:30999,y:33647,ptovrint:False,ptlb:U_mask,ptin:_U_mask,varname:_U_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:10;n:type:ShaderForge.SFN_Slider,id:8892,x:30999,y:33822,ptovrint:False,ptlb:v_mask,ptin:_v_mask,varname:_v_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:0,max:10;n:type:ShaderForge.SFN_Add,id:2364,x:31853,y:33336,varname:node_2364,prsc:2|A-6120-UVOUT,B-6422-OUT;n:type:ShaderForge.SFN_Multiply,id:6422,x:31698,y:33543,varname:node_6422,prsc:2|A-8359-T,B-2185-OUT;n:type:ShaderForge.SFN_Append,id:2185,x:31411,y:33748,varname:node_2185,prsc:2|A-354-OUT,B-8892-OUT;n:type:ShaderForge.SFN_Time,id:8359,x:31439,y:33503,varname:node_8359,prsc:2;n:type:ShaderForge.SFN_Rotator,id:6120,x:31522,y:33299,varname:node_6120,prsc:2|UVIN-7242-UVOUT,ANG-6702-OUT,SPD-8945-OUT;n:type:ShaderForge.SFN_Slider,id:8945,x:31082,y:33514,ptovrint:False,ptlb:UVrotator_mask,ptin:_UVrotator_mask,varname:_UVrotator_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:-5,cur:5,max:5;n:type:ShaderForge.SFN_Slider,id:6702,x:30837,y:33334,ptovrint:False,ptlb:jiao'dujiao'du_mask,ptin:_jiaodujiaodu_mask,varname:_jiaodujiaodu_copy,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:45,max:360;n:type:ShaderForge.SFN_Multiply,id:482,x:32919,y:33084,varname:node_482,prsc:2|A-575-A,B-2121-A,C-4934-A;proporder:575-2121-8098-6202-4958-9955-1642-1142-4993-5433-354-8892-8945-6702;pass:END;sub:END;*/

Shader "Shader Forge/henshuuv_b" {
    Properties {
        _tex ("tex", 2D) = "white" {}
        [HDR]_TintColor ("Color", Color) = (0.5,0.5,0.5,1)
        _mask ("mask", 2D) = "white" {}
        _duibi ("duibi", Range(0, 10)) = 1
        _liangdu ("liangdu", Range(0, 10)) = 0
        [HDR]_glowcolor ("glowcolor", Color) = (0.5,0.5,0.5,1)
        _U ("U", Range(0, 10)) = 0
        _v ("v", Range(0, 10)) = 0
        _UVrotator ("UVrotator", Range(-5, 5)) = 5
        _jiaodujiaodu ("jiao'dujiao'du", Range(0, 360)) = 45
        _U_mask ("U_mask", Range(0, 10)) = 0
        _v_mask ("v_mask", Range(0, 10)) = 0
        _UVrotator_mask ("UVrotator_mask", Range(-5, 5)) = 5
        _jiaodujiaodu_mask ("jiao'dujiao'du_mask", Range(0, 360)) = 45
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma only_renderers d3d9 d3d11 glcore gles metal 
            #pragma target 3.0
            uniform sampler2D _tex; uniform float4 _tex_ST;
            uniform float4 _TintColor;
            uniform sampler2D _mask; uniform float4 _mask_ST;
            uniform float _U;
            uniform float _v;
            uniform float _liangdu;
            uniform float _duibi;
            uniform float4 _glowcolor;
            uniform float _UVrotator;
            uniform float _jiaodujiaodu;
            uniform float _U_mask;
            uniform float _v_mask;
            uniform float _UVrotator_mask;
            uniform float _jiaodujiaodu_mask;
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
////// Lighting:
////// Emissive:
                float node_8069_ang = _jiaodujiaodu;
                float node_8069_spd = _UVrotator;
                float node_8069_cos = cos(node_8069_spd*node_8069_ang);
                float node_8069_sin = sin(node_8069_spd*node_8069_ang);
                float2 node_8069_piv = float2(0.5,0.5);
                float2 node_8069 = (mul(i.uv0-node_8069_piv,float2x2( node_8069_cos, -node_8069_sin, node_8069_sin, node_8069_cos))+node_8069_piv);
                float4 node_6741 = _Time;
                float2 node_8476 = (node_8069+(node_6741.g*float2(_U,_v)));
                float4 _tex_var = tex2D(_tex,TRANSFORM_TEX(node_8476, _tex));
                float node_6120_ang = _jiaodujiaodu_mask;
                float node_6120_spd = _UVrotator_mask;
                float node_6120_cos = cos(node_6120_spd*node_6120_ang);
                float node_6120_sin = sin(node_6120_spd*node_6120_ang);
                float2 node_6120_piv = float2(0.5,0.5);
                float2 node_6120 = (mul(i.uv0-node_6120_piv,float2x2( node_6120_cos, -node_6120_sin, node_6120_sin, node_6120_cos))+node_6120_piv);
                float4 node_8359 = _Time;
                float2 node_2364 = (node_6120+(node_8359.g*float2(_U_mask,_v_mask)));
                float4 _mask_var = tex2D(_mask,TRANSFORM_TEX(node_2364, _mask));
                float3 node_6754 = (pow(_tex_var.rgb,_duibi)*i.vertexColor.a*_TintColor.rgb*_mask_var.rgb*_TintColor.a);
                float3 emissive = node_6754;
                float3 finalColor = emissive + (node_6754*_liangdu*i.vertexColor.a*_glowcolor.rgb);
                fixed4 finalRGBA = fixed4(finalColor,(_tex_var.a*_TintColor.a*i.vertexColor.a));
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
