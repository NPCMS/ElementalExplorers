using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class BlenderData : MonoBehaviour
{
    [SerializeField, TextArea] public string test;
    [SerializeField] private BuildifyCityData city;
    void Start()
    {
        SerialisableTransform[] transforms = new SerialisableTransform[3];
        for (int i = 0; i < 3; i++)
        {
            transforms[i] = new SerialisableTransform()
            {
                position = new float[]{i, 0,0},
                eulerAngles = new float[]{i, 0,0},
                scale=  new float[]{i, 0,0},
            };
        }

        BuildifyPrefabData[] data = new BuildifyPrefabData[]
        {
            new BuildifyPrefabData() {name = "middle_floor_wall_03", transforms = transforms},
            new BuildifyPrefabData(){name = "middle_floor_wall01", transforms = transforms}
        };

        BuildifyBuildingData building = new BuildifyBuildingData() {prefabs = data};
        BuildifyCityData city = new BuildifyCityData() {buildings = new[] {building, building}};
        Debug.Log(JsonUtility.ToJson(city, true));

        List<BuildifyFootprint> footprints = new List<BuildifyFootprint>();
        footprints.Add(new BuildifyFootprint() {verts = new []{new []{0.0f,0,0},new []{1.0f,0,0}, new []{1.0f,1,0}}, faces = new []{0,1,2}, height=6.2f, levels=3});
        footprints.Add(new BuildifyFootprint() {verts = new []{new []{1.0f,0,0},new []{2.0f,0,0}, new []{3.0f,3.0f,0}}, faces = new []{2,1,0}, height=10.2f, levels=5});
        Debug.Log(JsonConvert.SerializeObject(new BuildifyFootprintList() {footprints = footprints.ToArray()},
            Formatting.Indented));
        
        this.city = (BuildifyCityData)JsonUtility.FromJson(test, typeof(BuildifyCityData));
    }
}

[System.Serializable]
public class BuildifyCityData
{
    public BuildifyBuildingData[] buildings;
}

[System.Serializable]
public class BuildifyBuildingData
{
    public BuildifyPrefabData[] prefabs;
}

[System.Serializable]
public class BuildifyPrefabData
{
    public string name;
    public SerialisableTransform[] transforms;
}

[System.Serializable]
public struct SerialisableTransform
{
    public float[] position;
    public float[] eulerAngles;
    public float[] scale;
}

[System.Serializable]
public class BuildifyFootprintList
{
    public BuildifyFootprint[] footprints;
}

[System.Serializable]
public class BuildifyFootprint
{
    public float[][] verts;
    public int[] faces;
    public float height;
    public int levels;
}