Shader "Effects/Gamma"
{
    // shader properties for material
    Properties
    {
        _MainTex("Source", 2D) = "white" { } // wtf is this body even for

        [Tooltip("Gamma value.")]
        [Range(0.1, 5.0)]
        _Gamma("Gamma", Float) = 1; // forgot this wasn't C# and added the float suffix ;-;
    }

    // our actual shader logic
    SubShader
    {
        // i'm just fucking guessing these values
        Tags
        {
            // json ahh
            "RenderPipeline" = "UniversalPipeline"
        }

        // wait nvm THIS is our actual shader pass
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM // dude CG is so much better

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // this is such bullshit, in CG i can just do "UnityCG.cginc"

            float _Gamma; // sampled from property (allegedly)

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION; // i think this means Attributes.positionOS is the POSITION value from... somewhere
                float2 uv : TEXCOORD0; // tbf idk what this UV means they just put this in the tutorial
            }; // C++ PTSD arouses

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o; // o for output
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            // technically a pixel shader, idk y it's called fragment
            float4 frag(Varyings i) : SV_Target // i for input (i think)
            {
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // Gamma > 1 brightens, < 1 darkens
                color.rgb = pow(color.rgb, _Gamma); // where the fuck does float4.rgb come from, there is no documentation

                return color;
            }

            ENDHLSL
        }
    }
}