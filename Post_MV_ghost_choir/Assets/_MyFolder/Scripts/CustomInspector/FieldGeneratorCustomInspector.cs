#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FieldGenerator))]
public class FieldGeneratorCustomInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        FieldGenerator generator = (FieldGenerator)target;

        if (GUILayout.Button("Load Mesh"))
        {
            generator.LoadFieldMesh();
        }

        if (GUILayout.Button("Apply Texture"))
        {
            generator.ApplyTexture();
        }

        if (GUILayout.Button("Load Texture"))
        {
            generator.LoadFieldTexture();
        }
    }
}
#endif