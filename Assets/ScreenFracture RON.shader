Shader "Custom/ScreenFracture"
{
    Properties
    {
        [NoScaleOffset]
        _CapturedFrame ("Captured Frame", 2D) = "black" {}

        _Amount ("Effect Amount", Range(0, 1)) = 0

        _Travel ("Shard Travel", Range(0, 3)) = 1.1
        _Gravity ("Gravity", Range(0, 2)) = 0.25
        _Rotation ("Shard Rotation", Range(0, 4)) = 1.4
        _Separation ("Shard Separation", Range(0, 0.1)) = 0.018

        _BlurStrength ("Pane Blur", Range(0, 0.15)) = 0.055
        _Distortion ("Pane Distortion", Range(0, 0.1)) = 0.035
        _SmearLines ("Smear Lines", Range(20, 300)) = 110
        _PaneVariation ("Pane Brightness Variation", Range(0, 1)) = 0.5

        _FlipY ("Flip Captured Image", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Overlay"
        }

        Pass
        {
            Name "MessyScreenFracture"

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CapturedFrame);
            SAMPLER(sampler_CapturedFrame);

            CBUFFER_START(UnityPerMaterial)
                float _Amount;

                float _Travel;
                float _Gravity;
                float _Rotation;
                float _Separation;

                float _BlurStrength;
                float _Distortion;
                float _SmearLines;
                float _PaneVariation;

                float _FlipY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;

                // XY = movement direction
                // Z = rotation direction
                // W = delay
                float4 motion : TEXCOORD1;

                // Center of this pane
                float2 pivot : TEXCOORD2;

                // Alpha contains random pane brightness
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 motionDirection : TEXCOORD1;
                float shardProgress : TEXCOORD2;
                float brightness : TEXCOORD3;
            };

            float RandomValue(float value)
            {
                return frac(
                    sin(value * 12.9898) *
                    43758.5453
                );
            }

            float2 RotateVector(
                float2 vectorValue,
                float angleValue
            )
            {
                float sineValue = sin(angleValue);
                float cosineValue = cos(angleValue);

                return float2(
                    cosineValue * vectorValue.x -
                    sineValue * vectorValue.y,

                    sineValue * vectorValue.x +
                    cosineValue * vectorValue.y
                );
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float rawProgress = saturate(
                    (_Amount - input.motion.w) /
                    max(1.0 - input.motion.w, 0.001)
                );

                float movementProgress =
                    rawProgress *
                    rawProgress;

                float2 localOffset =
                    input.positionOS.xy -
                    input.pivot;

                // Slightly shrink panes to create messy gaps.
                localOffset *=
                    1.0 -
                    _Separation *
                    rawProgress;

                float rotationAngle =
                    input.motion.z *
                    _Rotation *
                    movementProgress;

                localOffset = RotateVector(
                    localOffset,
                    rotationAngle
                );

                float2 movedPosition =
                    input.pivot +
                    localOffset;

                movedPosition +=
                    input.motion.xy *
                    _Travel *
                    movementProgress;

                movedPosition.y -=
                    _Gravity *
                    movementProgress *
                    movementProgress;

                output.positionCS = float4(
                    movedPosition,
                    0.0,
                    1.0
                );

                output.uv =
                    input.uv;

                output.motionDirection =
                    input.motion.xy;

                output.shardProgress =
                    movementProgress;

                output.brightness =
                    input.color.a;

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 capturedUV =
                    input.uv;

                capturedUV.y = lerp(
                    capturedUV.y,
                    1.0 - capturedUV.y,
                    _FlipY
                );

                float paneSeed = RandomValue(
                    input.motionDirection.x * 37.0 +
                    input.motionDirection.y * 91.0 +
                    input.brightness * 13.0
                );

                // Break every pane into uneven horizontal strips.
                float stripID = floor(
                    capturedUV.y *
                    _SmearLines
                );

                float animationStep =
                    floor(_Amount * 24.0);

                float stripRandom = RandomValue(
                    stripID +
                    paneSeed * 100.0 +
                    animationStep * 17.0
                );

                float verticalRandom = RandomValue(
                    stripID * 2.3 +
                    paneSeed * 77.0
                );

                float2 distortedUV =
                    capturedUV;

                // Horizontal tearing.
                distortedUV.x +=
                    (stripRandom - 0.5) *
                    _Distortion *
                    (0.4 + input.shardProgress);

                // Smaller vertical distortion.
                distortedUV.y +=
                    (verticalRandom - 0.5) *
                    _Distortion *
                    0.18;

                float2 blurDirection = normalize(
                    input.motionDirection +
                    float2(0.0001, 0.0001)
                );

                // Each pane gets a different blur angle.
                float randomBlurAngle =
                    (paneSeed - 0.5) *
                    1.8;

                blurDirection = RotateVector(
                    blurDirection,
                    randomBlurAngle
                );

                float blurAmount =
                    _BlurStrength *
                    (0.4 + input.shardProgress);

                half3 paneColor =
                    half3(0.0, 0.0, 0.0);

                float totalWeight = 0.0;

                // Eleven samples produce the blurry smear.
                [unroll]
                for (
                    int sampleIndex = 0;
                    sampleIndex < 11;
                    sampleIndex++
                )
                {
                    float samplePosition =
                        (
                            (float)sampleIndex -
                            5.0
                        ) / 5.0;

                    float sampleWeight =
                        1.0 -
                        abs(samplePosition) *
                        0.6;

                    float2 sampleUV =
                        distortedUV +
                        blurDirection *
                        blurAmount *
                        samplePosition;

                    // Extra horizontal mess on longer trails.
                    sampleUV.x +=
                        (stripRandom - 0.5) *
                        _Distortion *
                        abs(samplePosition) *
                        0.35;

                    half3 sampledColor =
                        SAMPLE_TEXTURE2D(
                            _CapturedFrame,
                            sampler_CapturedFrame,
                            sampleUV
                        ).rgb;

                    paneColor +=
                        sampledColor *
                        sampleWeight;

                    totalWeight +=
                        sampleWeight;
                }

                paneColor /= totalWeight;

                // Small brightness difference between panes.
                float paneBrightness = lerp(
                    1.0,
                    input.brightness,
                    _PaneVariation
                );

                paneColor *= paneBrightness;

                // No tint, posterization, or borders.
                return half4(
                    paneColor,
                    1.0
                );
            }

            ENDHLSL
        }
    }
}