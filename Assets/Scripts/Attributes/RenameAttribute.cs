using UnityEngine;
using UnityEditor;

public class RenameAttribute : PropertyAttribute
{
    public string NewName { get; private set; }
    public RenameAttribute(string name)
    {
        NewName = name;
    }
}

[CustomPropertyDrawer(typeof(RenameAttribute))]
public class RenameEditor : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, new GUIContent((attribute as RenameAttribute).NewName));
    }
}