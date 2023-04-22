using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VignetteController : MonoBehaviour
{
    [Header("vignette on values")]
    [SerializeField] [Range(0f, 1f)] private float apetureSize;

    [SerializeField] [Range(0f, 1f)] private float featheringEffect;
    
    private Material vignetteMaterial;

    private bool enabled = false;
    // Start is called before the first frame update
    void Start()
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        vignetteMaterial = renderer.sharedMaterial;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ToggleVignette()
    {
        string apetureProperty = "_ApetureSize";
        float newApeture = enabled ? 1f : apetureSize;
        enabled = !enabled;

        vignetteMaterial.SetFloat(apetureProperty, newApeture);
    }
}
