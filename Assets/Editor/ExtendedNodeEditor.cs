using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

[CustomNodeEditor(typeof(ExtendedNode))]
public class ExtendedNodeEditor : NodeEditor
{
    public override void OnBodyGUI()
    {
        // Draw default editor
        base.OnBodyGUI();

        // Get your node
        ExtendedNode node = (ExtendedNode)target;
        node.ApplyGUI();
    }
}
