// Copyright Elliot Bentine, 2018-
#ifndef COLORGRADING_INCLUDED
#define COLORGRADING_INCLUDED

#include "ScreenUtils.hlsl"

#define MAXCOLOR 16.0
#define RES 16.0
#define DITHER_SIZE 16.0

// Calculates the dither offset in the palette LUT.
//
// Returns a number in the range [0,1], which selects a particular row from the palette LUT.
// The row is selected using the screen position of the fragment relative to the object's position,
// and accounting for the macro pixel size, such that the dither pattern will occur using
// the macropixel grid.
inline float GetDitherPaletteOffset(float2 ditherUV) {
	return ((uint(ditherUV.x) % 4) * 4 + uint(ditherUV.y) % 4) / DITHER_SIZE;
}

// Maps the given float input, in the range [0,1], to an internal cell range on the LUT.
// Assumes that the input float is already clamped to the range (0,1).
inline float mapToCell(float input) {
	float half_px = 0.5 / (RES * RES);
	return half_px + input * (RES - 1) / (RES*RES);
}

inline void ColorGrade_float(Texture2D<float4> _palette, SamplerState sampler_palette, float4 orig, float2 ditherUV, out float4 graded)
{ 
	float half_px_x = 0.5 / (RES * RES);
	float half_px_y = 0.5 / (RES);
	orig = clamp(orig, 0.0, 1.0);

	// uv within one segment of RG space.
	float u = mapToCell(orig.r);
	float v = mapToCell(orig.g);

	// select cell using b
	float cell = clamp(floor(orig.b * MAXCOLOR), 0.0, RES - 1.0);
	u = cell / RES + u;

	// Use ditherUV to select palette row.
	float4 scaledScreenParams;
	GetScaledScreenParameters_float(scaledScreenParams);
	v = v + GetDitherPaletteOffset(ditherUV * scaledScreenParams.xy);

	// Perform the LUT
	graded = SAMPLE_TEXTURE2D(_palette, sampler_palette, float2(u, v));

	//Visualise dither pattern indices:
	//graded.rgb = GetDitherPaletteOffset(ditherUV * _ScreenParams.xy) * float3(1,1,1);
}

#endif