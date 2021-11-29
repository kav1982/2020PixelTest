// Copyright Elliot Bentine, 2018-
Shader "ProPixelizer/SRP/Object Outline"
{
	//A shader that renders outline buffer data for pixelated objects.

	Properties {
		[Header(Outline control)]
		[Space(5)]
		[IntRange] _ID("ID", Range(0, 255)) = 1 // A unique ID used to differentiate objects for purposes of outlines.
		_OutlineColor("Outline Color", Color) = (0.5, 0.5, 0.5, 1.0) 
		
		[Space(15)]
		[Header(Pixelisation)]
		[Space(5)]
		[IntRange] _PixelSize("Pixel Size", Range(1,5)) = 3
		[Toggle(USE_OBJECT_POSITION_ON)] _UseObjectPositionForGridOrigin("Use Object Position as Grid Origin", Float) = 1
		_PixelGridOrigin("Pixel Grid Origin", Vector) = (0,0,0,0)
		[Space(15)]
		[Header(Alpha cutout)]
		[Space(5)]
		[Toggle(USE_ALPHA_ON)] _UseAlphaClipping("Enable using the alpha cutout texture", Float) = 1
		[MainTex] Texture2D_FBC26130("Alpha Cutout Texture", 2D) = "white" {}
		_AlphaClipThreshold("Alpha Cutout Threshold", Range(0, 1)) = 0.5
	}

	SubShader {
		Tags{
		"RenderType" = "TransparentCutout"
		"PreviewType" = "Plane"
		}

		Pass
		{ 
			Name "OutlinePass"
			Tags {
				"LightMode" = "Outlines"
				"DisableBatching" = "True"
			}

			ZWrite On
			Cull Off
			Blend Off

			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "PixelUtils.hlsl"
			#include "PackingUtils.hlsl"
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.5
			#pragma multi_compile USE_OBJECT_POSITION_ON _
			#pragma multi_compile USE_ALPHA_ON _

			CBUFFER_START(UnityPerMaterial)
				float _ID;
				float4 _OutlineColor;
				float4 _PixelGridOrigin;
				float _PixelSize;
				float4 Texture2D_FBC26130_ST;
				#if defined(USE_ALPHA_ON)
				float _AlphaClipThreshold;
				#endif
			CBUFFER_END

			#if defined(USE_ALPHA_ON)
			TEXTURE2D(Texture2D_FBC26130);
			SAMPLER(sampler_Texture2D_FBC26130);
			#endif

			struct appdata
			{
				float4 vertex : POSITION; // vertex position
				#if defined(USE_ALPHA_ON)
				float2 uv : TEXCOORD0; // texture coordinate
				#endif
			};

			struct Varyings {
				float4 pos : SV_POSITION; // clip space position
				#if defined(USE_ALPHA_ON)
				float2 uv : TEXCOORD0; //texture coordinate
				#endif
			};

			Varyings vert(
				appdata data
			)
			{
				Varyings output = (Varyings)0;
				VertexPositionInputs vertexInput = GetVertexPositionInputs(data.vertex.xyz);
				output.pos = float4(vertexInput.positionCS);
				
				#if defined(USE_ALPHA_ON)
					output.uv = TRANSFORM_TEX(data.uv, Texture2D_FBC26130);
				#endif
				return output;
			}

			void frag(Varyings i, out float4 color : COLOR)
			{
				#if defined(USE_ALPHA_ON)
					float alpha = step(_AlphaClipThreshold, SAMPLE_TEXTURE2D(Texture2D_FBC26130, sampler_Texture2D_FBC26130, i.uv).a);
				#else
					float alpha = 1;
				#endif 

				float alpha_out;
				float2 dither_uv;
				#if defined(USE_OBJECT_POSITION_ON)
					float4 object_pixel_pos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1));
					PixelClipAlpha_float(unity_MatrixVP, object_pixel_pos.xyz, floor(_ScaledScreenParams), i.pos, round(_PixelSize), alpha, alpha_out, dither_uv);
				#else
					PixelClipAlpha_float(unity_MatrixVP, _PixelGridOrigin.xyz, floor(_ScaledScreenParams), i.pos, round(_PixelSize), alpha, alpha_out, dither_uv);
				#endif
				clip(alpha_out - 0.001);
				PackOutline(_OutlineColor, _ID, round(_PixelSize), color);
			}
			ENDHLSL
		}

	}
}