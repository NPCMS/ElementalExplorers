using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class RPCManager : NetworkBehaviour
{
    [SerializeField] private TileInfo tileInfo;
    
    public static RPCManager Instance;

    public void Awake()
    {
        Instance = this;
    }

    public void CallSetPipelineCoords(Vector2Int[] tiles, Vector2 selectedCoords)
    {
        SetPipelineCoordsClientRpc(tiles, selectedCoords);
    }

    [ClientRpc]
    private void SetPipelineCoordsClientRpc(Vector2Int[] tiles, Vector2 selectedCoords)
    {
        Debug.Log("Set Pipeline ClientRPC recieved!");
        tileInfo.tiles = tiles.ToList();
        tileInfo.selectedCoords = selectedCoords;
    }
}
