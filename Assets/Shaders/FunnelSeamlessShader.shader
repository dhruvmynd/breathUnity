Shader "Custom/FunnelSeamlessShader"
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
        
        // Cylindrical Projection
        _TextureScale ("Texture Scale", Vector) = (1, 1, 0, 0)
        _HeightScale ("Height Scale Factor", Range(0.1, 2)) = 1.0
        [Toggle] _UseWorldSpace("Use World Space", Float) = 0
        _ProjectionAxis ("Projection Axis (0=Y, 1=X, 2=Z)", Range(0, 2)) = 0
        
        // Twirl Effect
        _TwirlStrength ("Twirl Strength", Float) = 1.0
        _TwirlCenter ("Twirl Center", Vector) = (0, 0, 0, 0)
        _TwirlAxis ("Twirl Axis (0=Y, 1=X, 2=Z)", Range(0, 2)) = 0
        _TwirlFalloff ("Twirl Falloff", Range(0.1, 10)) = 1.0
        
        // Fresnel Rim Light
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2.0
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimStrength ("Rim Strength", Range(0, 2)) = 1.0
        
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
            #pragma shader_feature _FLIPNORMALS_ON
            #pragma shader_feature _USEWORLDSPACE_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0; // Keep original UVs as fallback
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                float3 tangentWS : TEXCOORD5;
                float3 bitangentWS : TEXCOORD6;
                float2 originalUV : TEXCOORD7; // Fallback UVs
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Metallic;
                float _Smoothness;
                float _Alpha;
                float4 _TextureScale;
                float _HeightScale;
                float _ProjectionAxis;
                float _TwirlStrength;
                float4 _TwirlCenter;
                float _TwirlAxis;
                float _TwirlFalloff;
                float _FresnelPower;
                float4 _RimColor;
                float _RimStrength;
            CBUFFER_END
            
            // Calculate cylindrical coordinates
            float2 CylindricalProjection(float3 position, float axis)
            {
                float2 uv;
                
                if (axis < 0.5) // Y axis (most common for funnels)
                {
                    float angle = atan2(position.z, position.x);
                    uv.x = (angle + PI) / (2.0 * PI); // Normalize to 0-1
                    uv.y = position.y;
                }
                else if (axis < 1.5) // X axis
                {
                    float angle = atan2(position.y, position.z);
                    uv.x = (angle + PI) / (2.0 * PI);
                    uv.y = position.x;
                }
                else // Z axis
                {
                    float angle = atan2(position.x, position.y);
                    uv.x = (angle + PI) / (2.0 * PI);
                    uv.y = position.z;
                }
                
                return uv;
            }
            
            // Apply 3D twirl effect
            float3 ApplyTwirl3D(float3 position, float3 center, float axis, float strength, float falloff)
            {
                float3 delta = position - center;
                float distance = length(delta);
                float twirlAmount = strength * exp(-distance * falloff);
                
                float angle = twirlAmount;
                float s = sin(angle);
                float c = cos(angle);
                
                float3 result = position;
                
                if (axis < 0.5) // Rotate around Y
                {
                    result.x = center.x + (delta.x * c - delta.z * s);
                    result.z = center.z + (delta.x * s + delta.z * c);
                }
                else if (axis < 1.5) // Rotate around X
                {
                    result.y = center.y + (delta.y * c - delta.z * s);
                    result.z = center.z + (delta.y * s + delta.z * c);
                }
                else // Rotate around Z
                {
                    result.x = center.x + (delta.x * c - delta.y * s);
                    result.y = center.y + (delta.x * s + delta.y * c);
                }
                
                return result;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Store original object space position
                output.positionOS = input.positionOS.xyz;
                
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
                
                // Store original UV as fallback
                output.originalUV = TRANSFORM_TEX(input.uv, _MainTex);
                
                // Fog
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Choose position space for projection
                #ifdef _USEWORLDSPACE_ON
                    float3 projectionPos = input.positionWS;
                    float3 twirlCenter = _TwirlCenter.xyz;
                #else
                    float3 projectionPos = input.positionOS;
                    float3 twirlCenter = TransformWorldToObject(_TwirlCenter.xyz);
                #endif
                
                // Apply 3D twirl effect
                float3 twirledPos = ApplyTwirl3D(projectionPos, twirlCenter, _TwirlAxis, _TwirlStrength, _TwirlFalloff);
                
                // Generate cylindrical UVs
                float2 cylindricalUV = CylindricalProjection(twirledPos, _ProjectionAxis);
                
                // Apply texture scaling
                cylindricalUV *= _TextureScale.xy;
                
                // Apply height-based scaling for funnel taper (optional)
                float heightFactor = 1.0 + (cylindricalUV.y - 0.5) * (_HeightScale - 1.0);
                cylindricalUV.x *= heightFactor;
                
                // Sample texture with seamless cylindrical UVs
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, cylindricalUV);
                half4 baseColor = texColor * _Color;
                
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Calculate fresnel for rim lighting
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
                half3 rimLight = _RimColor.rgb * fresnel * _RimStrength;
                
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
                surfaceData.emission = rimLight;
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
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            float3 _LightDirection;
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
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
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 position : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.position.xyz);
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}