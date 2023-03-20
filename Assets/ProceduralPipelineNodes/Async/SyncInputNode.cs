namespace ProceduralPipelineNodes.Async
{
    public abstract class SyncInputNode : SyncExtendedNode
    {
        public abstract void ApplyInputs(AsyncPipelineManager manager);   
    }
}
