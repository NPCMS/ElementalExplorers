using UnityEngine;

[CreateAssetMenu(fileName = "Asset Database", menuName = "Asset Database")]
public class AssetDatabaseSO : ScriptableObject
{
    //https://github.com/azixMcAze/Unity-SerializableDictionary
    [SerializeField] private SerializableDictionary<string, GameObject> prefabDictionary;
    [SerializeField] private SerializableDictionary<string, Material> materialDictionary;
    
    public bool TryGetPrefab(string name, out GameObject prefab)
    {
        return prefabDictionary.TryGetValue(name, out prefab);
    }
    
    public bool TryGetMaterial(string name, out Material material)
    {
        return materialDictionary.TryGetValue(name, out material);
    }
}