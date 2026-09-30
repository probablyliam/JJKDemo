Shader "Custom/StylizedEnergy"
{
    Properties
    {
        [Header(Colors)]
        [HDR] _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        [HDR] _EdgeColor ("Edge Color", Color) = (0.5, 0, 1, 1)
        
        [Header(Energy Settings)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3.0
        _NoiseScale ("Noise Scale", Range(1, 50)) = 10.0
        _NoiseSpeed ("Noise Speed", Range(0, 10)) = 2.0
        
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Transparent"
        }
        
        // Pass 1: Outline (Inverted Hull)
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 extrudedPos = input.positionOS.xyz + (input.normalOS * _OutlineWidth * 0.005);
                output.positionCS = TransformObjectToHClip(extrudedPos);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
        
        // Pass 2: The Energy Core
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            // Standard transparent blending
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float4 _EdgeColor;
                float _FresnelPower;
                float _NoiseScale;
                float _NoiseSpeed;
            CBUFFER_END
            
            // Simple 2D Random
            float random (float2 st) {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }

            // 2D Value Noise
            float noise (float2 st) {
                float2 i = floor(st);
                float2 f = frac(st);
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a)* u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS  = GetWorldSpaceViewDir(output.positionWS);
                output.uv         = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                
                // 1. Fresnel Effect (Bright inside, darker/colored edges)
                // NdotV is 1 at center, 0 at edges.
                float NdotV = saturate(dot(normal, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                
                // 2. Animated Noise
                // We use world space position for noise so it doesn't just stick to the UV map (makes it look volumetric)
                float2 noiseUV = input.positionWS.xy * _NoiseScale;
                noiseUV.y += _Time.y * _NoiseSpeed; // Scroll up over time
                noiseUV.x += sin(_Time.y * _NoiseSpeed * 0.5); // Wiggle
                
                float n = noise(noiseUV);
                
                // Mix the fresnel with the noise to create a boiling edge effect
                float energyBlend = saturate(fresnel + (n * 0.5));
                
                // Combine colors
                half4 finalColor = lerp(_CoreColor, _EdgeColor, energyBlend);
                
                // Make the edges slightly more transparent to give a soft plasma feel, 
                // but keep the core solid.
                finalColor.a *= saturate(NdotV + 0.3);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}
