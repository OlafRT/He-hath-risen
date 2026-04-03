Shader "Custom/UIBlur_URP"
{
    Properties
    {
        [Header(Blur Settings)]
        _BlurSize       ("Blur Size",       Range(0.0, 20.0)) = 5.0
        _BlurSamples    ("Blur Samples",    Range(1, 4))      = 2

        [Header(Tint)]
        _TintColor      ("Tint Color",      Color)            = (1, 1, 1, 0)

        [Header(Unity UI Internal)]
        [PerRendererData]
        _MainTex        ("Sprite Texture",  2D)               = "white" {}
        _Color          ("Vertex Color",    Color)            = (1, 1, 1, 1)
        _StencilComp    ("Stencil Comparison", Float)         = 8
        _Stencil        ("Stencil ID",          Float)        = 0
        _StencilOp      ("Stencil Operation",   Float)        = 0
        _StencilWriteMask ("Stencil Write Mask",Float)        = 255
        _StencilReadMask  ("Stencil Read Mask", Float)        = 255
        _ColorMask        ("Color Mask",         Float)       = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline"    = "UniversalPipeline"
        }

        // ── Unity UI stencil block (required for Mask component support) ──
        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        ColorMask [_ColorMask]

        // Premultiplied-alpha blend so the tint alpha controls overlay opacity
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "UIBlur"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Textures ──────────────────────────────────────────────────
            // Requires "Opaque Texture" enabled in your URP Renderer asset!
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // ── Uniforms ──────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half4  _TintColor;
                float  _BlurSize;
                int    _BlurSamples;
            CBUFFER_END

            // ── Structs ───────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
                half4  color       : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Gaussian weights (σ ≈ 1.0, kernel radius 4) ──────────────
            // Stored as a 1D half-kernel; index 0 = center.
            static const float GW[5] = { 0.2270270, 0.1945946, 0.1216216, 0.0540540, 0.0162162 };

            // ── Vertex shader ─────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.screenPos   = ComputeScreenPos(OUT.positionHCS);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            // ── Fragment shader ───────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                // Perspective-correct screen UV
                float2 screenUV  = IN.screenPos.xy / IN.screenPos.w;
                float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                float2 blurStep  = texelSize * _BlurSize;

                // ── Two-pass separable Gaussian approximated in one pass ──
                // We loop over a [-_BlurSamples*2 .. +_BlurSamples*2] grid.
                // Clamped to the 5-tap half-kernel stored above.
                half3  col       = 0;
                float  totalW    = 0;
                int    radius    = clamp(_BlurSamples * 2, 1, 4);

                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        float2 offset = float2(x, y) * blurStep;
                        float  wx     = GW[abs(x)];
                        float  wy     = GW[abs(y)];
                        float  w      = wx * wy;

                        col    += SAMPLE_TEXTURE2D(_CameraOpaqueTexture,
                                    sampler_CameraOpaqueTexture,
                                    screenUV + offset).rgb * w;
                        totalW += w;
                    }
                }
                col /= totalW;

                // ── Tint / frosted-glass overlay ─────────────────────────
                // _TintColor.a = 0  → pure blur, no tint
                // _TintColor.a = 1  → full tint colour on top
                col = lerp(col, _TintColor.rgb, _TintColor.a);

                // ── Respect the sprite alpha (for rounded panels / masks) ─
                half spriteAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;
                half finalAlpha  = spriteAlpha * IN.color.a;

                // Pre-multiplied alpha output
                return half4(col * finalAlpha, finalAlpha);
            }
            ENDHLSL
        }
    }

    // Fallback so the panel still renders in Built-in RP (shows sprite tint)
    Fallback "UI/Default"
}
