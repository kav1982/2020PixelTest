// Copyright Elliot Bentine, 2018-
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

#if (UNITY_EDITOR) 

/// <summary>
/// A tool for verifying that materials are correctly serialized.
/// </summary>
public class VerifyMaterials : EditorWindow
{
    [MenuItem("Window/ProPixelizer/Verify Materials")]
    public static void ShowWindow()
    {
        GetWindow(typeof(VerifyMaterials));
    }
    
    void OnGUI()
    {
        //GUILayout.Label("TextureIndexer", EditorStyles.largeLabel);
        EditorGUILayout.LabelField("ProPixelizer | Verify Materials", EditorStyles.boldLabel);
        if (GUILayout.Button("User Guide")) Application.OpenURL("https://sites.google.com/view/propixelizer/user-guide");
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This tool checks ProPixelizer materials in the project to make sure they are correctly serialized, and fixes any broken keywords.", MessageType.Info);
        //EditorGUILayout.LabelField("");
        EditorGUILayout.Space();
        if (GUILayout.Button("Verify Shaders", EditorStyles.miniButton))
        {
            VerifyShaders();
        }
    }

    void VerifyShaders()
    {
        var outlineShader = Shader.Find("ProPixelizer/SRP/Object Outline");
        FixUseObjectPositionKeywords(outlineShader, "_UseObjectPositionForGridOrigin");
        var compositeShader = Shader.Find("ProPixelizer/SRP/PixelizedWithOutline");
        FixUseObjectPositionKeywords(compositeShader, "USE_OBJECT_POSITION");
    }

    void FixUseObjectPositionKeywords(Shader shader, string serializedPropertyName)
    {
        string shaderPath = AssetDatabase.GetAssetPath(shader);
        string[] allMaterials = AssetDatabase.FindAssets("t:Material");

        foreach (string materialID in allMaterials)
        {
            var path = AssetDatabase.GUIDToAssetPath(materialID);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path) as Material;

            if (material.shader != shader)
                continue;

            Debug.Log("Updating material: " + path);
            if (material.GetFloat(serializedPropertyName) > 0.1f)
            {
                material.EnableKeyword("USE_OBJECT_POSITION_ON");
            }
            else
            {
                material.DisableKeyword("USE_OBJECT_POSITION_ON");
            }
        }
    }
}

#endif