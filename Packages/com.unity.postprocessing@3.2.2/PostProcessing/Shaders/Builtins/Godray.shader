Shader "PostProcessing/GodRay" {
    Properties{
        _MainTex("Base (RGB)", 2D) = "white" {}
        _BlurTex("Blur", 2D) = "white"{}

        _CookieA("CookieA", 2D) = "while" {}
        _CookieB("CookieB", 2D) = "while" {}
        _CookieC("CookieC", 2D) = "while" {}
        _CookieD("CookieD", 2D) = "while" {}
    }
        CGINCLUDE
#define RADIAL_SAMPLE_COUNT 6
#include "UnityCG.cginc"

     //用于阈值提取高亮部分
        struct v2f_threshold
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };
    //用于blur
    struct v2f_blur
    {
        float4 pos : SV_POSITION;
        float2 uv  : TEXCOORD0;
        float2 blurOffset0 : TEXCOORD1;
        float2 blurOffset1 : TEXCOORD2;
        float2 blurOffset2 : TEXCOORD3;
        float2 blurOffset3 : TEXCOORD4;
    };
    //用于最终融合
    struct v2f_merge
    {
        float4 pos : SV_POSITION;
        float2 uv  : TEXCOORD0;
        float2 uv1 : TEXCOORD1;
    };
    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _BlurTex;
    sampler2D _CameraDepthTexture;
    float4 _BlurTex_TexelSize;
    float4 _ViewPortLightPos0;
    float4 _ViewPortLightPos1;
    float4 _ViewPortLightPos2;
    float4 _ViewPortLightPos3;
    float4 _offsets;
    float4 _ColorThreshold;
    float4 _LightColorA;
    float4 _LightColorB;
    float4 _LightColorC;
    float4 _LightColorD;
    float4 _LightFactors;
    float4 _PowFactors;
    float4 _LightRadius;
    float4 _LightDepths;
    float _ScreenRatio;

    sampler2D _CookieA;
    float4 _CookieA_TexelSize;
    float4 _CookieA_ScaleOffset;
    sampler2D _CookieB;
    float4 _CookieB_TexelSize;
    float4 _CookieB_ScaleOffset;
    sampler2D _CookieC;
    float4 _CookieC_TexelSize;
    float4 _CookieC_ScaleOffset;
    sampler2D _CookieD;
    float4 _CookieD_TexelSize;
    float4 _CookieD_ScaleOffset;

    //高亮部分提取shader
    v2f_threshold vert_threshold(appdata_img v)
    {
        v2f_threshold o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord.xy;
        //dx中纹理从左上角为初始坐标，需要反向
#if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0)
            o.uv.y = 1 - o.uv.y;
#endif    
        return o;
    }

    fixed4 frag_threshold(v2f_threshold i) : SV_Target
    {
        float lineSceneDepth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv).r);
        fixed4 color = tex2D(_MainTex, i.uv);
        float lum = Luminance(saturate(color - _ColorThreshold).rgb);
        float2 sr = float2(_ScreenRatio, 1.0f);

        float4 distFromLights = float4(0, 0, 0, 0);
#if GODRAY0_ON
        distFromLights[0] = length((_ViewPortLightPos0.xy - i.uv) * sr);
#endif
#if GODRAY1_ON
        distFromLights[1] = length((_ViewPortLightPos1.xy - i.uv) * sr);
#endif
#if GODRAY2_ON
        distFromLights[2] = length((_ViewPortLightPos2.xy - i.uv) * sr);
#endif
#if GODRAY3_ON
        distFromLights[3] = length((_ViewPortLightPos3.xy - i.uv) * sr);
#endif

        float edge = 1.0 - i.uv.x * (1.0 - i.uv.x) * i.uv.y * (1.0 - i.uv.y) * 8.0;
        float edge2 = edge * edge;
        float edgeControl = 1.0 - edge2 * edge2;
        float4 edgeControls = float4(edgeControl, edgeControl, edgeControl, edgeControl);
        float4 depthControls = saturate((lineSceneDepth - _LightDepths) * 32.0);
        float4 distanceControls = saturate(_LightRadius - distFromLights);

        fixed4 finalret = pow(lum* distanceControls, _PowFactors) * min(depthControls,edgeControls);
        return finalret;
    }
    
    //径向模糊 vert shader
    v2f_blur vert_blur(appdata_img v)
    {
        v2f_blur o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord.xy;
        //径向模糊采样偏移值*沿光的方向权重
        o.blurOffset0 = _offsets[0] * (_ViewPortLightPos0.xy - o.uv) * float2(_ScreenRatio, 1.0f);
        o.blurOffset1 = _offsets[1] * (_ViewPortLightPos1.xy - o.uv) * float2(_ScreenRatio, 1.0f);
        o.blurOffset2 = _offsets[2] * (_ViewPortLightPos2.xy - o.uv) * float2(_ScreenRatio, 1.0f);
        o.blurOffset3 = _offsets[3] * (_ViewPortLightPos3.xy - o.uv) * float2(_ScreenRatio, 1.0f);
        return o;
    }

    //径向模拟pixel shader
    fixed4 frag_blur(v2f_blur i) : SV_Target
    {
        half4 color = half4(0,0,0,0);
        float2 uv0 = i.uv.xy;
        float2 uv1 = i.uv.xy;
        float2 uv2 = i.uv.xy;
        float2 uv3 = i.uv.xy;
        for (int j = 0; j < RADIAL_SAMPLE_COUNT; j++)
        {
#if GODRAY0_ON
            color.r += tex2D(_MainTex, uv0).r;
            uv0 += i.blurOffset0;
#endif
#if GODRAY1_ON
            color.g += tex2D(_MainTex, uv1).g;
            uv1 += i.blurOffset0;
#endif
#if GODRAY2_ON
            color.b += tex2D(_MainTex, uv2).b;
            uv2 += i.blurOffset0;
#endif
#if GODRAY3_ON
            color.a += tex2D(_MainTex, uv3).a;
            uv3 += i.blurOffset0;
#endif
        }
        return color / RADIAL_SAMPLE_COUNT;
    }

    //融合vertex shader
    v2f_merge vert_merge(appdata_img v)
    {
        v2f_merge o;
        //mvp矩阵变换
        o.pos = UnityObjectToClipPos(v.vertex);
        //uv坐标传递
        o.uv.xy = v.texcoord.xy;
        o.uv1.xy = o.uv.xy;
#if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0)
            o.uv.y = 1 - o.uv.y;
#endif    
        return o;
    }

    fixed4 frag_merge(v2f_merge i) : SV_Target
    {
        fixed4 ori = tex2D(_MainTex, i.uv1);
        fixed4 blur = tex2D(_BlurTex, i.uv);
        //输出= 原始图像，叠加体积光贴图
        fixed4 sum = ori;
#if GODRAY0_ON
#if COOKIEA_ON
        fixed4 cookieA = tex2D(_CookieA, i.uv * _CookieA_ScaleOffset.xy + _CookieA_ScaleOffset.zw);
#else
        fixed4 cookieA = float4(1, 1, 1, 1);
#endif
        sum += _LightFactors[0] * blur[0] * _LightColorA * cookieA;
#endif
#if GODRAY1_ON
#if COOKIEB_ON
        fixed4 cookieB = tex2D(_CookieB, i.uv * _CookieB_ScaleOffset.xy + _CookieB_ScaleOffset.zw);
#else
        fixed4 cookieB = float4(1, 1, 1, 1);
#endif
        sum += _LightFactors[1] * blur[1] * _LightColorB * cookieB;
#endif
#if GODRAY2_ON
#if COOKIEC_ON
        fixed4 cookieC = tex2D(_CookieC, i.uv * _CookieC_ScaleOffset.xy + _CookieC_ScaleOffset.zw);
#else
        fixed4 cookieC = float4(1, 1, 1, 1);
#endif
        sum += _LightFactors[2] * blur[2] * _LightColorC * cookieC;
#endif
#if GODRAY3_ON
#if COOKIED_ON
        fixed4 cookieD = tex2D(_CookieD, i.uv * _CookieD_ScaleOffset.xy + _CookieD_ScaleOffset.zw);
#else
        fixed4 cookieD = float4(1, 1, 1, 1);
#endif
        sum += _LightFactors[3] * blur[3] * _LightColorD * cookieD;
#endif
        return sum;
    }

        ENDCG
        SubShader
    {
        //pass 0: 提取高亮部分
        Pass
        {
            ZTest Off
            Cull Off
            ZWrite Off
            Fog{ Mode Off }
            CGPROGRAM
            #pragma vertex vert_threshold
            #pragma fragment frag_threshold
            #pragma multi_compile_local _ GODRAY0_ON
            #pragma multi_compile_local _ GODRAY1_ON
            #pragma multi_compile_local _ GODRAY2_ON
            #pragma multi_compile_local _ GODRAY3_ON
            ENDCG
        }
            //pass 1: 径向模糊
            Pass
        {
            ZTest Off
            Cull Off
            ZWrite Off
            Fog{ Mode Off }
            CGPROGRAM
            #pragma vertex vert_blur
            #pragma fragment frag_blur
            #pragma multi_compile_local _ GODRAY0_ON
            #pragma multi_compile_local _ GODRAY1_ON
            #pragma multi_compile_local _ GODRAY2_ON
            #pragma multi_compile_local _ GODRAY3_ON
            ENDCG
        }
            //pass 2: 将体积光模糊图与原图融合
            Pass
        {
            ZTest Off
            Cull Off
            ZWrite Off
            Fog{ Mode Off }
            CGPROGRAM
            #pragma vertex vert_merge
            #pragma fragment frag_merge
            #pragma multi_compile_local _ GODRAY0_ON
            #pragma multi_compile_local _ GODRAY1_ON
            #pragma multi_compile_local _ GODRAY2_ON
            #pragma multi_compile_local _ GODRAY3_ON
            #pragma multi_compile_local _ COOKIEA_ON
            #pragma multi_compile_local _ COOKIEB_ON
            #pragma multi_compile_local _ COOKIEC_ON
            #pragma multi_compile_local _ COOKIED_ON
            ENDCG
        }
    }
}