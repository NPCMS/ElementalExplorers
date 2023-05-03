using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using XNode;

[CreateNodeMenu("Utils/Tile to Bounding Box")]
public class TileToBoundingBoxNode : SyncExtendedNode
{
    [Input] public Vector2Int tileInput;
    [Input] public float zoomLevel = 15;
    [Output] public GlobeBoundingBox boundingBox;

    // Return the correct value of an output port when requested
    public override object GetValue(NodePort port) 
    {
        if (port.fieldName == "boundingBox")
        {
            return boundingBox;
        }
        return null; // Replace this
    }

    public override IEnumerator CalculateOutputs(Action<bool> callback)
    {
        boundingBox = TileCreation.GetBoundingBoxFromTile(GetInputValue("tileInput", tileInput), GetInputValue("zoomLevel", zoomLevel));
        callback.Invoke(true);
        yield break;
    }

    public override void Release()
    {
    }
    public override void ApplyGUI()
    {
        base.ApplyGUI();
#if UNITY_EDITOR
        EditorGUILayout.LabelField($"North: {boundingBox.north}, East: {boundingBox.east}, South: {boundingBox.south}, West: {boundingBox.west}");
#endif
    }
}