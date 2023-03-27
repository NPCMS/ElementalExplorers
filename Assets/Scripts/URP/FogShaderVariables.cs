using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[ExecuteInEditMode]
public class FogShaderVariables : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private float offset;
    [SerializeField] private FogDataSO fogData;
    [SerializeField] private MistDataSO mistData;
    
    private static readonly int SunColor = Shader.PropertyToID("_SunColor");
    private static readonly int SunDirection = Shader.PropertyToID("_SunDirection");
    private static readonly int MistHeight = Shader.PropertyToID("_MistHeight");
    private static readonly int MistPow = Shader.PropertyToID("_MistPow");
    private static readonly int FogColor = Shader.PropertyToID("_FogColor");
    private static readonly int ExtinctionID = Shader.PropertyToID("_Extinction");
    private static readonly int InscatteringID = Shader.PropertyToID("_Inscattering");
    private static readonly int OffsetID = Shader.PropertyToID("_MistHeightOffset");

    private Dictionary<Vector2Int, TileComponent> tiles = new Dictionary<Vector2Int, TileComponent>();
    private float terrainSize;
    private Vector2Int origin;
    private Transform camTransform;
    
    private void Start()
    {
        Shader.SetGlobalFloat(OffsetID, offset);
        if (tiles == null)
        {
            tiles = new Dictionary<Vector2Int, TileComponent>();
        }
    }

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

        if (camTransform != null)
        {
            Vector2 pos = new Vector2(camTransform.position.x, camTransform.position.z);
            Vector2Int coord = new Vector2Int((int) (pos.x / terrainSize),
                -(int) (pos.y / terrainSize));
            if (tiles.TryGetValue(coord + origin, out TileComponent tile))
            {
                coord.y = -coord.y;
                Vector3 position = new Vector3(pos.x % terrainSize, 0, pos.y % terrainSize);
                double height = tile.ElevationData.SampleHeightFromPosition(position);
                Shader.SetGlobalFloat(OffsetID, (float)height + offset);
            }
        }
    }
    
    public void InitialiseMultiTile(Dictionary<Vector2Int, TileComponent> components, Vector2Int origin, float terrainSize)
    {
        tiles = components;
        this.terrainSize = terrainSize;
        this.origin = origin;
        foreach (var component in components)
        {
            Debug.Log(component.Key);
        }
        // Look for the only active camera from all cameras
        foreach (var c in Camera.allCameras)
        {
            if (c.isActiveAndEnabled)
            {
                camTransform = c.transform;
                break;
            } 
        }
    }
}
