Shader "PostProcessing/AdvanceFog"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _FogDensity("_FogDensity",Float) = 1.0
        _FogColor("_FogColor",Color) = (1,1,1,1)
    }
        SubShader
        {
            CGINCLUDE
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            half4 _MainTex_TexelSize;
            sampler2D _CameraDepthTexture;
            sampler2D _CookieTex;
            float _FogDensity;
            fixed4 _FogColor;
            float _FogDepthStart;
            float _FogDepthEnd;
            float _FogHeightStart;
            float _FogHeightEnd;

            float _CookieSpeedU;
            float _CookieSpeedV;
            float _CookieTilingU;
            float _CookieTilingV;
            float _CookieOffsetU;
            float _CookieOffsetV;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv_depth : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uv_depth = v.uv;

                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                {
                    o.uv_depth.y = 1 - o.uv_depth.y;
                }
                #endif

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,i.uv_depth);
                float3 viewSpaceRay = mul(unity_CameraInvProjection, float4(i.uv_depth * 2.0 - 1.0, 1.0, 1.0) * _ProjectionParams.z);
                float3 viewPos = viewSpaceRay * Linear01Depth(rawDepth);
                float3 worldPos = mul(unity_CameraToWorld, float4(viewPos.xy, -viewPos.z, 1.0)).xyz;
                float distance = length(worldPos - _WorldSpaceCameraPos);

                float2 cookieuv = i.uv * float2(_CookieTilingU, _CookieTilingV) + float2(_CookieOffsetU, _CookieOffsetV) + float2(_CookieSpeedU, _CookieSpeedV) * _Time.y;
                fixed4 cookie = tex2D(_CookieTex, cookieuv);

                float fogDensity1a = 1 - saturate((_FogHeightEnd - worldPos.y) / (_FogHeightEnd - _FogHeightStart));
                float fogDensity1b = 1 - saturate((worldPos.y - _FogHeightStart) / (_FogHeightEnd - _FogHeightStart));
                float fogDensity2 = saturate((distance - _FogDepthStart) / (_FogDepthEnd - _FogDepthStart));
                float fogDensity = saturate(fogDensity1a * fogDensity1b * fogDensity2 * _FogDensity * cookie.r);

                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb = lerp(col.rgb, _FogColor.rgb, fogDensity);

                return col;
            }

            ENDCG

            Pass
            {
                ZTest Always
                Cull Off
                ZWrite Off

                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                ENDCG
            }
        }

        Fallback Off
}