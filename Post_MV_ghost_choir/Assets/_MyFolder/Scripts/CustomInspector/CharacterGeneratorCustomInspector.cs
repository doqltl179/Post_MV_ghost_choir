#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterGenerator))]
public class CharacterGeneratorCustomInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        CharacterGenerator generator = (CharacterGenerator)target;

        if (GUILayout.Button("Generate Character"))
        {
            generator.Create();
        }
    }
}
#endif