using System.Collections.Generic;
using UnityEngine;

public class VignetteController : MonoBehaviour
{
    [Header("vignette on values")]
    [SerializeField] [Range(0f, 1f)] private float low;
    [SerializeField] [Range(0f, 1f)] private float medium;
    [SerializeField] [Range(0f, 1f)] private float high;

    private List<float> size;
    
    private Material vignetteMaterial;

    private bool enabled = false;
    // Start is called before the first frame update
    void Start()
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        vignetteMaterial = renderer.sharedMaterial;
        size = new List<float> { 1f, low, medium, high };
        vignetteMaterial.SetFloat("_ApertureSize", 1f);
        SettingsMenu.instance.AddVignetteCallback(ToggleVignette);
    }

    private void OnDestroy()
    {
        SettingsMenu.instance.RemoveVignetteCallback(ToggleVignette);
    }

    private void ToggleVignette(int index)
    {
        vignetteMaterial.SetFloat("_ApertureSize", size[index]);
    }
    
}
