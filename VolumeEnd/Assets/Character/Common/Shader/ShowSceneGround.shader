Shader "Sixerrr/ShowSceneGround"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
        _BaseColor ("Base Color", COLOR) = (1,1,1,1)
    }
    SubShader
    {
        Pass
        {
            tags
            {
                "RenderPipeline" = "UniversalPipeline"
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
            }

            // 关闭渲染剔除
            Cull Off
            // 关闭深度写入
            ZWrite Off
            // 混合因子设置
            Blend SrcAlpha OneMinusSrcAlpha

            Name "Base"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _BaseColor;
            CBUFFER_END

            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert(a2v v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            real4 frag(v2f i) : SV_Target
            {
                real4 mainCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                return real4(mainCol.a * _BaseColor);
            }
            ENDHLSL
        }
    }
}