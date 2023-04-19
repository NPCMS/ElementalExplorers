using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FogShaderVariables : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private float offset;
    [SerializeField] private FogDataSO fogData;
    [SerializeField] private MistDataSO mistData;
    [SerializeField] private Material fog;
    
    //private static readonly int SunColor = Shader.PropertyToID("_SunColor");
    //private static readonly int SunDirection = Shader.PropertyToID("_SunDirection");
    //private static readonly int MistHeight = Shader.PropertyToID("_MistHeight");
    //private static readonly int MistPow = Shader.PropertyToID("_MistPow");
    //private static readonly int FogColor = Shader.PropertyToID("_FogColor");
    //private static readonly int ExtinctionID = Shader.PropertyToID("_Extinction");
    //private static readonly int InscatteringID = Shader.PropertyToID("_Inscattering");
    //private static readonly int OffsetID = Shader.PropertyToID("_MistHeightOffset");

    private Dictionary<Vector2Int, TileComponent> tiles = new Dictionary<Vector2Int, TileComponent>();
    private float terrainSize;
    private Vector2Int origin;
    private Transform camTransform;
    
    private void Start()
    {
        fog.SetFloat("_MistHeightOffset", offset);
        fog.SetColor("_SunColor", sun.color);
        fog.SetVector("_SunDirection", sun.transform.up);

        fog.SetFloat("_MistHeight", mistData.MistData.MistAmount);
        fog.SetFloat("_MistPow", mistData.MistData.MistPow);
        fog.SetColor("_Extinction", fogData.FogData.Extinction * fogData.FogData.Density);
        fog.SetColor("_Inscattering", fogData.FogData.Inscattering * fogData.FogData.Density);
        fog.SetColor("_FogColor", fogData.FogData.FogColour);
        if (tiles == null)
        {
            tiles = new Dictionary<Vector2Int, TileComponent>();
        }
    }

    private void OnValidate()
    {
        fog.SetFloat("_MistHeightOffset", offset);
        fog.SetColor("_SunColor", sun.color);
        fog.SetVector("_SunDirection", sun.transform.forward);

        fog.SetFloat("_MistHeight", mistData.MistData.MistAmount);
        fog.SetFloat("_MistPow", mistData.MistData.MistPow);
        fog.SetColor("_Extinction", fogData.FogData.Extinction * fogData.FogData.Density);
        fog.SetColor("_Inscattering", fogData.FogData.Inscattering * fogData.FogData.Density);
        fog.SetColor("_FogColor", fogData.FogData.FogColour);
    }

    void Update()
    {
        UpdateFog();
    }
    

    private void UpdateFog()
    {

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
                fog.SetFloat("_MistHeightOffset", (float)height + offset);
            }
        }
    }
    
    public void InitialiseMultiTile(Dictionary<Vector2Int, TileComponent> components, Vector2Int origin, float terrainSize)
    {
        tiles = components;
        this.terrainSize = terrainSize;
        this.origin = origin;
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
