// Copyright Elliot Bentine, 2018-
Shader "ProPixelizer/SRP/PixelizedWithOutline"
{
	//A shader that renders outline buffer data and color appearance for pixelated objects.

    Properties
    {
		[NoScaleOffset]Texture2D_F406AA7C("LightingRamp", 2D) = "white" {}
		[NoScaleOffset]Texture2D_A4CD04C4("Palette", 2D) = "white" {}
		[MainTex]Texture2D_FBC26130("Albedo", 2D) = "white" {}
		Vector3_C98FB62A("AmbientLight", Vector) = (0.2, 0.2, 0.2, 0)
		[IntRange] _PixelSize("Pixel Size", Range(1,5)) = 3
		_PixelGridOrigin("PixelGridOrigin", Vector) = (0, 0, 0, 0)
		[NoScaleOffset]Texture2D_4084966E("Normal Map", 2D) = "white" {}
		[NoScaleOffset]Texture2D_9A2EA9A0("Emission", 2D) = "white" {}
		_AlphaClipThreshold("AlphaClipThreshold", Float) = 0.5
		[Toggle]COLOR_GRADING("Use Color Grading", Float) = 0
		[Toggle]RECEIVE_SHADOWS("ReceiveShadows", Float) = 1
		[Toggle]USE_OBJECT_POSITION("UseObjectPosition", Float) = 1
		[Toggle]NORMAL_MAP("Use Normal Maps", Float) = 0
		[Toggle]USE_EMISSION("Use Emission", Float) = 0
		[Toggle]USE_ALPHA("UseAlphaClip", Float) = 0
		[IntRange] _ID("ID", Range(0, 255)) = 1 // A unique ID used to differentiate objects for purposes of outlines.
		_OutlineColor("Outline Color", Color) = (0.5, 0.5, 0.5, 1.0)
    }
    SubShader
    {
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
		
		UsePass "ProPixelizer/SRP/Pixelised/UNIVERSAL FORWARD"
		UsePass "ProPixelizer/SRP/Pixelised/UNIVERSAL FORWARD"
		UsePass "ProPixelizer/SRP/Pixelised/SHADOWCASTER"
		UsePass "ProPixelizer/SRP/Pixelised/DEPTHONLY"
		
		//UsePass "ProPixelizer/SRP/Object Outline/OUTLINEPASS"
		
		// Note: For now there is an error with UsePass in builds
		//	- see here: https://forum.unity.com/threads/usepass-what-exactly-does-it-do.1091569/
		//
		// This will be replaced with UsePass in future once the bug is fixed,
		// but for now the pass is replicated here.

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
	CustomEditor "PixelizedWithOutlineShaderGUI"
	FallBack "ProPixelizer/SRP/Pixelised"
}
