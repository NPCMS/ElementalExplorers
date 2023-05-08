using XNodeEditor;

[CustomNodeEditor(typeof(SyncExtendedNode))]
public class ExtendedNodeEditor : NodeEditor
{
    public override void OnBodyGUI()
    {
        // Draw default editor
        base.OnBodyGUI();

        // Get your node
        SyncExtendedNode node = (SyncExtendedNode)target;
        node.ApplyGUI();
    }
}
