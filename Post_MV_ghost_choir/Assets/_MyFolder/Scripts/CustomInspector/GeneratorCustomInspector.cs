#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(Generator))]
public class GeneratorCustomInspector : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var inspector = new VisualElement();

        var blends = new PropertyField(serializedObject.FindProperty("_blends"));

        inspector.Add(blends);

        blends.schedule.Execute(() =>
        {
            var sizeField = blends.Q<IntegerField>();

            sizeField.SetEnabled(false);
        });

        return inspector;
    }
}
#endif