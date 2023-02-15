using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(WindSystem))]
[CanEditMultipleObjects]
public class WindSystemEditor : Editor
{
    SerializedProperty damageProp;
    public override bool RequiresConstantRepaint()
    {
        return true;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        WindSystem system = (WindSystem) target;
        EditorGUI.DrawPreviewTexture(new Rect(10, 200, 128, 128), system.windTexture);

        serializedObject.ApplyModifiedProperties();
    }
}
