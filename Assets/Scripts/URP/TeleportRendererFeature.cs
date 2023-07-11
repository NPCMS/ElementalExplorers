using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

internal class TeleportRendererFeature : ScriptableRendererFeature
{    
    [SerializeField] private Material material;

    private TeleportPass teleportPass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //Calling ConfigureInput with the ScriptableRenderPassInput.Color argument ensures that the opaque texture is available to the Render Pass
        teleportPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        teleportPass.SetTarget(renderer.cameraColorTarget);
        renderer.EnqueuePass(teleportPass);
    }

    public override void Create()
    {
        teleportPass = new TeleportPass(material);
    }
}

internal class TeleportPass : ScriptableRenderPass
{

    private ProfilingSampler fogProfilingSampler = new ProfilingSampler("ColorBlit");
    private Material material;
    private RenderTargetIdentifier cameraColorTarget;
    public TeleportPass(Material material)
    {
        this.material = material;
        renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
    }

    public void SetTarget(RenderTargetIdentifier colorHandle)
    {
        cameraColorTarget = colorHandle;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        ConfigureTarget(new RenderTargetIdentifier(cameraColorTarget, 0, CubemapFace.Unknown, -1));
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;

        if (material == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, fogProfilingSampler))
        {
            cmd.SetRenderTarget(new RenderTargetIdentifier(cameraColorTarget, 0, CubemapFace.Unknown, -1));
            //The RenderingUtils.fullscreenMesh argument specifies that the mesh to draw is a quad.
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }
}
