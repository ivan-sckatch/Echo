Shader "ECHO/Phase Surface"
{
    Properties
    {
        _BaseColor("Colour",Color)=(.5,.52,.5,1)
        _EmissionColor("Emission",Color)=(0,0,0,1)
        _Reveal("Phase visibility",Range(0,1))=1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float3 positionOS:TEXCOORD2; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor; half4 _EmissionColor; float _Reveal;
            CBUFFER_END
            V Vert(A input)
            {
                V o; o.positionWS=TransformObjectToWorld(input.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.positionOS=input.positionOS.xyz;return o;
            }
            half4 Frag(V input):SV_Target
            {
                float3 cell=floor(input.positionOS*230);
                float noise=frac(sin(dot(cell,float3(12.9898,78.233,37.719)))*43758.5453);
                clip(_Reveal-noise-.00001);
                half3 normal=normalize(input.normalWS);Light main=GetMainLight();
                half3 lighting=SampleSH(normal)+main.color*saturate(dot(normal,main.direction))*main.distanceAttenuation;
                #ifdef _ADDITIONAL_LIGHTS
                uint count=GetAdditionalLightsCount();
                for(uint i=0;i<count;i++){Light l=GetAdditionalLight(i,input.positionWS);lighting+=l.color*saturate(dot(normal,l.direction))*l.distanceAttenuation;}
                #endif
                lighting=max(lighting,half3(.30,.32,.34));
                float grain=frac(sin(dot(floor(input.positionOS*1700),float3(3.1,13.7,57.3)))*471.7);
                return half4(_BaseColor.rgb*lighting*(.94+.06*grain)+_EmissionColor.rgb,1);
            }
            ENDHLSL
        }
    }
}
