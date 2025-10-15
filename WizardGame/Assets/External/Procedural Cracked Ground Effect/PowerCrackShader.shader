Shader "Custom/Power Crack"
{
    Properties
    {
        _MainTex("Main Texture", 3D) = "white" {}                   // The result of PowerCrackCompute shader
        _Scale("Scale", float) = 1                                  // The overall scale of the effect
        _CharBegin("Char Begin", float) = 0.1                       // Controls how much Colour1 there is
        _FireBegin("Fire Begin", float) = 0.5                       // Controls how much Colour2 there is
        _FadeBegin("Fade Begin", float) = 0.05                      // Controls how much of the effect is fully transparent
        _FadeEnd("Fade End", float) = 0.15                          // Controls how much of the effect is fully opaque
        _Alpha("Alpha", float) = 0.5                                // Controls how transparent the "opaque" parts of the effect are
        _Colour1("Colour1", Color) = (0, 0, 0, 0)
        _Colour2("Colour2", Color) = (1, 0.5, 0, 0)
        _Colour2Multiplier("Colour2 Multiplier", float) = 100       // Allows for HDR highlights on Colour2
        _EdgeFade("Edge Fade", float) = 5                           // Controls how quickly the effect fades at the edges of the effected area
        _DistortScale("Distort Scale", float) = 5                   // Controls the size of the turbulence effect
        _DistortStrength("Distort Strength", float) = 0.25          // Controls the strength of the turbulence effect
        _DistortionTimeScale("Distortion Time Scale", float) = 0    // Controls the speed of the turbulence effect
    }
    SubShader
    {

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        
        #pragma vertex vert
        #pragma fragment frag

        CBUFFER_START(UnityPerMaterial)

        sampler3D _MainTex;
        float _Scale;
        float _CharBegin;
        float _FireBegin;
        float _FadeBegin;
        float _FadeEnd;
        float _Alpha;
        float4 _Colour1;
        float4 _Colour2;
        float _Colour2Multiplier;
        float _EdgeFade;
        float _DistortScale;
        float _DistortStrength;
        float _DistortionTimeScale;

        CBUFFER_END

        // linearly interpolate from a to b, so that f = lo -> a, and f = hi -> b
        #define lerpRange(f, lo, hi, a, b) lerp(a, b, (f - lo) / (hi - lo))

        // linearly interpolate from a to b, so that f <= lo -> a, and f >= hi -> b
        #define lerpRangeClamped(f, lo, hi, a, b) lerp(a, b, saturate((f - lo) / (hi - lo)))

        struct appdata
        {
            float4 vertex : POSITION;
        };
            
        struct v2f
        {
            float4 vertex : SV_POSITION;
        };
        
        v2f vert (appdata v)
        {
            v2f o;
            o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
            return o;
        }

        // Normalises and snaps a vector to the closest cardinal or diagonal direction
        float3 primaryAxis(float3 vec)
        {
            const float INV2ROOT3 = 0.57735026919 / 2.0; // 1/(2√3)
            
            vec = normalize(vec);

            // only include axese greater than 1/(2√3)
            vec.x = abs(vec.x) >= INV2ROOT3 ? normalize(vec.x) : 0.0;
            vec.y = abs(vec.y) >= INV2ROOT3 ? normalize(vec.y) : 0.0;
            vec.z = abs(vec.z) >= INV2ROOT3 ? normalize(vec.z) : 0.0;

            return normalize(vec);
        }

        ENDHLSL

        // Volumes are used to determine pixels to consider for the effect, so cull front faces
        Cull Front

        // Depth checking is done in the shader, so no depth testing
        ZWrite Off
        ZTest Always

        // Regular alpha blending
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM

            half4 frag (v2f i) : SV_Target
            {
                float2 texel = 1 / _ScreenParams.xy;
                float2 uv = i.vertex.xy * texel;

                float3 worldPos = ComputeWorldSpacePosition(uv, SampleSceneDepth(uv), UNITY_MATRIX_I_VP);

                // determine position within the volume
                // do this in model space so that the volume can be stretched into different proportions
                float3 modelSpace = mul(UNITY_MATRIX_I_M, float4(worldPos, 1)).xyz;
                float modelSpaceLength = length(modelSpace) * 2;
                if (modelSpaceLength > 1.0) discard;

                // use alphaMul to fade out the effect at the edges of the volume
                float alphaMul = 1 - pow(1 - saturate(1 - modelSpaceLength), _EdgeFade);

                // determine the direction of the turbulence
                // should be perpendicular to the closest fault line
                float3 scrollDirection = primaryAxis(tex3D(_MainTex, worldPos).xyz);
                float3 distortionOffset = scrollDirection * _Time.y * _DistortionTimeScale;

                // offset the sample position according to turbulence
                worldPos += tex3D(_MainTex, (worldPos + distortionOffset) * _DistortScale).xyz * _DistortStrength * 0.1;

                float4 sample = tex3D(_MainTex, worldPos * _Scale);

                // this fades out the effect more naturally
                float mix = pow(sample.a * alphaMul, 3);

                float4 col = lerpRangeClamped(mix, _CharBegin, _FireBegin, _Colour1, _Colour2 * _Colour2Multiplier);
                col.a = lerpRangeClamped(mix, _FadeBegin, _FadeEnd, 0, _Alpha) * alphaMul;

                return col;
            }

            ENDHLSL
        }
    }
}
