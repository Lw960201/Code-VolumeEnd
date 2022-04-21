Shader "Sixerrr/Cel"
{
    Properties
    {
        _BaseTex ("Base Tex", 2D) = "white" {}
        _SSSTex ("SSS Tex", 2D) = "white" {}
        _ILMTex ("ILM Tex", 2D) = "white" {}
        _DetailTex ("Detail Tex", 2D) = "white" {}
        _OutlineWidth("Width", float) = 0.2
    }
    SubShader
    {
//        Pass
//        {
//            Name "Outline"
//            HLSLPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//
//            struct appdata
//            {
//                float4 vertex : POSITION;
//                float3 normal : NORMAL;
//            };
//
//            struct v2f
//            {
//                float4 vertex : SV_POSITION;
//            };
//
//            float _OutlineWidth;
//
//            v2f vert(appdata v)
//            {
//                v2f o;
//                o.vertex = TransformObjectToHClip(v.vertex);
//                float3 normalView = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
//                // float2 offset = TransformViewToProjection(normalView.xy) * 0.0001;
//                float2 offset = TransformWViewToHClip(normalView) * 0.0001;
//                o.vertex.xy += _OutlineWidth * offset;
//                return o;
//            }
//
//            real4 frag(v2f i) : SV_Target
//            {
//                return real4(0, 0, 0, 0);
//            }
//            ENDHLSL
//        }


        Pass
        {
            tags
            {
                "RenderPipeline" = "UniversalPipeline"
            }
            
            Name "Base"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

            sampler2D _BaseTex;
            sampler2D _SSSTex;
            sampler2D _ILMTex;
            sampler2D _DetailTex;

            float4 _MainTex_ST;
            float4 _SSSTex_ST;
            float4 _ILMTex_ST;
            float4 _DetailTex_ST;

            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float3 nDirWS : TEXCOORD2;
                float4 color : COLOR;
            };

            v2f vert(a2v v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv.xy = v.uv;
                o.uv.zw = v.uv;
                o.uv1.xy = v.uv;
                o.uv1.zw = v.uv;
                o.color = v.color;
                o.nDirWS = TransformObjectToWorldNormal(v.normal);
                return o;
            }

            real4 frag(v2f i) : SV_Target
            {
                real3 result;
                
                float3 lDirWS = GetMainLight().direction;
                
                float NdotL = dot(i.nDirWS, lDirWS)*0.5 + 0.5;
                // float NdotV = dot(i.nDirWS, GetWorldSpaceViewDir(i.vertex));
                float Diffuse = max(0, NdotL);
                real3 Base = tex2D(_BaseTex, i.uv.xy);
                real3 SSS = tex2D(_SSSTex, i.uv.zw)*Base;
                real4 ILM = tex2D(_ILMTex, i.uv1.xy);
                real3 Detail = tex2D(_DetailTex, i.uv1.zw);
                result = lerp(Base, SSS, 1-step(0.5,Diffuse) * i.color.r) * Detail * ILM.a;
                return real4(result,1);
            }
            ENDHLSL
        }
    }
}