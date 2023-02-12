#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(TestScript))]
[CanEditMultipleObjects]
public class TestScriptCustomInspector : Editor
{
    private SerializedProperty _calculateDot;
    private SerializedProperty _calculateDotProperties;

    private SerializedProperty _canvas;

    private SerializedProperty _showVideoProperties;
    private SerializedProperty _textFont;

    private SerializedProperty _showAudioVisualizer;
    private SerializedProperty _showAudioVisualizerProperties;

    private SerializedProperty _testCharacter;

    private void OnEnable()
    {
        _calculateDot = serializedObject.FindProperty("_calculateDot");
        _calculateDotProperties = serializedObject.FindProperty("_dotProperties");

        _canvas = serializedObject.FindProperty("_canvas");

        _showVideoProperties = serializedObject.FindProperty("_showVideoProperties");
        _textFont = serializedObject.FindProperty("_textFont");

        _showAudioVisualizer = serializedObject.FindProperty("_showAudioVisualizer");
        _showAudioVisualizerProperties = serializedObject.FindProperty("_audioVisualizerProperties");

        _testCharacter = serializedObject.FindProperty("_testCharacter");
    }

    public override void OnInspectorGUI()
    {
        TestScript testScript = (TestScript)target;
        serializedObject.Update();

        EditorGUILayout.PropertyField(_calculateDot);
        if (testScript._calculateDot)
        {
            EditorGUILayout.PropertyField(_calculateDotProperties);
        }

        EditorGUILayout.PropertyField(_canvas);

        EditorGUILayout.PropertyField(_showVideoProperties);
        if(testScript._showVideoProperties)
        {
            EditorGUILayout.PropertyField(_textFont);
        }

        EditorGUILayout.PropertyField(_showAudioVisualizer);
        if(testScript._showAudioVisualizer)
        {
            EditorGUILayout.PropertyField(_showAudioVisualizerProperties);
        }

        EditorGUILayout.PropertyField(_testCharacter);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif