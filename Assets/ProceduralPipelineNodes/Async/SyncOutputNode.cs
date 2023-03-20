namespace ProceduralPipelineNodes.Async
{
    public abstract class SyncOutputNode : SyncExtendedNode
    {
        public abstract void ApplyOutput(AsyncPipelineManager manager);
    }
}
