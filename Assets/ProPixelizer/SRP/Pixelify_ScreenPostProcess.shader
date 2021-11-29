// Copyright Elliot Bentine, 2018-
Shader "Hidden/ProPixelizer/SRP/Screen Post Process" {
	Properties{
		_OutlineDepthTestThreshold("Threshold used for depth testing outlines.", Float) = 0.0001
	}

		SubShader{
		Tags{
			"RenderType" = "TransparentCutout"
			"PreviewType" = "Plane"
		}

		Pass{
			Cull Off
			ZWrite On
			ZTest Off

			HLSLINCLUDE
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
				#include "PixelUtils.hlsl"
				#include "PackingUtils.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"		
			ENDHLSL

			HLSLPROGRAM
			#pragma target 2.5
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature DEPTH_TEST_OUTLINES_ON

			#if DEPTH_TEST_OUTLINES_ON
			float _OutlineDepthTestThreshold;
			#endif

		uniform sampler2D _MainTex;
		uniform sampler2D _Outlines;
		uniform sampler2D _Pixelised;
		TEXTURE2D(_PixelizationMap);
		SAMPLER(sampler_point_clamp);
		TEXTURE2D_X_FLOAT(_Depth);
		SAMPLER(sampler_Depth);
		float4 _TexelSize;
		float4 _Test;

		struct v2f {
			float4 pos : SV_POSITION;
			float4 scrPos:TEXCOORD1;
		};

		struct appdata_base
		{
			float4 vertex   : POSITION;  // The vertex position in model space.
			float3 normal   : NORMAL;    // The vertex normal in model space.
			float4 texcoord : TEXCOORD0; // The first UV coordinate.
		};

		v2f vert(appdata_base v) {
			v2f o;
			o.pos = TransformObjectToHClip(v.vertex.rgb);
			o.scrPos = ComputeScreenPos(o.pos);
			return o;
		}

		void frag(v2f i, out float4 color: COLOR) {
			float2 mainTexel = i.scrPos.xy;

			#if UNITY_UV_STARTS_AT_TOP
				//i.scrPos.y = 1 - i.scrPos.y;
			#endif

			float2 pShift = float2(_TexelSize.x, _TexelSize.y);
			float2 npos;
			float4 packedData = tex2D(_Outlines, i.scrPos.xy);
			float4 screenColor = tex2D(_MainTex, mainTexel);
			float4 pixelisedColor = tex2D(_Pixelised, i.scrPos.xy);
			float screenColorPixelSize = AlphaToPixelSize(pixelisedColor.a);

			float depth = SAMPLE_TEXTURE2D_X(_Depth, sampler_Depth, UnityStereoTransformScreenSpaceTex(i.scrPos.xy)).r;

			if (screenColorPixelSize < 1)
			{
				// if this pixel is not pixelised, just return main texture colour.
				color = screenColor;
				return;
			}

			// Determine outline threshold by comparing pixels from the macropixel centre, not the screen pixel.
			float4 PMpacked = SAMPLE_TEXTURE2D(_PixelizationMap, sampler_point_clamp, i.scrPos.xy);
			float2 macroPixelUV = UnpackPixelMapUV(PMpacked, _TexelSize);

			float4 outline_color;
			float ID, pixelSize;
			UnpackOutline(packedData, outline_color, ID, pixelSize);
			#if DEPTH_TEST_OUTLINES_ON
			float neighbourDepth;
			#endif
			float4 neighbourID, neighbourPC;
			float count = 0; // The number of similar pixels surrounding this one.
			[unroll]
			for (int u = -1; u <= 1; u++)
			{
				[unroll]
				for (int v = -1; v <= 1; v++)
				{
					npos = macroPixelUV + float2(u * pShift.x * pixelSize, v * pShift.y * pixelSize);
					neighbourID = tex2D(_Outlines, npos);
					neighbourPC = tex2D(_Pixelised, npos);
					#if DEPTH_TEST_OUTLINES_ON
						neighbourDepth = SAMPLE_TEXTURE2D_X(_Depth, sampler_Depth, npos).r;
						#if UNITY_REVERSED_Z
							bool neighbourInFront = neighbourDepth > depth + _OutlineDepthTestThreshold;
						#else
							bool neighbourInFront = neighbourDepth < depth - _OutlineDepthTestThreshold;
						#endif
						count += neighbourInFront || (getUID(neighbourID) == getUID(packedData) && AlphaToPixelSize(neighbourPC.a)) > 0.5 ? 1 : 0;
					#else
						count += getUID(neighbourID) == getUID(packedData) && AlphaToPixelSize(neighbourPC.a) > 0.5 ? 1 : 0;
					#endif
				}
			}
			float factor = count > 7 ? 0.0 : 1.0;
			color = lerp(pixelisedColor, float4(outline_color.rgb, 1.0), factor*outline_color.a);
			color.a = 1.0;
		}
		
		ENDHLSL
	}
	}
	FallBack "Diffuse"
}