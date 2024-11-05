Shader "PostProcessing/GlowOutline"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _OutlineWidth("_OutlineWidth",Float) = 1.0
        _OutlineScale("_OutlineScale",Float) = 1.0
        _OutlineColor("_OutlineColor",Color) = (1,1,1,1)
    }
        SubShader
        {
            CGINCLUDE
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            half4 _MainTex_TexelSize;
            sampler2D _GlowOutlineTemp;
            sampler2D _GlowOutlineBlur;
            float _OutlineWidth;
            float _OutlineScale;
            fixed4 _OutlineColor;
            float4 _Offset;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 fragObject(v2f i) : SV_Target
            {
                fixed4 col = _OutlineColor;

                return col;
            }

            fixed4 fragBlurH(v2f i) : SV_Target
            {
                float4 col = float4(0, 0, 0, 0);

                col += 0.40 * tex2D(_MainTex, i.uv);
                col += 0.15 * tex2D(_MainTex, i.uv + _Offset.xy * float2(1, 1) * _MainTex_TexelSize.xy * _OutlineWidth);
                col += 0.15 * tex2D(_MainTex, i.uv + _Offset.xy * float2(-1, -1) * _MainTex_TexelSize.xy * _OutlineWidth);
                col += 0.10 * tex2D(_MainTex, i.uv + _Offset.xy * float2(2, 2) * _MainTex_TexelSize.xy * _OutlineWidth);
                col += 0.10 * tex2D(_MainTex, i.uv + _Offset.xy * float2(-2, -2) * _MainTex_TexelSize.xy * _OutlineWidth);
                col += 0.05 * tex2D(_MainTex, i.uv + _Offset.xy * float2(6, 6) * _MainTex_TexelSize.xy * _OutlineWidth);
                col += 0.05 * tex2D(_MainTex, i.uv + _Offset.xy * float2(-6, -6) * _MainTex_TexelSize.xy * _OutlineWidth);

                return col;
            }

            fixed4 fragBlurV(v2f i) : SV_Target
            {
                float4 col = float4(0, 0, 0, 0);

                col += 0.40 * tex2D(_MainTex, i.uv);
                col += 0.15 * tex2D(_MainTex, i.uv + _Offset.zw * float2(1, 1) * _MainTex_TexelSize.xy * _OutlineWidth);
                col += 0.15 * tex2D(_MainTex, i.uv + _Offset.zw * float2(-1, -1) * _MainTex_TexelSize.xy * _OutlineWidth);
                col += 0.10 * tex2D(_MainTex, i.uv + _Offset.zw * float2(2, 2) * _MainTex_TexelSize.xy * _OutlineWidth);
                col += 0.10 * tex2D(_MainTex, i.uv + _Offset.zw * float2(-2, -2) * _MainTex_TexelSize.xy * _OutlineWidth);
                col += 0.05 * tex2D(_MainTex, i.uv + _Offset.zw * float2(6, 6) * _MainTex_TexelSize.xy * _OutlineWidth);
                col += 0.05 * tex2D(_MainTex, i.uv + _Offset.zw * float2(-6, -6) * _MainTex_TexelSize.xy * _OutlineWidth);

                return col;
            }

            fixed4 fragCombine(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 temp = tex2D(_GlowOutlineTemp, i.uv);
                fixed4 blur = tex2D(_GlowOutlineBlur, i.uv);
                return col + saturate(blur - temp) * _OutlineScale;
            }

            ENDCG

            Pass
            {
                ZTest Always
                    Cull Off
                    ZWrite Off

                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment fragObject
                    ENDCG
            }

            Pass
            {
                    ZTest Always
                    Cull Off
                    ZWrite Off

                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment fragBlurH
                    ENDCG
            }

            Pass
            {
                    ZTest Always
                    Cull Off
                    ZWrite Off

                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment fragBlurV
                    ENDCG
            }

            Pass
            {
                ZTest Always
                Cull Off
                ZWrite Off

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment fragCombine
                ENDCG
            }
        }

        Fallback Off
}