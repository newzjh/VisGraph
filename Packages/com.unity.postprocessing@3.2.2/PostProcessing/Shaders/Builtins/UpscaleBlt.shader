
Shader "PostProcessing/UpscaleBlt"
{
	HLSLINCLUDE
	
	
	uniform half _CentralFactor;
	uniform half _SideFactor;



	struct VertexOutput
	{
		float4 vertex: SV_POSITION;
		float2 texcoord: TEXCOORD0;
		float4 texcoord1  : TEXCOORD1;
	};

	struct AttributesDefault
	{
		float3 vertex : POSITION;
	};

	float2 TransformTriangleVertexToUV(float2 vertex)
	{
		float2 uv = (vertex + 1.0) * 0.5;
		return uv;
	}

	
	sampler2D _MainTex;
	float4 _MainTex_TexelSize;

	VertexOutput Vert(AttributesDefault v)
	{
		VertexOutput o;
		o.vertex = float4(v.vertex.xy, 0.0, 1.0);
		o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);
#if UNITY_UV_STARTS_AT_TOP
		o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif
		o.texcoord1 = half4(o.texcoord.xy - _MainTex_TexelSize.xy, o.texcoord.xy + _MainTex_TexelSize.xy);
		return o;
	}

	half4 Frag(VertexOutput i) : SV_Target
	{
		//return i.texcoord1;

		float2 texcoord = i.texcoord;
		//texcoord.y = 1 - texcoord.y;
		float4 texcoord1 = i.texcoord1;
		//texcoord1.y = 1 - texcoord1.y;
		//texcoord1.w = 1 - texcoord1.w;

		half4 color = tex2D(_MainTex,  texcoord.xy) * _CentralFactor;
		color -= tex2D(_MainTex,  texcoord1.xy) * _SideFactor;
		color -= tex2D(_MainTex,  texcoord1.xw) * _SideFactor;
		color -= tex2D(_MainTex,  texcoord1.zy) * _SideFactor;
		color -= tex2D(_MainTex,  texcoord1.zw) * _SideFactor;
		return color;
	}
	
	ENDHLSL
	

	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex Vert
			#pragma fragment Frag
			
			ENDHLSL
			
		}
	}
}

    
