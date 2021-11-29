// Copyright Elliot Bentine, 2018-
#ifndef PIXELUTILS_INCLUDED
#define PIXELUTILS_INCLUDED
#define ROUNDING_PREC 0.49
#define SCRN_OFFSET 2000.0

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"
#if VERSION_GREATER_EQUAL(10, 0)
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
#endif
#include "PackingUtils.hlsl"

inline float mod(float x, float y)
{
	return x - y * floor(x / y);
}

inline void PixelClipAlpha_float(float4x4 unity_MatrixVP, float3 objectCentreWS, float4 screenParams, float4 posCS, float macroPixelSize, float alpha_in, out float alpha_out, out float2 ditherUV) {

#if VERSION_GREATER_EQUAL(10,0)
#define SHADOWTEST ( SHADERPASS == SHADERPASS_SHADOWCASTER )
#else
#define SHADOWTEST defined(SHADERPASS_SHADOWCASTER)
#endif

	screenParams = floor(screenParams);

#if SHADOWTEST
	alpha_out = alpha_in;
	ditherUV = float2(0.0, 0.0);
#else
	//posCS is the position of the fragment, in (integer) coordinates of screen pixel location.

	//Get object position in screen pixels
	float4 objClipPos = mul(unity_MatrixVP, float4(objectCentreWS, 1));
	float2 objViewPos = objClipPos.xy / objClipPos.w;
	float2 objPixelPos = (0.5 * (objViewPos.xy + float2(1,1)) * screenParams.xy);
	float xfactor, yfactor;
	float pixelSize = round(macroPixelSize);

	//For perspective, objPixelPos must be rounded to the nearest pixel to prevent float precision errors (causing tearing in perspective)
	if (unity_OrthoParams.w < 0.5)
	{
		objPixelPos = round(objPixelPos);
	}

	// Make directX and openGL consistent.
#if UNITY_UV_STARTS_AT_TOP
	objPixelPos.y = screenParams.y - objPixelPos.y;
#else

#endif

	//Clip according to object position and pixel position.
	alpha_in = clamp(round(alpha_in), 0.0, 1.0);

	// posCS is half-pixel coord. objPixelPos is also half-pixel coord.
	// We take floor to round them to integer, then include a half so that mod(x,y) consistently returns the correct value,
	//   eg 3.0+bias mod 3 = bias, whereas 3 mod 3 != 0 but 2.999 on some cards.
	float2 delta = floor(floor(posCS.xy) - floor(objPixelPos.xy+0.1)) + 0.01;
	xfactor = step(mod(delta.x, pixelSize), 0.1);
	yfactor = step(mod(delta.y, pixelSize), 0.1);
	ditherUV = delta / pixelSize / screenParams.xy + float2(0.5, 0.5);

	// Always draw edge pixels
	float x_edge = min(1.0, step(posCS.x, 1) + step(screenParams.x - 1, posCS.x));
	float y_edge = min(1.0, step(posCS.y, 1) + step(screenParams.y - 1, posCS.y));
	float draw = min(1.0, (x_edge + xfactor) * (y_edge + yfactor));
	alpha_out = alpha_in * draw * PixelSizeToAlpha(pixelSize);
#endif
}

#endif