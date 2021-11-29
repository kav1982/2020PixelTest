﻿// This file contains shader code required for custom lighting calculations in unity. 
// The original source of this file can be found here:
// https://github.com/Unity-Technologies/ShaderGraph-Custom-Lighting/blob/master/Assets/Includes/CustomLighting.hlsl
//
// It comes from an associated Unity blog post about implementing custom lighting in the SRP, found here:
// https://blogs.unity3d.com/2019/07/31/custom-lighting-in-shader-graph-expanding-your-graphs-in-2019/

#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED
 
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"

#if VERSION_GREATER_EQUAL(10, 0)
	#define SHADERGRAPH_PREVIEW_TEST defined(SHADERGRAPH_PREVIEW)
#else
	#define SHADERGRAPH_PREVIEW_TEST SHADERGRAPH_PREVIEW
#endif

void MainLightShadow_float(float4 positionWS, out float shadow) {
#if (defined(_MAIN_LIGHT_SHADOWS) || defined(SHADOWS)) && defined(RECEIVE_SHADOWS_ON)
#if SHADOWS_SCREEN
	float4 clipPos = TransformWorldToHClip(positionWS);
	float4 shadowCoord = ComputeScreenPos(clipPos);
#else
	float4 shadowCoord = TransformWorldToShadowCoord(positionWS.xyz);
#endif
	shadow = MainLightRealtimeShadow(shadowCoord);
#else
	shadow = 1.0;
#endif
}

void MainLightShadow_half(float4 positionWS, out half shadow) {
#if (defined(_MAIN_LIGHT_SHADOWS) || defined(SHADOWS)) && defined(RECEIVE_SHADOWS_ON)
#if SHADOWS_SCREEN
	float4 clipPos = TransformWorldToHClip(positionWS);
	float4 shadowCoord = ComputeScreenPos(clipPos);
#else
	float4 shadowCoord = TransformWorldToShadowCoord(positionWS.xyz);
#endif
	shadow = MainLightRealtimeShadow(shadowCoord);
#else
	shadow = 1.0;
#endif
}

void MainLight_float(float3 WorldPos, out float3 Direction, out float3 Color, out float DistanceAtten) {
#if SHADERGRAPH_PREVIEW_TEST
	Direction = float3(0.5, 0.5, 0);
	Color = 1;
	DistanceAtten = 1;
#else
	Light mainLight = GetMainLight();
	Direction = mainLight.direction;
	Color = mainLight.color;
	DistanceAtten = mainLight.distanceAttenuation;
#endif
}

void MainLight_half(float3 WorldPos, out half3 Direction, out half3 Color, out half DistanceAtten)
{
#if SHADERGRAPH_PREVIEW_TEST
	Direction = half3(0.5, 0.5, 0);
	Color = 1;
	DistanceAtten = 1;
#else
	Light mainLight = GetMainLight();
	Direction = mainLight.direction;
	Color = mainLight.color;
	DistanceAtten = mainLight.distanceAttenuation;
#endif
}

void DirectSpecular_float(float3 Specular, float Smoothness, float3 Direction, float3 Color, float3 WorldNormal, float3 WorldView, out float3 Out)
{
#if SHADERGRAPH_PREVIEW_TEST
	Out = 0;
#else
	Smoothness = exp2(10 * Smoothness + 1);
	WorldNormal = normalize(WorldNormal);
	WorldView = SafeNormalize(WorldView);
	Out = LightingSpecular(Color, Direction, WorldNormal, WorldView, float4(Specular, 0), Smoothness);
#endif
}

void DirectSpecular_half(half3 Specular, half Smoothness, half3 Direction, half3 Color, half3 WorldNormal, half3 WorldView, out half3 Out)
{
#if SHADERGRAPH_PREVIEW_TEST
	Out = 0;
#else
	Smoothness = exp2(10 * Smoothness + 1);
	WorldNormal = normalize(WorldNormal);
	WorldView = SafeNormalize(WorldView);
	Out = LightingSpecular(Color, Direction, WorldNormal, WorldView, half4(Specular, 0), Smoothness);
#endif
}

void AdditionalLights_float(float3 SpecColor, float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, out float3 Diffuse, out float3 Specular)
{
	float3 diffuseColor = 0;
	float3 specularColor = 0;

#if SHADERGRAPH_PREVIEW_TEST

#else
	Smoothness = exp2(10 * Smoothness + 1);
	WorldNormal = normalize(WorldNormal);
	WorldView = SafeNormalize(WorldView);
	int pixelLightCount = GetAdditionalLightsCount();
	for (int i = 0; i < pixelLightCount; ++i)
	{
		Light light = GetAdditionalLight(i, WorldPosition);
		half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
		diffuseColor += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
		specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, float4(SpecColor, 0), Smoothness);
	}
#endif

	Diffuse = diffuseColor;
	Specular = specularColor;
}

void AdditionalLights_half(half3 SpecColor, half Smoothness, half3 WorldPosition, half3 WorldNormal, half3 WorldView, out half3 Diffuse, out half3 Specular)
{
	half3 diffuseColor = 0;
	half3 specularColor = 0;

#if SHADERGRAPH_PREVIEW_TEST

#else
	Smoothness = exp2(10 * Smoothness + 1);
	WorldNormal = normalize(WorldNormal);
	WorldView = SafeNormalize(WorldView);
	int pixelLightCount = GetAdditionalLightsCount();
	for (int i = 0; i < pixelLightCount; ++i)
	{
		Light light = GetAdditionalLight(i, WorldPosition);
		half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
		diffuseColor += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
		specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, half4(SpecColor, 0), Smoothness);
	}
#endif

	Diffuse = diffuseColor;
	Specular = specularColor;
}

#endif