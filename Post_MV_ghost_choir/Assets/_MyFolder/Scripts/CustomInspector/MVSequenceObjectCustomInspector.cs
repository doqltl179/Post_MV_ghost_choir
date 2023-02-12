#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MVSequenceObject))]
[CanEditMultipleObjects]
public class MVSequenceObjectCustomInspector : Editor
{
    private SerializedProperty _startNormalTime;

    private SerializedProperty _changeCameraSetting;
    private SerializedProperty _sequenceCameraSetting;

    private SerializedProperty _changeCharacterSetting;
    private SerializedProperty _sequenceCharacterSettings;

    private SerializedProperty _tooltip;

    private void OnEnable()
    {
        _startNormalTime = serializedObject.FindProperty("_startNormalTime");

        _changeCameraSetting = serializedObject.FindProperty("_changeCameraSetting");
        _sequenceCameraSetting = serializedObject.FindProperty("_sequenceCameraSetting");

        _changeCharacterSetting = serializedObject.FindProperty("_changeCharacterSetting");
        _sequenceCharacterSettings = serializedObject.FindProperty("_sequenceCharacterSettings");

        _tooltip = serializedObject.FindProperty("_tooltip");
    }

    public override void OnInspectorGUI()
    {
        MVSequenceObject _mvSequenceObject = (MVSequenceObject)target;
        serializedObject.Update();

        EditorGUILayout.PropertyField(_startNormalTime);

        EditorGUILayout.PropertyField(_changeCameraSetting);
        if (_mvSequenceObject.ChangeCameraSetting)
        {
            EditorGUILayout.PropertyField(_sequenceCameraSetting);
        }

        EditorGUILayout.PropertyField(_changeCharacterSetting);
        if (_mvSequenceObject.ChangeCharacterSetting)
        {
            EditorGUILayout.PropertyField(_sequenceCharacterSettings);
        }

        EditorGUILayout.PropertyField(_tooltip);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif