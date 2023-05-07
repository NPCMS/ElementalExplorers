using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PrecomputeWindow : EditorWindow
{
    public Vector2Int fromTile;
    public Vector2Int toTile;
    public List<Vector2Int> output;
    [MenuItem("Window/Precompute Window")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        PrecomputeWindow window = (PrecomputeWindow)EditorWindow.GetWindow(typeof(PrecomputeWindow));
        window.Show();
    }

    private void OnGUI()
    {
        fromTile = EditorGUILayout.Vector2IntField("fromTile", fromTile);
        toTile = EditorGUILayout.Vector2IntField("toTile", toTile);

        if (GUILayout.Button("Create Tiles", new GUILayoutOption[] { }))
        {
            output = new List<Vector2Int>();
            for (int i = 0; i <= Mathf.Abs(fromTile.x - toTile.x); i++)
            {
                int x = fromTile.x + (toTile.x - fromTile.x > 0 ? 1 : -1) * i;
                for (int j = 0; j <= Mathf.Abs(fromTile.y - toTile.y); j++)
                {
                    int y = fromTile.y + (toTile.y - fromTile.y > 0 ? 1 : -1) * j;
                    output.Add(new Vector2Int(x, y));
                }
            }
        }
       
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty stringsProperty = so.FindProperty("output");

        EditorGUILayout.PropertyField(stringsProperty, true);
        so.ApplyModifiedProperties();

    }
}
