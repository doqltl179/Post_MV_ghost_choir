using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class ReadOnlyAttribute : PropertyAttribute
{

}

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif

[Serializable]
public class Blend
{
#if UNITY_EDITOR
    [ReadOnlyAttribute]
#endif
    public string BlendName;

    private Vector3[] _blendVertices;
    private Vector3[] _blendVerticesDiff;

    /// <summary>
    /// _blendVerticesDiff * BlendStrength
    /// </summary>
    public Vector3[] BlendVerts { get; private set; }

    [Range(0f, 1f)] public float BlendStrength = 0f;
    private float _blendStrengthSaver = 0f;

    public float BlendSize { get; private set; }


    public Blend(string blendName, Vector3[] originalVertices, Vector3[] blendVertices, float blendStrength = 0f, float blendSize = 1f)
    {
        BlendName = blendName;

        _blendVerticesDiff = new Vector3[originalVertices.Length];
        for (int i = 0; i < _blendVerticesDiff.Length; i++)
        {
            _blendVerticesDiff[i] = blendVertices[i] * blendSize - originalVertices[i];
        }

        _blendVertices = blendVertices;
        BlendStrength = blendStrength;

        BlendSize = blendSize;

        BlendVerts = new Vector3[_blendVertices.Length];
    }

    public void StrengthChanged()
    {
        if (BlendStrength != _blendStrengthSaver)
        {
            for (int i = 0; i < BlendVerts.Length; i++)
            {
                BlendVerts[i] = _blendVerticesDiff[i] * BlendStrength;
            }

            _blendStrengthSaver = BlendStrength;
        }
    }

    public void StrengthChanged(ref bool isChanged)
    {
        if (BlendStrength != _blendStrengthSaver)
        {
            for (int i = 0; i < BlendVerts.Length; i++)
            {
                BlendVerts[i] = _blendVerticesDiff[i] * BlendStrength;
            }

            _blendStrengthSaver = BlendStrength;

            isChanged = true;
        }
    }

    public void InitializePropertySavers()
    {
        _blendStrengthSaver = 0f;
    }

    internal object Where(Func<object, bool> p)
    {
        throw new NotImplementedException();
    }
}
