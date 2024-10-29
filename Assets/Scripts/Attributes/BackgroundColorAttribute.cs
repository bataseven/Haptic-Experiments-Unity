using UnityEngine;
using UnityEditor;

public enum FieldColor
{
    Red,
    Green,
    Blue,
    Yellow,
    Orange
}

public class RequiredField : PropertyAttribute
{
    public Color color;

    public RequiredField(FieldColor _color = FieldColor.Red)
    {
        switch (_color)
        {
            case FieldColor.Red:
                color = Color.red;
                break;
            case FieldColor.Green:
                color = Color.green;
                break;
            case FieldColor.Blue:
                color = Color.blue;
                break;
            case FieldColor.Yellow:
                color = Color.yellow;
                break;
            case FieldColor.Orange:
                color = new Color(1, 0.5f, 0);
                break;
            default:
                color = Color.red;
                break;
        }
    }
}



[CustomPropertyDrawer(typeof(RequiredField))]
public class RequiredFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        RequiredField field = attribute as RequiredField;
        
        if(property.objectReferenceValue == null)
        {
            GUI.color = field.color; //Set the color of the GUI
            EditorGUI.PropertyField(position, property, label); //Draw the GUI
            GUI.color = Color.white; //Reset the color of the GUI to white
        }
        else
            EditorGUI.PropertyField(position, property, label);
    }
}