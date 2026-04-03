Shader "Custom/URP_FryingOil"
{
    Properties
    {
        [Header(Oil Appearance)]
        _OilColor           ("Oil Color",            Color)         = (0.78, 0.52, 0.08, 0.88)
        _OilColorDeep       ("Oil Deep Color",       Color)         = (0.55, 0.30, 0.04, 0.95)
        _Smoothness         ("Smoothness",            Range(0,1))    = 0.88
        _Metallic           ("Metallic",              Range(0,1))    = 0.0

        [Header(Fresnel)]
        _FresnelColor       ("Fresnel Color",         Color)         = (1.0, 0.85, 0.45, 1.0)
        _FresnelPower       ("Fresnel Power",          Float)         = 4.0
        _FresnelStrength    ("Fresnel Strength",       Range(0,2))    = 0.8

        [Header(Bubbles)]
        _BubbleColor        ("Bubble Edge Color",     Color)         = (1.0, 0.92, 0.60, 1.0)
        _BubbleScale        ("Bubble Scale",          Float)         = 6.0
        _BubbleSpeed        ("Bubble Speed",          Float)         = 0.4
        _BubbleRiseSpeed    ("Bubble Rise Speed",     Float)         = 0.25
        _BubbleEdgeWidth    ("Bubble Edge Width",     Range(0.01,0.4))= 0.12
        _BubbleIntensity    ("Bubble Intensity",      Range(0,1))    = 0.75
        _BubblePopChance    ("Pop Frequency",         Range(0.1,2.0))= 0.8
        _BubbleShimmer      ("Bubble Shimmer",        Range(0,1))    = 0.5

        [Header(Surface Ripples)]
        _RippleScale        ("Ripple Scale",          Float)         = 12.0
        _RippleSpeed        ("Ripple Speed",          Float)         = 0.6
        _RippleStrength     ("Ripple Strength",       Range(0,0.15)) = 0.035
        _NormalStrength     ("Normal Ripple Strength",Range(0,3))    = 1.2

        [Header(Vertex Displacement)]
        _DisplacementScale  ("Displacement Scale",    Float)         = 8.0
        _DisplacementStrength("Displacement Amount",  Range(0,0.1))  = 0.018
        _DisplacementSpeed  ("Displacement Speed",    Float)         = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // URP feature keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── Constant Buffer ─────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                half4  _OilColor;
                half4  _OilColorDeep;
                half   _Smoothness;
                half   _Metallic;

                half4  _FresnelColor;
                half   _FresnelPower;
                half   _FresnelStrength;

                half4  _BubbleColor;
                float  _BubbleScale;
                float  _BubbleSpeed;
                float  _BubbleRiseSpeed;
                half   _BubbleEdgeWidth;
                half   _BubbleIntensity;
                float  _BubblePopChance;
                half   _BubbleShimmer;

                float  _RippleScale;
                float  _RippleSpeed;
                float  _RippleStrength;
                half   _NormalStrength;

                float  _DisplacementScale;
                float  _DisplacementStrength;
                float  _DisplacementSpeed;
            CBUFFER_END

            // ── Structs ──────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 tangentWS  : TEXCOORD2;
                float3 bitangentWS: TEXCOORD3;
                float2 uv         : TEXCOORD4;
                float  fogFactor  : TEXCOORD5;
            };

            // ── Utility: hash & noise ────────────────────────────────────────

            // Fast 2D hash → [0,1]²
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // Scalar hash → [0,1]
            float hash1(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Smooth value noise (bilinear)
            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);            // smoothstep

                float a = hash1(i);
                float b = hash1(i + float2(1, 0));
                float c = hash1(i + float2(0, 1));
                float d = hash1(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Fractal Brownian Motion — layered noise for organic ripples
            float fbm(float2 uv, int octaves)
            {
                float value = 0.0;
                float amp   = 0.5;
                float freq  = 1.0;
                for (int o = 0; o < octaves; o++)
                {
                    value += valueNoise(uv * freq) * amp;
                    amp   *= 0.5;
                    freq  *= 2.1;
                }
                return value;
            }

            // ── Voronoi / Cellular noise for bubbles ─────────────────────────
            //   Returns: x = distance to nearest seed (bubble radius proxy)
            //            y = a per-cell random value (for colour variation)
            //            z = time phase (drives pop animation)
            float3 voronoi(float2 uv, float time)
            {
                float2 p = floor(uv);
                float2 f = frac(uv);

                float  minDist  = 1e9;
                float  cellVal  = 0.0;
                float  phase    = 0.0;

                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        float2 b    = float2(i, j);
                        float2 seed = hash2(p + b);       // random point in cell

                        // Each bubble has its own birth/pop cycle
                        float  cellPhase = frac(seed.x * 7.3 + time * _BubbleSpeed * _BubblePopChance);

                        // Bubbles rise upward over their life span
                        float2 animated = b + seed;
                        animated.y     -= cellPhase * _BubbleRiseSpeed * 2.0;

                        float2 r = animated - f;
                        float  d = length(r);

                        if (d < minDist)
                        {
                            minDist = d;
                            cellVal = seed.y;
                            phase   = cellPhase;
                        }
                    }
                }
                return float3(minDist, cellVal, phase);
            }

            // ── Vertex stage ─────────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float time = _Time.y;

                // Two layers of noise for organic vertex displacement
                float2 dUV  = IN.uv * _DisplacementScale;
                float  d1   = valueNoise(dUV + time * _DisplacementSpeed);
                float  d2   = valueNoise(dUV * 1.61 - time * _DisplacementSpeed * 0.7 + 4.3);
                float  disp = (d1 + d2 - 1.0) * _DisplacementStrength;

                float3 displaced = IN.positionOS.xyz + IN.normalOS * disp;

                OUT.positionWS    = TransformObjectToWorld(displaced);
                OUT.positionCS    = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS      = TransformObjectToWorldNormal(IN.normalOS);
                OUT.tangentWS     = TransformObjectToWorldDir(IN.tangentOS.xyz);
                OUT.bitangentWS   = cross(OUT.normalWS, OUT.tangentWS)
                                    * (IN.tangentOS.w * GetOddNegativeScale());
                OUT.uv            = IN.uv;
                OUT.fogFactor     = ComputeFogFactor(OUT.positionCS.z);

                return OUT;
            }

            // ── Fragment stage ────────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;
                float2 uv  = IN.uv;

                // ── Bubble layer ─────────────────────────────────────────────
                float2 bubbleUV = uv * _BubbleScale;
                float3 vor      = voronoi(bubbleUV, time);
                float  dist     = vor.x;    // 0 = centre of bubble
                float  cellID   = vor.y;    // per-cell random
                float  phase    = vor.z;    // life phase [0..1]

                // Bubble shrinks to a point at birth (phase≈0) and at pop (phase≈1)
                float  sizeAnim = sin(phase * PI) * 0.85 + 0.15;
                float  rim      = dist / max(sizeAnim, 0.01);

                // Bubble edge ring
                float edgeOuter = smoothstep(_BubbleEdgeWidth, 0.0,             rim - (1.0 - _BubbleEdgeWidth));
                float edgeInner = smoothstep(0.0,              _BubbleEdgeWidth, rim - (1.0 - _BubbleEdgeWidth * 2.0));
                float bubbleRim = saturate(edgeOuter - edgeInner * 0.5);

                // Bright specular cap on each bubble dome
                float capDist    = length((frac(bubbleUV) - 0.5) / max(sizeAnim, 0.01));
                float bubbleCap  = smoothstep(0.25, 0.0, capDist) * sizeAnim * _BubbleShimmer;

                // Combine bubble visual
                float bubbleMask = saturate(bubbleRim + bubbleCap * 0.4);

                // Colour variation per cell
                half3 bubbleTint = lerp(_BubbleColor.rgb, _BubbleColor.rgb * 1.3,
                                        cellID * _BubbleShimmer);

                // ── Ripple normals ───────────────────────────────────────────
                float2 rUV  = uv * _RippleScale;
                float  rn1  = fbm(rUV  + time * _RippleSpeed,           3) - 0.5;
                float  rn2  = fbm(rUV * 1.4 - time * _RippleSpeed * 0.7 + 5.1, 3) - 0.5;

                // Derive a perturbed normal in tangent space
                float3 bumpTS  = normalize(float3(rn1 * _NormalStrength,
                                                   rn2 * _NormalStrength,
                                                   1.0));
                float3x3 TBN   = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                float3 normalWS = normalize(mul(bumpTS, TBN));

                // ── View direction & Fresnel ─────────────────────────────────
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float  NdotV     = saturate(dot(normalWS, viewDirWS));
                float  fresnel   = pow(1.0 - NdotV, _FresnelPower);

                // ── Lighting ─────────────────────────────────────────────────
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                    Light  mainLight   = GetMainLight(shadowCoord);
                #else
                    Light  mainLight   = GetMainLight();
                #endif

                float  NdotL  = saturate(dot(normalWS, mainLight.direction));
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float  NdotH  = saturate(dot(normalWS, halfDir));

                // GGX-ish specular
                float  roughness = 1.0 - _Smoothness;
                float  alpha2    = roughness * roughness;
                float  denom     = (NdotH * NdotH * (alpha2 - 1.0) + 1.0);
                float  D         = alpha2 / (PI * denom * denom + 1e-5);
                float  spec      = D * _Smoothness * _Smoothness;

                // Diffuse wrap
                half3  diffuse   = mainLight.color * (NdotL * 0.7 + 0.3)
                                   * mainLight.shadowAttenuation;

                // Additional lights
                #if defined(_ADDITIONAL_LIGHTS)
                uint   pixelLightCount = GetAdditionalLightsCount();
                for (uint idx = 0; idx < pixelLightCount; ++idx)
                {
                    Light  addLight  = GetAdditionalLight(idx, IN.positionWS);
                    float  addNdotL  = saturate(dot(normalWS, addLight.direction));
                    diffuse += addLight.color * addNdotL * addLight.distanceAttenuation;
                }
                #endif

                // ── Colour composition ───────────────────────────────────────
                // Depth tint: deeper areas are richer/darker
                float  depthMask  = fbm(uv * 4.0 + time * 0.05, 2);
                half3  baseColor  = lerp(_OilColorDeep.rgb, _OilColor.rgb, depthMask);

                // Apply bubble overlay
                half3  finalRGB   = lerp(baseColor, bubbleTint, bubbleMask * _BubbleIntensity);

                // Lighting pass
                finalRGB *= diffuse + 0.15;

                // Specular highlights (shiny oil)
                finalRGB += mainLight.color * spec * _Smoothness;

                // Fresnel edge glow (oil picks up warm reflections at glancing angles)
                finalRGB += _FresnelColor.rgb * fresnel * _FresnelStrength;

                // Bubble dome sheen (bright cap on each bubble)
                finalRGB += bubbleCap * _BubbleColor.rgb * _BubbleShimmer * 0.6;

                // ── Alpha ────────────────────────────────────────────────────
                half   alpha = lerp(_OilColor.a, 1.0, fresnel * 0.4);
                alpha        = saturate(alpha + bubbleMask * _BubbleIntensity * 0.1);

                // ── Fog ──────────────────────────────────────────────────────
                finalRGB = MixFog(finalRGB, IN.fogFactor);

                return half4(finalRGB, alpha);
            }
            ENDHLSL
        }

        // No ShadowCaster pass — transparent surfaces don't cast shadows.
    }

    FallBack "Universal Render Pipeline/Lit"
}
