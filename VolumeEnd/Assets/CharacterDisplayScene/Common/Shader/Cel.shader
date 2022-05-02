Shader "Sixerrr/Cel"
{
    Properties
    {
        _BaseTex ("Base Tex", 2D) = "white" {}
        _SSSTex ("SSS Tex", 2D) = "white" {}
        _ILMTex ("ILM Tex", 2D) = "white" {}
        _DetailTex ("Detail Tex", 2D) = "white" {}
    }
    SubShader
    {
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

            TEXTURE2D(_BaseTex);
            SAMPLER(sampler_BaseTex);

            TEXTURE2D(_SSSTex);
            SAMPLER(sampler_SSSTex);
            
            TEXTURE2D(_ILMTex);
            SAMPLER(sampler_ILMTex);

            TEXTURE2D(_DetailTex);
            SAMPLER(sampler_DetailTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _SSSTex_ST;
            float4 _ILMTex_ST;
            float4 _DetailTex_ST;
            CBUFFER_END
            


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
                float4 posCS : SV_POSITION;
                float3 nDirWS : TEXCOORD2;
                float4 color : COLOR;
            };

            v2f vert(a2v v)
            {
                v2f o;
                o.posCS = TransformObjectToHClip(v.vertex);
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

                float NdotL = dot(i.nDirWS, lDirWS) * 0.5 + 0.5;
                // float NdotV = dot(i.nDirWS, GetWorldSpaceViewDir(i.vertex));
                float Diffuse = max(0, NdotL);
                real3 Base = SAMPLE_TEXTURE2D(_BaseTex,sampler_BaseTex, i.uv.xy);
                real3 SSS = SAMPLE_TEXTURE2D(_SSSTex,sampler_SSSTex, i.uv.zw)*Base;
                real4 ILM = SAMPLE_TEXTURE2D(_ILMTex,sampler_ILMTex, i.uv1.xy);
                real3 Detail = SAMPLE_TEXTURE2D(_DetailTex,sampler_DetailTex, i.uv1.zw);
                result = lerp(Base, SSS, 1 - step(0.5, Diffuse) * i.color.r) * Detail * ILM.a;
                return real4(result, 1);
            }
            ENDHLSL
        }
    }
}