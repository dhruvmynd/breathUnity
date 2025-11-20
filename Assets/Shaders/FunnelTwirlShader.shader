Shader "Custom/FunnelTwirlShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        // Metallic and Smoothness
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        
        // Transparency
        _Alpha ("Alpha", Range(0,1)) = 1.0
        
        // Twirl Effect
        _TwirlStrength ("Twirl Strength", Float) = 1.0
        _TwirlCenter ("Twirl Center", Vector) = (0.5, 0.5, 0, 0)
        _TwirlRadius ("Twirl Radius", Range(0, 1)) = 0.5
        
        // Fresnel Rim Light
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2.0
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimStrength ("Rim Strength", Range(0, 2)) = 1.0
        
        // Fresnel Specular
        _FresnelSpecularPower ("Fresnel Specular Power", Range(0.1, 10)) = 5.0
        _FresnelSpecularStrength ("Fresnel Specular Strength", Range(0, 3)) = 1.0
        _FresnelSpecularColor ("Fresnel Specular Color", Color) = (1,1,1,1)
        
        // UV Seam Blurring
        _SeamBlurRadius ("Seam Blur Radius", Range(0, 0.1)) = 0.02
        _SeamBlurSamples ("Seam Blur Samples", Range(1, 9)) = 5
        _SeamBlurStrength ("Seam Blur Strength", Range(0, 1)) = 1.0
        [Toggle] _UseVertexColorMask("Use Vertex Color as Seam Mask", Float) = 0
        
        // Normal Flip
        [Toggle] _FlipNormals("Flip Normals", Float) = 0
        
        // Render Settings
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_instancing
            #pragma shader_feature _FLIPNORMALS_ON
            #pragma shader_feature _USEVERTEXCOLORMASK_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // Vertex color for seam masking
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                float3 tangentWS : TEXCOORD5;
                float3 bitangentWS : TEXCOORD6;
                float4 color : TEXCOORD7; // Pass vertex color
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Metallic;
                float _Smoothness;
                float _Alpha;
                float _TwirlStrength;
                float4 _TwirlCenter;
                float _TwirlRadius;
                float _FresnelPower;
                float4 _RimColor;
                float _RimStrength;
                float _SeamBlurRadius;
                float _SeamBlurSamples;
                float _SeamBlurStrength;
                float _FresnelSpecularPower;
                float _FresnelSpecularStrength;
                float4 _FresnelSpecularColor;
            CBUFFER_END
            
            // Twirl function
            float2 ApplyTwirl(float2 uv)
            {
                float2 delta = uv - _TwirlCenter.xy;
                float angle = length(delta) * _TwirlStrength;
                float s = sin(angle);
                float c = cos(angle);
                
                // Create rotation matrix
                float2x2 rotationMatrix = float2x2(c, -s, s, c);
                
                // Apply rotation only within radius
                float mask = 1.0 - saturate(length(delta) / _TwirlRadius);
                delta = lerp(delta, mul(rotationMatrix, delta), mask);
                
                return _TwirlCenter.xy + delta;
            }
            
            // Detect proximity to UV seams
            float GetSeamMask(float2 uv)
            {
                // Distance to edges (0 or 1 in U and V)
                float distToEdgeU = min(uv.x, 1.0 - uv.x);
                float distToEdgeV = min(uv.y, 1.0 - uv.y);
                float minDist = min(distToEdgeU, distToEdgeV);
                
                // Create mask that's 1 at seams, 0 elsewhere
                float seamMask = 1.0 - saturate(minDist / _SeamBlurRadius);
                return seamMask;
            }
            
            // Sample texture with seam blurring
            half4 SampleTextureWithSeamBlur(float2 uv, float seamMask)
            {
                // If no seam nearby, just sample normally
                if (seamMask < 0.01 || _SeamBlurStrength < 0.01)
                {
                    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                }
                
                // Multi-sample blur at seams
                half4 color = half4(0, 0, 0, 0);
                float totalWeight = 0.0;
                
                int samples = clamp((int)_SeamBlurSamples, 1, 9);
                float blurStep = _SeamBlurRadius / float(samples);
                
                // Use fixed-size loop with conditional execution for D3D11 compatibility
                [unroll(19)] // Max 9x9 kernel = 81 samples, but we use -9 to 9 = 19 iterations
                for (int i = 0; i < 19; i++)
                {
                    [unroll(19)]
                    for (int j = 0; j < 19; j++)
                    {
                        int x = i - 9;
                        int y = j - 9;
                        
                        // Early termination for unused samples
                        if (abs(x) > samples || abs(y) > samples)
                            continue;
                        
                        float2 offset = float2(x, y) * blurStep;
                        float2 sampleUV = uv + offset;
                        
                        // Wrap UV coordinates
                        sampleUV = frac(sampleUV);
                        
                        // Weight based on distance from center
                        float weight = 1.0 - length(offset) / (_SeamBlurRadius * 2.0);
                        weight = saturate(weight);
                        
                        color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV) * weight;
                        totalWeight += weight;
                    }
                }
                
                color /= max(totalWeight, 0.001); // Avoid division by zero
                
                // Blend between normal and blurred based on seam proximity
                half4 normalSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return lerp(normalSample, color, seamMask * _SeamBlurStrength);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // Transform positions
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                
                // Transform normals with flip option
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                #ifdef _FLIPNORMALS_ON
                    output.normalWS = -normalInputs.normalWS;
                    output.tangentWS = normalInputs.tangentWS;
                    output.bitangentWS = -normalInputs.bitangentWS;
                #else
                    output.normalWS = normalInputs.normalWS;
                    output.tangentWS = normalInputs.tangentWS;
                    output.bitangentWS = normalInputs.bitangentWS;
                #endif
                
                // View direction
                output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);
                
                // UV with potential transformations
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                // Pass vertex color
                output.color = input.color;
                
                // Fog
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                // Apply twirl to UVs
                float2 twirlUV = ApplyTwirl(input.uv);
                
                // Detect seams - use vertex color if enabled, otherwise automatic detection
                float seamMask;
                #ifdef _USEVERTEXCOLORMASK_ON
                    // Use red channel of vertex color as seam mask
                    seamMask = input.color.r;
                #else
                    // Automatic seam detection based on UV edges
                    seamMask = GetSeamMask(twirlUV);
                #endif
                
                // Sample texture with seam blur
                half4 texColor = SampleTextureWithSeamBlur(twirlUV, seamMask);
                half4 baseColor = texColor * _Color;
                
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Calculate fresnel for rim lighting
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                half3 rimLight = _RimColor.rgb * fresnel * _RimStrength;
                
                // Calculate fresnel-based fake specular
                float fresnelSpecular = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelSpecularPower);
                half3 fakeSpecular = _FresnelSpecularColor.rgb * fresnelSpecular * _FresnelSpecularStrength;
                
                // Get main light for specular direction influence
                Light mainLight = GetMainLight();
                float3 halfVector = normalize(mainLight.direction + viewDirWS);
                float specularDot = saturate(dot(normalWS, halfVector));
                
                // Modulate fake specular by light direction for more realistic look
                fakeSpecular *= pow(specularDot, 2.0) * mainLight.color * mainLight.shadowAttenuation;
                
                // Setup surface data for lighting
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.fogCoord = input.fogFactor;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = half3(0, 0, 0);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);
                
                // Setup surface data
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseColor.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = rimLight + fakeSpecular;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = baseColor.a * _Alpha;
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 0.0h;
                
                // Calculate lighting
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                
                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogFactor);
                
                return color;
            }
            ENDHLSL
        }
        
        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            float3 _LightDirection;
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
        
        // Depth pass
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}
            
            ZWrite On
            ColorMask 0
            Cull[_Cull]
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 position : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}