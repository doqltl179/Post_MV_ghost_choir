#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(EyeGenerator))]
public class EyeGeneratorCustomInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EyeGenerator generator = (EyeGenerator)target;

        //if (GUILayout.Button("Generate Character"))
        //{
        //    generator.GenerateMesh();
        //}
    }
}
#endif