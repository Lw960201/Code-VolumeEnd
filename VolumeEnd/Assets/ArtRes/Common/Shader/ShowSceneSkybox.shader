Shader "Sixerrr/ShowSceneSkybox"
{
    Properties
    {
        _CloudTex ("Cloud Tex", 2D) = "white" {}
        _CloudColor ("Cloud Color", COLOR) = (1,1,1,1)
        _CloudSpeed ("Cloud Speed", float) = 0.01
        _NoiseTex ("Noise Tex", 2D) = "white" {}
        _NoiseSpeed ("Noise Speed", float) = 0.01
        [HDR]_StartsTex ("Starts Tex", 2D) = "white" {}
        _StartsSpeed ("Starts Speed", float) = 0.01
        [HDR]_BigStartsTex ("BigStarts Tex(RG)", 2D) = "white" {}
        _BigStartsSpeed ("Big Starts Speed", float) = 0.01
        _BigStartsIntensity_R ("Big Starts Intensity R", float) = 0.01
        _StartsColorPaletteTex ("Starts Color Palette HighSaturation", 2D) = "white" {}
        [HDR]_StartsColorPaletteSpeed ("Starts Color Palette Speed", float) = 0.01
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


            TEXTURE2D(_CloudTex);
            SAMPLER(sampler_CloudTex);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            TEXTURE2D(_StartsTex);
            SAMPLER(sampler_StartsTex);

            TEXTURE2D(_BigStartsTex);
            SAMPLER(sampler_BigStartsTex);

            TEXTURE2D(_StartsColorPaletteTex);
            SAMPLER(sampler_StartsColorPaletteTex);
            
            CBUFFER_START(UnityPerMaterial)
            float4 _CloudTex_ST;
            float4 _NoiseTex_ST;
            float4 _StartsTex_ST;
            float4 _BigStartsTex_ST;
            float4 _StartsColorPaletteTex_ST;
            float4 _CloudColor;
            float _CloudSpeed;
            float _NoiseSpeed;
            float _StartsSpeed;
            float _BigStartsSpeed;
            float _StartsColorPaletteSpeed;
            float _BigStartsIntensity_R;
            CBUFFER_END

            struct a2v
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float4 tangent : TEXCOORD3;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 uv2 : TEXCOORD2;
                float4 posCS : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(a2v v)
            {
                v2f o;
                o.posCS = TransformObjectToHClip(v.vertex);
                o.uv.xy = TRANSFORM_TEX(v.uv,_CloudTex);
                o.uv.zw = TRANSFORM_TEX(v.uv,_NoiseTex);
                o.uv1.xy = TRANSFORM_TEX(v.uv,_StartsTex);
                o.uv1.zw = TRANSFORM_TEX(v.uv,_BigStartsTex);
                o.uv2.xy = TRANSFORM_TEX(v.uv,_StartsColorPaletteTex);
                // o.uv2.zw = TRANSFORM_TEX(v.uv,_BigStartsTex);
                o.color = v.color;
                return o;
            }

            real StarLightIntensity(real t)
            {
                real f1 = fmod(t,PI/2);
                real f2 = fmod(trunc(2*t/PI),2)*PI/2;
                real f3 = f2-f1;
                real f4 = fmod(trunc(2*t/PI),2);
                real f5 = f3*f4;
                real f6 = f1+2*f5-f2;
                return f6;
            }

            real4 frag(v2f i) : SV_Target
            {
                real4 result;
                
                real2 cloudUv =  float2(i.uv.x + frac(_CloudSpeed * _Time.y),i.uv.y);
                real4 cloud = SAMPLE_TEXTURE2D(_CloudTex,sampler_CloudTex, cloudUv) * _CloudColor;

                real2 noiseUv =  float2(i.uv.z + frac(_NoiseSpeed * _Time.y),i.uv.w);
                real noise = SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex, noiseUv).r;

                real2 startsUv =  float2(i.uv1.x + frac(_StartsSpeed * _Time.y), i.uv1.y);
                real4 starts = SAMPLE_TEXTURE2D(_StartsTex,sampler_StartsTex, startsUv);

                real2 bigStartsUv = float2(i.uv1.z + frac(_BigStartsSpeed * _Time.y), i.uv1.w);
                real4 bigStartsTex = SAMPLE_TEXTURE2D(_BigStartsTex,sampler_BigStartsTex, bigStartsUv);

                real2 startsColorPaletteUv = float2(i.uv2.x + frac(_StartsColorPaletteSpeed * _Time.y), i.uv2.y);
                real4 startsColorPaletteTex = SAMPLE_TEXTURE2D(_StartsColorPaletteTex,sampler_StartsColorPaletteTex, startsColorPaletteUv);
                
                // result = max(0,lerp(cloud + starts,bigStartsTex.r * _BigStartsIntensity_R * noise * startsColorPaletteTex * max(0,sin(frac(_Time.y/4.28))),bigStartsTex.r));
                result = lerp(cloud + starts,max(0,bigStartsTex.r * _BigStartsIntensity_R * noise * startsColorPaletteTex *StarLightIntensity(_Time.y)),bigStartsTex.r);
                result = bigStartsTex.r * _BigStartsIntensity_R * noise;
                return real4(result);
            }
            ENDHLSL
        }
    }
}