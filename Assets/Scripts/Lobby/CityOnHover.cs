using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CityOnHover : MonoBehaviour
{
    private Renderer renderer;
    private bool active = false;
    
    // Start is called before the first frame update
    void Start()
    {
        renderer = gameObject.GetComponent<Renderer>();
        renderer.sharedMaterial.SetFloat("PulseAmount", 0f);
    }

    // Update is called once per frame
    void Update()
    {
        renderer.sharedMaterial.SetFloat("PulseAmount", active? 1f : 0f);
    }

    public void OnHover()
    {
        active = !active;
    }
}
