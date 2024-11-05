// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "VG/Scene/Base"
{
	Properties
	{
		[KeywordEnum(Opaque,Cutout)] _RenderingMode("渲染模式", Float) = 0
		[HDR][Space(10)]_Color("颜色", Color) = (1,1,1,1)
		_MainTex("基础图", 2D) = "white" {}
		_Cutoff("裁剪值", Range( 0 , 1)) = 0.5
		[Space(10)]_BumpMap("法线图", 2D) = "bump" {}
		_BumpScale("法线强度", Range( 0 , 2)) = 1
		[Space(10)]_MaskMap("材质遮罩图", 2D) = "white" {}
		_Glossiness("粗糙度", Range( 0 , 1)) = 1
		_Metallic("金属度", Range( 0 , 1)) = 1
		_OcclusionStrength("AO强度", Range( 0 , 1)) = 1
		[Space(10)][Toggle(_EMISSION_ON)] _EMISSION("自发光开关", Float) = 0
		_EmissionMap("自发光图", 2D) = "white" {}
		_EmissionColor("自发光颜色", Color) = (0,0,0,0)
		[Space(10)][Toggle(_RIM_ON)] _RIM("边缘光开关", Float) = 0
		_RimPower("边缘光力度", Range( 0 , 10)) = 0
		_RimColor("边缘光颜色", Color) = (0,0,0,0)
		_RimIntensity("边缘光强度", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma shader_feature _EMISSION_ON
		#pragma shader_feature _RIM_ON
		#pragma shader_feature _RENDERINGMODE_OPAQUE _RENDERINGMODE_CUTOUT
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float2 uv_texcoord;
			float3 viewDir;
			INTERNAL_DATA
		};

		uniform sampler2D _BumpMap;
		uniform float4 _BumpMap_ST;
		uniform float _BumpScale;
		uniform float4 _Color;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _EmissionMap;
		uniform float4 _EmissionMap_ST;
		uniform float4 _EmissionColor;
		uniform float _RimPower;
		uniform float _RimIntensity;
		uniform float4 _RimColor;
		uniform float _Metallic;
		uniform sampler2D _MaskMap;
		uniform float4 _MaskMap_ST;
		uniform float _Glossiness;
		uniform float _OcclusionStrength;
		uniform float _Cutoff;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_BumpMap = i.uv_texcoord * _BumpMap_ST.xy + _BumpMap_ST.zw;
			float3 bump33 = UnpackScaleNormal( tex2D( _BumpMap, uv_BumpMap ), _BumpScale );
			o.Normal = bump33;
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode2 = tex2D( _MainTex, uv_MainTex );
			float4 base36 = ( _Color * tex2DNode2 );
			o.Albedo = base36.rgb;
			float4 temp_cast_1 = (0.0).xxxx;
			float2 uv_EmissionMap = i.uv_texcoord * _EmissionMap_ST.xy + _EmissionMap_ST.zw;
			#ifdef _EMISSION_ON
				float4 staticSwitch45 = ( tex2D( _EmissionMap, uv_EmissionMap ) * _EmissionColor );
			#else
				float4 staticSwitch45 = temp_cast_1;
			#endif
			float4 emission38 = staticSwitch45;
			float4 temp_cast_2 = (0.0).xxxx;
			float3 normalizeResult24 = normalize( i.viewDir );
			float dotResult26 = dot( bump33 , normalizeResult24 );
			float saferPower30 = abs( ( 1.0 - saturate( dotResult26 ) ) );
			#ifdef _RIM_ON
				float4 staticSwitch43 = ( pow( saferPower30 , _RimPower ) * _RimIntensity * _RimColor );
			#else
				float4 staticSwitch43 = temp_cast_2;
			#endif
			float4 rim40 = staticSwitch43;
			o.Emission = ( emission38 + rim40 ).rgb;
			float2 uv_MaskMap = i.uv_texcoord * _MaskMap_ST.xy + _MaskMap_ST.zw;
			float4 tex2DNode6 = tex2D( _MaskMap, uv_MaskMap );
			o.Metallic = ( _Metallic * tex2DNode6.g );
			o.Smoothness = ( _Glossiness * ( 1.0 - tex2DNode6.r ) );
			o.Occlusion = ( tex2DNode6.b * _OcclusionStrength );
			o.Alpha = 1;
			#if defined(_RENDERINGMODE_OPAQUE)
				float staticSwitch59 = 1.0;
			#elif defined(_RENDERINGMODE_CUTOUT)
				float staticSwitch59 = tex2DNode2.a;
			#else
				float staticSwitch59 = 1.0;
			#endif
			float alpha53 = staticSwitch59;
			clip( alpha53 - _Cutoff );
		}

		ENDCG
		CGPROGRAM
		#pragma exclude_renderers xbox360 xboxone ps4 psp2 n3ds wiiu switch nomrt 
		#pragma surface surf Standard keepalpha fullforwardshadows exclude_path:deferred nometa 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.viewDir = IN.tSpace0.xyz * worldViewDir.x + IN.tSpace1.xyz * worldViewDir.y + IN.tSpace2.xyz * worldViewDir.z;
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Legacy Shaders/VertexLit"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
256;732;1475;766;2959.482;-575.2681;1.504901;True;False
Node;AmplifyShaderEditor.CommentaryNode;49;-2507.782,136.4849;Inherit;False;863.5054;281.1183;;3;4;33;5;法线;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-2457.782,232.6032;Inherit;False;Property;_BumpScale;法线强度;5;0;Create;False;0;0;0;False;0;False;1;1;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;4;-2180.783,187.6032;Inherit;True;Property;_BumpMap;法线图;4;0;Create;False;0;0;0;False;1;Space(10);False;-1;None;None;True;0;False;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;47;-2507.148,1044.455;Inherit;False;1576.067;470;;14;40;43;46;32;31;51;30;29;28;27;26;24;35;23;边缘光;1,1,1,1;0;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;23;-2473.083,1169.455;Float;False;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;33;-1887.275,186.4849;Inherit;False;bump;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;35;-2301.083,1094.455;Inherit;False;33;bump;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NormalizeNode;24;-2269.083,1174.455;Inherit;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DotProductOpNode;26;-2109.083,1126.455;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;27;-1981.084,1126.455;Inherit;False;1;0;FLOAT;1.23;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;28;-1837.083,1142.455;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-1949.084,1206.455;Float;False;Property;_RimPower;边缘光力度;14;0;Create;False;0;0;0;False;0;False;0;0;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;48;-2507.045,510.847;Inherit;False;1004.241;449.7827;;6;20;15;45;14;38;44;自发光;1,1,1,1;0;0
Node;AmplifyShaderEditor.ColorNode;31;-1714.083,1337.455;Float;False;Property;_RimColor;边缘光颜色;15;0;Create;False;0;0;0;False;0;False;0,0,0,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;51;-1686.473,1257.795;Inherit;False;Property;_RimIntensity;边缘光强度;16;0;Create;False;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;20;-2372.494,752.6297;Inherit;False;Property;_EmissionColor;自发光颜色;12;0;Create;False;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;14;-2457.045,560.847;Inherit;True;Property;_EmissionMap;自发光图;11;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;30;-1661.083,1158.455;Inherit;False;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;-1487.083,1238.455;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;-2111.044,648.847;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;46;-1492.635,1157.661;Inherit;False;Constant;_Float1;Float 1;14;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;44;-2115.103,572.1999;Inherit;False;Constant;_Float0;Float 0;13;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;50;-2500.618,-391.4214;Inherit;False;968.0189;421.3002;;7;53;59;60;36;3;2;1;基础;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;60;-2138.912,-138.3357;Inherit;False;Constant;_alpha;alpha;17;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;1;-2366.619,-347.4214;Inherit;False;Property;_Color;颜色;1;1;[HDR];Create;False;0;0;0;False;1;Space(10);False;1,1,1,1;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-2451.618,-169.1211;Inherit;True;Property;_MainTex;基础图;2;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;45;-1961.42,621.581;Inherit;False;Property;_EMISSION;自发光开关;10;0;Create;False;0;0;0;False;1;Space(10);False;0;0;0;True;_EMISSION_ON;Toggle;2;Key0;Key1;Create;False;False;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;43;-1343.083,1206.455;Inherit;False;Property;_RIM;边缘光开关;13;0;Create;False;0;0;0;False;1;Space(10);False;0;0;0;True;_RIM_ON;Toggle;2;Key0;Key1;Create;False;False;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch;59;-1991.112,-98.23586;Inherit;False;Property;_RenderingMode;渲染模式;0;0;Create;False;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;2;Opaque;Cutout;Create;False;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;40;-1151.083,1206.455;Inherit;False;rim;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;6;-907.9711,597.741;Inherit;True;Property;_MaskMap;材质遮罩图;6;0;Create;False;0;0;0;False;1;Space(10);False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;38;-1745.802,621.6932;Inherit;False;emission;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-2134.618,-258.4211;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;53;-1752.018,-97.55655;Inherit;False;alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;41;-660.5118,997.1523;Inherit;False;40;rim;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;39;-661.3708,899.4719;Inherit;False;38;emission;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-749.6838,793.1347;Inherit;False;Property;_OcclusionStrength;AO强度;9;0;Create;False;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;52;-612.2278,609.6654;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;10;-748.6838,428.1347;Inherit;False;Property;_Glossiness;粗糙度;7;0;Create;False;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-748.6838,511.1347;Inherit;False;Property;_Metallic;金属度;8;0;Create;False;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;36;-1999.598,-262.0228;Inherit;False;base;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;34;-652.5316,340.1875;Inherit;False;33;bump;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;56;-511.8967,1087.208;Inherit;False;Property;_Cutoff;裁剪值;3;0;Create;False;0;0;0;False;0;False;0.5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-420.6838,723.1347;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-419.6838,535.1347;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;54;-456.9354,842.6777;Inherit;False;53;alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-419.6838,629.1347;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;42;-415.6208,941.647;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;37;-654.9953,263.2395;Inherit;False;36;base;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-68.69547,536.7583;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;VG/Scene/Base;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.08;True;True;0;True;TransparentCutout;;Geometry;ForwardOnly;8;d3d9;d3d11_9x;d3d11;glcore;gles;gles3;metal;vulkan;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;Legacy Shaders/VertexLit;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;True;56;0;0;0;False;0.1;False;-1;0;False;56;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;4;5;5;0
WireConnection;33;0;4;0
WireConnection;24;0;23;0
WireConnection;26;0;35;0
WireConnection;26;1;24;0
WireConnection;27;0;26;0
WireConnection;28;0;27;0
WireConnection;30;0;28;0
WireConnection;30;1;29;0
WireConnection;32;0;30;0
WireConnection;32;1;51;0
WireConnection;32;2;31;0
WireConnection;15;0;14;0
WireConnection;15;1;20;0
WireConnection;45;1;44;0
WireConnection;45;0;15;0
WireConnection;43;1;46;0
WireConnection;43;0;32;0
WireConnection;59;1;60;0
WireConnection;59;0;2;4
WireConnection;40;0;43;0
WireConnection;38;0;45;0
WireConnection;3;0;1;0
WireConnection;3;1;2;0
WireConnection;53;0;59;0
WireConnection;52;0;6;1
WireConnection;36;0;3;0
WireConnection;9;0;6;3
WireConnection;9;1;12;0
WireConnection;7;0;10;0
WireConnection;7;1;52;0
WireConnection;8;0;11;0
WireConnection;8;1;6;2
WireConnection;42;0;39;0
WireConnection;42;1;41;0
WireConnection;0;0;37;0
WireConnection;0;1;34;0
WireConnection;0;2;42;0
WireConnection;0;3;8;0
WireConnection;0;4;7;0
WireConnection;0;5;9;0
WireConnection;0;10;54;0
ASEEND*/
//CHKSM=0CDBCE91A63FB55CF60295158EFF81AFD7F74D4A