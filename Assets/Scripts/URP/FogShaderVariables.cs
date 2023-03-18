using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FogShaderVariables : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private FogDataSO fogData;
    [SerializeField] private MistDataSO mistData;
    
    private static readonly int SunColor = Shader.PropertyToID("_SunColor");
    private static readonly int SunDirection = Shader.PropertyToID("_SunDirection");
    private static readonly int MistHeight = Shader.PropertyToID("_MistHeight");
    private static readonly int MistPow = Shader.PropertyToID("_MistPow");
    private static readonly int FogColor = Shader.PropertyToID("_FogColor");
    private static readonly int ExtinctionID = Shader.PropertyToID("_Extinction");
    private static readonly  int InscatteringID = Shader.PropertyToID("_Inscattering");

    void Update()
    {
        Shader.SetGlobalColor(SunColor, sun.color);
        Shader.SetGlobalVector(SunDirection, sun.transform.up);
        UpdateFog(fogData.FogData, mistData.MistData);
    }
    

    private void UpdateFog(FogData fog, MistData mist)
    {
        Shader.SetGlobalFloat(MistHeight, mist.MistAmount);
        Shader.SetGlobalFloat(MistPow, mist.MistPow);
        Shader.SetGlobalColor(ExtinctionID, fog.Extinction * fog.Density);
        Shader.SetGlobalColor(InscatteringID, fog.Inscattering * fog.Density);
        Shader.SetGlobalColor(FogColor, fog.FogColour);
    }

}
