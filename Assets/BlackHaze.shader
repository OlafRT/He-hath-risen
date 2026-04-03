Shader "Custom/URP/BlackHaze"
{
    // ─────────────────────────────────────────────────────────────
    //  Apply to a large flat plane at the bottom of the world.
    //  The smoke is denser at the base height and fades upward.
    //  Uses domain-warped FBM noise so the wisps look organic.
    // ─────────────────────────────────────────────────────────────
    Properties
    {
        [Header(Colour)]
        _HazeColor          ("Haze Color",              Color)      = (0.03, 0.03, 0.04, 1)

        [Header(Height)]
        _HeightBase         ("Base Height (world Y)",   Float)      = -4.0
        _HeightRange        ("Fade Range (upward)",     Float)      = 6.0
        _HeightCurve        ("Height Curve (power)",    Float)      = 1.8

        [Header(Density)]
        _Density            ("Max Density",         Range(0,1))     = 0.92
        _DensityContrast    ("Density Contrast",    Range(0.5,4))   = 2.2

        [Header(Noise)]
        _NoiseScale         ("Noise Scale",             Float)      = 0.18
        _WarpStrength       ("Warp Strength",           Float)      = 0.55

        [Header(Animation)]
        _SpeedA             ("Layer A Speed",           Vector)     = (0.022,  0.008, 0, 0)
        _SpeedB             ("Layer B Speed",           Vector)     = (-0.013, 0.019, 0, 0)
        _SpeedC             ("Warp Speed",              Vector)     = (0.009, -0.011, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"        = "Transparent"
            "Queue"             = "Transparent+50"
            "RenderPipeline"    = "UniversalPipeline"
            "IgnoreProjector"   = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            Name "BlackHaze"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ─────────────────────────────────────────────────────
            //  Uniforms
            // ─────────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _HazeColor;
                float4 _SpeedA;
                float4 _SpeedB;
                float4 _SpeedC;
                float  _HeightBase;
                float  _HeightRange;
                float  _HeightCurve;
                float  _Density;
                float  _DensityContrast;
                float  _NoiseScale;
                float  _WarpStrength;
            CBUFFER_END

            // ─────────────────────────────────────────────────────
            //  Structs
            // ─────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ─────────────────────────────────────────────────────
            //  Noise
            // ─────────────────────────────────────────────────────
            float2 GradHash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453);
            }

            float GradNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                // Quintic — smoother than cubic hermite
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                return lerp(
                    lerp(dot(GradHash(i + float2(0,0)), f - float2(0,0)),
                         dot(GradHash(i + float2(1,0)), f - float2(1,0)), u.x),
                    lerp(dot(GradHash(i + float2(0,1)), f - float2(0,1)),
                         dot(GradHash(i + float2(1,1)), f - float2(1,1)), u.x),
                    u.y);
            }

            // 4 octaves, hardcoded so the loop is compile-time constant
            float FBM4(float2 p)
            {
                float v = 0.0;
                v += 0.5000 * GradNoise(p);       p *= 2.03;
                v += 0.2500 * GradNoise(p);       p *= 2.03;
                v += 0.1250 * GradNoise(p);       p *= 2.03;
                v += 0.0625 * GradNoise(p);
                // Normalise to roughly [-1, 1]
                return v / (0.5 + 0.25 + 0.125 + 0.0625);
            }

            // 3 octaves for the cheaper secondary layers
            float FBM3(float2 p)
            {
                float v = 0.0;
                v += 0.5000 * GradNoise(p);       p *= 2.03;
                v += 0.2500 * GradNoise(p);       p *= 2.03;
                v += 0.1250 * GradNoise(p);
                return v / (0.5 + 0.25 + 0.125);
            }

            // ─────────────────────────────────────────────────────
            //  Vertex
            // ─────────────────────────────────────────────────────
            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            // ─────────────────────────────────────────────────────
            //  Fragment
            // ─────────────────────────────────────────────────────
            half4 Frag(Varyings IN) : SV_Target
            {
                float2 baseUV = IN.positionWS.xz * _NoiseScale;
                float  t      = _Time.y;

                // Layer A — large slow wisps
                float2 uvA = baseUV + _SpeedA.xy * t;
                float  nA  = FBM4(uvA);

                // Layer B — medium detail
                float2 uvB = baseUV * 1.65 + _SpeedB.xy * t;
                float  nB  = FBM3(uvB);

                // Domain warp — feed nA back into the coords of a third sample.
                // This makes smoke curl and billow rather than slide flat.
                float2 warpUV = uvB + float2(nA, nA * 0.7) * _WarpStrength + _SpeedC.xy * t;
                float  nW     = FBM3(warpUV);

                // Blend layers
                float raw = nA * 0.45 + nB * 0.25 + nW * 0.30;
                raw = raw * 0.5 + 0.5;  // remap [-1,1] → [0,1]

                // Boost contrast — creates darker voids and denser clumps
                raw = saturate((raw - 0.5) * _DensityContrast + 0.5);

                // Height falloff — dense at _HeightBase, gone by _HeightBase + _HeightRange
                float heightT    = saturate((IN.positionWS.y - _HeightBase) / max(_HeightRange, 0.001));
                float heightMask = 1.0 - pow(heightT, _HeightCurve);

                float alpha = saturate(raw * heightMask * _Density);

                // Denser regions are slightly cooler — breaks up the flat black
                float3 col = lerp(_HazeColor.rgb * 1.4, _HazeColor.rgb * 0.6, raw);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
