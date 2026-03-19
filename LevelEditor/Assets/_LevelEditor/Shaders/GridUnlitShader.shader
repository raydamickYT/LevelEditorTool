Shader "Custom/GridFragmentShader"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
         _CellSize("CellSize", Float) = 1
         _lineWidth("LineWidth", Float) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION; //clip pos
                float2 uv : TEXCOORD0;
                float4 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _CellSize;
                float _lineWidth;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = mul(unity_ObjectToWorld, IN.positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float p1 = frac(IN.positionWS.y / _CellSize);
                float p2 = 1 - p1;
                float distY = min(p1, p2);
                float aaY = fwidth(distY);
                float lineY = 1- smoothstep((_lineWidth - aaY), (_lineWidth + aaY), distY);
                
                float p3 = frac(IN.positionWS.x / _CellSize);
                float p4 = 1 - p3;
                float distX = min(p3, p4);
                float aaX = fwidth(distX);
                float lineX = 1 - smoothstep((_lineWidth - aaX), (_lineWidth + aaX), distX);
                
                float gridMask = max(lineX, lineY);


                half4 color = half4(_BaseColor.r, _BaseColor.g, _BaseColor.b, _BaseColor.a * gridMask);

                return color;
            }
            ENDHLSL
        }
    }
}
