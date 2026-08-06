Shader "Custom/URP_Bubble"
{
    Properties
    {
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 worldPos     : TEXCOORD0;
                float3 worldNormal  : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half _Glossiness;
                half _Metallic;
            CBUFFER_END

            float getAddPos(float pos, int offset) 
            {
                float speed = 0.5 + offset * 0.25;
                return sin(pos * 10.0 + _Time.y * speed) * 0.02;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 기존 vert 오프셋 계산 로직
                float3 modifiedPos = input.positionOS.xyz;
                modifiedPos.x += getAddPos(modifiedPos.x, 0);
                modifiedPos.y += getAddPos(modifiedPos.y, 1);
                modifiedPos.z += getAddPos(modifiedPos.z, 2);

                // 좌표 변환
                VertexPositionInputs vertexInput = GetVertexPositionInputs(modifiedPos);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                output.worldNormal = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 기존 색상 공식 (시간 + 위치 기반 무지개 일렁임)
                float3 col = sin(_Time.w + input.worldPos * 10.0) * 0.3 + 0.7;

                // 기존 알파(림) 공식
                float3 normal = normalize(input.worldNormal);
                float3 viewDir = normalize(input.viewDirWS);
                float rim = dot(normal, viewDir);
                float alpha = saturate(pow(1.0 - rim, 2.0) );

                // 최종 색상 + 투명도 반환
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}