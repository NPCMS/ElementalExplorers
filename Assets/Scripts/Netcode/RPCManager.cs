using Unity.Netcode;
using UnityEngine;

public class RPCManager : NetworkBehaviour
{
    [SerializeField] private TileInfo tileInfo;
    
    public static RPCManager Instance;

    public void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void CallSetPipelineCoords(Vector2Int[] tiles, Vector2 selectedCoords, bool defaultSelected)
    {
        SetPipelineCoordsClientRpc(tiles, selectedCoords, defaultSelected);
    }

    [ClientRpc]
    private void SetPipelineCoordsClientRpc(Vector2Int[] tiles, Vector2 selectedCoords, bool defaultSelected)
    {
        Debug.Log("Set Pipeline ClientRPC recieved!");
        tileInfo.SetTiles(tiles);
        tileInfo.selectedCoords = selectedCoords;
        tileInfo.useDefault = defaultSelected;
    }
}
