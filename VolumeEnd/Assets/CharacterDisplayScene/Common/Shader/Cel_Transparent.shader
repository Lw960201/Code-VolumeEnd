/*
此Shader用来暂时忽略未知部分模型，使其不渲染
*/
Shader "Sixerrr/Cel_Transparent"
{
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

            struct a2v
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert(a2v v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                return o;
            }

            real4 frag(v2f i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}