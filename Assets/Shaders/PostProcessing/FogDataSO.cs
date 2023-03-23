using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Fog")]
public class FogDataSO : ScriptableObject
{
    [FormerlySerializedAs("fog")] [SerializeField] private FogData fogData;
    public FogData FogData => fogData;
}

[System.Serializable]
public struct FogData
{
    public Color FogColour => fogColour;
    public Color Extinction => extinction;
    public Color Inscattering => inscattering;
    public float Density => density;

    [SerializeField] private float density;
    [SerializeField] private Color fogColour;
    [SerializeField] private Color extinction;
    [SerializeField] private Color inscattering;
    
    public FogData(float density, Color fogColour, Color extinction, Color inscattering)
    {
        this.density = density;
        this.fogColour = fogColour;
        this.extinction = extinction;
        this.inscattering = inscattering;
    }
}