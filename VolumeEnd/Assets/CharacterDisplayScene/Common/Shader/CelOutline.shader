Shader "Sixerrr/CelOutline"
{
    Properties
    {
        _BaseColor ("Base  Color", Color) = (0,0,0,1)
        _BaseTex ("Base Tex", 2D) = "white" {}
        _SSSTex ("SSS Tex", 2D) = "white" {}
//        _ILMTex ("ILM Tex", 2D) = "white" {}
//        _DetailTex ("Detail Tex", 2D) = "white" {}
        _OutlineWidth("Width", float) = 0.2
    }
    SubShader
    {

        Pass
        {
            tags
            {
                "RenderPipeline" = "UniversalPipeline"
                "LightMode" = "Outline"
            }

            Cull Front
            Zwrite On

            Name "Outline"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseTex);
            SAMPLER(sampler_BaseTex);

            TEXTURE2D(_SSSTex);
            SAMPLER(sampler_SSSTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseTex_ST;
            float4 _SSSTex_ST;
            float _OutlineWidth;
            float _BaseColor;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                float3 normalView = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                // float2 offset = TransformViewToProjection(normalView.xy) * 0.0001;
                float2 offset = TransformWViewToHClip(normalView) * 0.0001;
                o.vertex.xy += _OutlineWidth * offset;
                return o;
            }

            real4 frag(v2f i) : SV_Target
            {
                real4 cLight = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, i.uv);
                real4 cSSS = SAMPLE_TEXTURE2D(_SSSTex, sampler_SSSTex, i.uv);
                real4 cDark = cLight * cSSS;
                cDark = cDark * 0.5f;
                cDark.a = 1;
                return cDark * _BaseColor;
            }
            ENDHLSL
        }
    }
}