using UnityEngine;

[ExecuteInEditMode]
public class SDFTest : MonoBehaviour
{
    [SerializeField] private ComputeShader compute;
    [SerializeField] private Texture2D test;
    [SerializeField] private Material mat;
    [SerializeField] private Texture2D output;
    [SerializeField] private int iterations = 10;
    [SerializeField] private float amount = 0.5f;
    [SerializeField] private float amountIn = 0.5f;

    private void OnValidate()
    {
        output = TextureGenerator.RenderSDF(compute, test, iterations, amount, amountIn);
        mat.mainTexture = output;
    }
}
