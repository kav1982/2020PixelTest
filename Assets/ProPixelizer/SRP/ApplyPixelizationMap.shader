// Copyright Elliot Bentine, 2018-
// 
// Applies a pixelization map to _MainTex.
Shader "Hidden/ProPixelizer/SRP/ApplyPixelizationMap" {
	Properties{
	}

	SubShader{
	Tags{
		"RenderType" = "Opaque"
		"PreviewType" = "Plane"
		"RenderPipeline" = "UniversalPipeline"
	}

	Pass{
		Cull Off
		ZWrite On
		ZTest Off
		Blend Off

		HLSLPROGRAM 
		#pragma vertex vert
		#pragma fragment frag
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "PixelUtils.hlsl"
		#include "PackingUtils.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

		TEXTURE2D(_PixelizationMap);
		TEXTURE2D(_MainTex);
		SAMPLER(sampler_point_clamp);
		float4 _MainTex_TexelSize;

		struct v2f {
			float4 pos : SV_POSITION;
			float4 scrPos:TEXCOORD1;
		};

		struct appdata_base
		{
			float4 vertex   : POSITION;  // The vertex position in model space.
			float4 texcoord : TEXCOORD0; // The first UV coordinate.
		};

		v2f vert(appdata_base v) {
			v2f o;
			o.pos = TransformObjectToHClip(v.vertex.xyz);
			o.scrPos = ComputeScreenPos(o.pos);
			return o;
		}

		void frag(v2f i, out float4 color: COLOR, out float depth : SV_DEPTH) {
			float4 packed = SAMPLE_TEXTURE2D(_PixelizationMap, sampler_point_clamp, i.scrPos.xy);
			float2 uvs = UnpackPixelMapUV(packed, _MainTex_TexelSize); 
			color = SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, uvs.xy);
			depth = SampleSceneDepth(uvs.xy);
		}
		ENDHLSL
	}
	}
	FallBack "Diffuse"
	}