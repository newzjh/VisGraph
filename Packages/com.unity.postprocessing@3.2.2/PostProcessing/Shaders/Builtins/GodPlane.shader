Shader "PostProcessing/GodPlane"
{
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
        _Cutoff("CutOff", Range(0, 1)) = 0.5
        [Toggle] _Alpha("Visible?", Int) = 0
        //_Alpha("Alpha", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags{"Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout"}

        
        Pass{
            Tags { "LightMode" = "ForwardBase"}

            ZWrite On
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Lighting.cginc"
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "UnityStandardUtils.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Cutoff;
            fixed _Alpha;
            struct a2v {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 pos : POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            v2f vert(a2v v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed3 worldNormal = normalize(i.worldNormal);
                fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));

                float2 uv = i.uv;// +_Time.x;

                fixed4 texColor = tex2D(_MainTex, uv);

                //Alpha Test
                //clip(texColor.a - _Cutoff);
                clip(Luminance(texColor.rgb) - _Cutoff);

                clip(Luminance(texColor.rgb) - (1-_Alpha));

                return fixed4(0, 0, 0, 1);
            }
            ENDCG
        }
        

       Pass{
            Tags {"LightMode" = "ShadowCaster"}

            ZWrite On
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "UnityStandardUtils.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Cutoff;
            struct a2v {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 pos : POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            v2f vert(a2v v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target{

                float2 uv = i.uv;// +_Time.x;

                fixed4 texColor = tex2D(_MainTex, uv);

                //Alpha Test
                //clip(texColor.a - _Cutoff);
                clip(Luminance(texColor.rgb) - _Cutoff);

                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }

    FallBack "Transparent/Cutout/VertexLit"
}