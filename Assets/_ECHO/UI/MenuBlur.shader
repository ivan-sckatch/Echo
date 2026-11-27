Shader "Hidden/ECHO/MenuBlur"
{
    Properties { _MainTex ("Scene", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 d = _MainTex_TexelSize.xy * 1.7;
                fixed4 c = tex2D(_MainTex, i.uv) * 4;
                c += tex2D(_MainTex, i.uv + float2(d.x, 0)) * 2;
                c += tex2D(_MainTex, i.uv - float2(d.x, 0)) * 2;
                c += tex2D(_MainTex, i.uv + float2(0, d.y)) * 2;
                c += tex2D(_MainTex, i.uv - float2(0, d.y)) * 2;
                c += tex2D(_MainTex, i.uv + d) + tex2D(_MainTex, i.uv - d);
                c += tex2D(_MainTex, i.uv + float2(d.x, -d.y)) + tex2D(_MainTex, i.uv + float2(-d.x, d.y));
                return c / 16;
            }
            ENDCG
        }
    }
}
