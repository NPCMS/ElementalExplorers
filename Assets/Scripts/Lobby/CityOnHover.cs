using UnityEngine;

public class CityOnHover : MonoBehaviour
{
    [SerializeField] private Color selectedColour = new Color(10,241,8);
    
    private Renderer renderer;
    private Color defaultColour;
    private readonly string pulseId = "_PulseAmount";
    private readonly string colourId = "_FresnelColour";
    
    // Start is called before the first frame update
    void Start()
    {
        renderer = gameObject.GetComponent<Renderer>();
        renderer.material.SetFloat(pulseId, 0f);
        defaultColour = renderer.material.GetColor(colourId);
        selectedColour = new Color(10, 241, 8);
    }

    public void OnSelection()
    {
        renderer.material.SetColor(colourId, selectedColour);
    }

    public void OnDeselection()
    {
        renderer.material.SetColor(colourId, defaultColour);
    }

    public void OnHoverStart()
    {
        renderer.material.SetFloat(pulseId, 1f);
    }

    public void OnHoverEnd()
    {
        renderer.material.SetFloat(pulseId, 0f);
    }
}
