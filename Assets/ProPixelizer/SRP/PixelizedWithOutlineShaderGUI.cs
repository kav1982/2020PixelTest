// Copyright Elliot Bentine, 2018-
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


public class PixelizedWithOutlineShaderGUI : ShaderGUI
{
    bool showColor, showAlpha, showPixelize, showLighting, showOutline;
    bool useColorGrading, useNormalMap, useEmission, useObjectPosition, useAlpha, useShadows;

    Material Material;

    public const string COLOR_GRADING_ON = "COLOR_GRADING_ON";
    public const string NORMAL_MAP_ON = "NORMAL_MAP_ON";
    public const string USE_EMISSION_ON = "USE_EMISSION_ON";
    public const string USE_OBJECT_POSITION_ON = "USE_OBJECT_POSITION_ON";
    public const string ALPHA_ON = "USE_ALPHA_ON";
    public const string RECEIVE_SHADOWS_ON = "RECEIVE_SHADOWS_ON";

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material = materialEditor.target as Material;
        useColorGrading = Material.IsKeywordEnabled(COLOR_GRADING_ON);
        useEmission = Material.IsKeywordEnabled(USE_EMISSION_ON);
        useNormalMap = Material.IsKeywordEnabled(NORMAL_MAP_ON);
        useObjectPosition = Material.IsKeywordEnabled(USE_OBJECT_POSITION_ON);
        useAlpha = Material.IsKeywordEnabled(ALPHA_ON);
        useShadows = Material.IsKeywordEnabled(RECEIVE_SHADOWS_ON);

        EditorGUILayout.LabelField("ProPixelizer | Appearance+Outline Material", EditorStyles.boldLabel);
        if (GUILayout.Button("User Guide")) Application.OpenURL("https://sites.google.com/view/propixelizer/user-guide");
        EditorGUILayout.Space();

        DrawAppearanceGroup(materialEditor, properties);
        DrawLightingGroup(materialEditor, properties);
        DrawPixelizeGroup(materialEditor, properties);
        DrawAlphaGroup(materialEditor, properties);
        DrawOutlineGroup(materialEditor, properties);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        //var enableInstancing = EditorGUILayout.ToggleLeft("Enable GPU Instancing", Material.enableInstancing);
        //Material.enableInstancing = enableInstancing;
        Material.enableInstancing = false;
        var renderQueue = EditorGUILayout.IntField("Render Queue", Material.renderQueue);
        Material.renderQueue = renderQueue;
        var dsgi = EditorGUILayout.ToggleLeft("Double Sided Global Illumination", Material.doubleSidedGI);
        Material.doubleSidedGI = dsgi;

        EditorUtility.SetDirty(Material);
    }

    public void DrawAppearanceGroup(MaterialEditor editor, MaterialProperty[] properties)
    {
        showColor = EditorGUILayout.BeginFoldoutHeaderGroup(showColor, "Appearance");
        if (showColor)
        {
            var albedo = FindProperty("Texture2D_FBC26130", properties);
            editor.TextureProperty(albedo, "Albedo", true);

            useColorGrading = EditorGUILayout.ToggleLeft("Color Grading", useColorGrading);
            if (useColorGrading)
            {
                var lut = FindProperty("Texture2D_A4CD04C4", properties);
                editor.ShaderProperty(lut, "Palette");
                Material.EnableKeyword(COLOR_GRADING_ON);
            }
            else
                Material.DisableKeyword(COLOR_GRADING_ON);

            useNormalMap = EditorGUILayout.ToggleLeft("Normal Map", useNormalMap);
            if (useNormalMap)
            {
                var normal = FindProperty("Texture2D_4084966E", properties);
                editor.TextureProperty(normal, "Normal Map", true);
                Material.EnableKeyword(NORMAL_MAP_ON);
            }
            else
                Material.DisableKeyword(NORMAL_MAP_ON);

            useEmission = EditorGUILayout.ToggleLeft("Emission", useEmission);
            if (useEmission)
            {
                var normal = FindProperty("Texture2D_9A2EA9A0", properties);
                editor.TextureProperty(normal, "Emission", true);
                Material.EnableKeyword(USE_EMISSION_ON);
            }
            else
                Material.DisableKeyword(USE_EMISSION_ON);

        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    public void DrawLightingGroup(MaterialEditor editor, MaterialProperty[] properties)
    {
        showLighting = EditorGUILayout.BeginFoldoutHeaderGroup(showLighting, "Lighting");
        if (showLighting)
        {
            var ramp = FindProperty("Texture2D_F406AA7C", properties);
            editor.ShaderProperty(ramp, "Lighting Ramp");

            var ambient = FindProperty("Vector3_C98FB62A", properties);
            editor.ShaderProperty(ambient, "Ambient Light");

            useShadows = EditorGUILayout.ToggleLeft("Receive shadows", useShadows);
            if (useShadows)
                Material.EnableKeyword(RECEIVE_SHADOWS_ON);
            else
                Material.DisableKeyword(RECEIVE_SHADOWS_ON);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    public void DrawPixelizeGroup(MaterialEditor editor, MaterialProperty[] properties)
    {
        showPixelize = EditorGUILayout.BeginFoldoutHeaderGroup(showPixelize, "Pixelize");
        if (showPixelize)
        {
            var pixelSize = FindProperty("_PixelSize", properties);
            editor.ShaderProperty(pixelSize, "Pixel Size");

            useObjectPosition = EditorGUILayout.ToggleLeft("Use object position as pixel grid origin", useObjectPosition);
            EditorGUILayout.HelpBox("For more information, see the 'Aligning Pixel Grids' section in the user guide.", MessageType.Info);
            if (!useObjectPosition)
            {
                var gridPosition = FindProperty("_PixelGridOrigin", properties);
                editor.ShaderProperty(gridPosition, "Origin (world space)");
                Material.DisableKeyword(USE_OBJECT_POSITION_ON);
                Material.SetFloat("USE_OBJECT_POSITION", 0.0f);
            }
            else
            {
                Material.EnableKeyword(USE_OBJECT_POSITION_ON);
                Material.SetFloat("USE_OBJECT_POSITION", 1.0f);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    public void DrawAlphaGroup(MaterialEditor editor, MaterialProperty[] properties)
    {
        showAlpha = EditorGUILayout.BeginFoldoutHeaderGroup(showAlpha, "Alpha Cutout");
        if (showAlpha)
        {
            useAlpha = EditorGUILayout.ToggleLeft("Alpha Cutout", useAlpha);
            if (useAlpha)
            {
                var threshold = FindProperty("_AlphaClipThreshold", properties);
                editor.ShaderProperty(threshold, "Threshold");
                Material.EnableKeyword(ALPHA_ON);
            }
            else
                Material.DisableKeyword(ALPHA_ON);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    public void DrawOutlineGroup(MaterialEditor editor, MaterialProperty[] properties)
    {
        showOutline = EditorGUILayout.BeginFoldoutHeaderGroup(showOutline, "Outline");
        if (showOutline)
        {
            var oID = FindProperty("_ID", properties);
            editor.ShaderProperty(oID, "ID");
            EditorGUILayout.HelpBox("The ID is an 8-bit number used to identify different objects in the " +
                "buffer for purposes of drawing outlines. Outlines are drawn when a pixel is next to a pixel " +
                "of different ID value.", MessageType.Info);
            var outlineColor = FindProperty("_OutlineColor", properties);
            editor.ShaderProperty(outlineColor, "Outline Color");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }
}
#endif