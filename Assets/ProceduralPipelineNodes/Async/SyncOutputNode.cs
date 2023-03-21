[NodeTint(0.6f, 0.2f, 0.2f)]
public abstract class SyncOutputNode : SyncExtendedNode
{
    public abstract void ApplyOutput(AsyncPipelineManager manager);
}
