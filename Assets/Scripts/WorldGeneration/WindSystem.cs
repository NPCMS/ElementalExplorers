using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

[ExecuteInEditMode]
public class WindSystem : MonoBehaviour
{
    [SerializeField] private ComputeShader windShader;
    [SerializeField] private float scale = 10;
    [SerializeField] private Vector2 scrollX;
    [SerializeField] private Vector2 scrollY;
    [SerializeField] private int width = 256;

    [SerializeField] private string globalWindIdentifier = "_WindTexture";

    private int kernel;
    public RenderTexture windTexture;

    private void Start()
    {
        InitialiseTexture();
    }
    

    private void OnValidate()
    {
        if (windTexture == null || windTexture.width != width)
        {
            InitialiseTexture();
        }

        windShader.SetFloat("_Scale", scale);
        windShader.SetVector("_ScrollX", scrollX);
        windShader.SetVector("_ScrollY", scrollY);
    }

    private void Update()
    {
        if (windTexture != null)
        {
            Profiler.BeginSample("Wind Compute");
            windShader.SetTextureFromGlobal(kernel, "Result", globalWindIdentifier);
            windShader.SetFloat("_Time", Time.timeSinceLevelLoad);
            windShader.Dispatch(kernel, width / 8, width / 8, 1);
            Profiler.EndSample();
        }
    }

    private void InitialiseTexture()
    {
        kernel = windShader.FindKernel("CSMain");
        if (windTexture != null)
        {
            windTexture.Release();
        }
        windTexture = new RenderTexture(width, width, 0, GraphicsFormat.R8G8_SNorm);
        windTexture.enableRandomWrite = true;
        windTexture.Create();
        
        Shader.SetGlobalTexture(globalWindIdentifier, windTexture);
    }
}
